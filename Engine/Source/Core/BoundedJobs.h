#pragma once
#include <algorithm>
#include <atomic>
#include <condition_variable>
#include <functional>
#include <memory>
#include <mutex>
#include <optional>
#include <stdexcept>
#include <string>
#include <thread>
#include <vector>
namespace engine {
// Cooperative token is independent of platform stop_token availability.
class JobCancellation {
    std::shared_ptr<std::atomic<bool>> flag_;
public:
    explicit JobCancellation(std::shared_ptr<std::atomic<bool>> flag):flag_(std::move(flag)){}
    bool cancelled()const{return flag_->load();}
};
struct UploadAdmission {
    size_t perFrame=8*1024*1024,hardLimit=128*1024*1024,used=0;
    bool oversizedItem=false;
    void beginFrame(){used=0;oversizedItem=false;}
    bool admit(size_t bytes){
        if(bytes>hardLimit)return false;
        if(used>perFrame||bytes>perFrame-used){if(used!=0)return false;oversizedItem=true;}
        used+=bytes;return true;
    }
};
// CPU-only jobs. Caller owns publishing on its simulation/render thread.
// Reservations bound queued, running and completed payloads together. Work must honor its declared peak bound.
template<class T> class BoundedJobs {
public:
    using Work=std::function<T(const JobCancellation&)>;
    struct Completion {uint64_t ticket;std::string owner;uint64_t epoch;std::optional<T> value;std::string error;size_t bytes=0;};
    struct Stats {size_t outstanding=0,reservedBytes=0,ready=0;};
private:
    struct Entry {
        uint64_t ticket,epoch;std::string owner;size_t reservation;Work work;
        std::shared_ptr<std::atomic<bool>> cancelled=std::make_shared<std::atomic<bool>>(false);
        bool running=false,done=false;std::optional<T> value;std::string error;size_t bytes=0;
    };
    std::mutex mutex_;std::condition_variable wake_;bool stopping_=false;
    size_t capacity_,limit_,reserved_=0;uint64_t next_=1;
    std::vector<std::shared_ptr<Entry>> entries_;std::vector<std::thread> workers_;
    std::function<size_t(const T&)> measure_;
    void erase(const std::shared_ptr<Entry>& entry){reserved_-=entry->reservation;entries_.erase(std::find(entries_.begin(),entries_.end(),entry));}
    void loop(){
        for(;;){std::shared_ptr<Entry> entry;
            {std::unique_lock lock(mutex_);wake_.wait(lock,[&]{return stopping_||std::any_of(entries_.begin(),entries_.end(),[](const auto& e){return !e->running&&!e->done;});});
                if(stopping_)return;
                for(const auto& e:entries_)if(!e->running&&!e->done){entry=e;break;}
                entry->running=true;
            }
            std::optional<T> value;std::string error;size_t bytes=0;
            try{if(!entry->cancelled->load()){value.emplace(entry->work(JobCancellation(entry->cancelled)));bytes=measure_(*value);if(bytes>entry->reservation)throw std::runtime_error("Job result exceeds reserved bytes");}}
            catch(const std::exception& e){error=e.what();value.reset();bytes=0;}
            catch(...){error="Unknown generation failure";value.reset();bytes=0;}
            {std::lock_guard lock(mutex_);entry->work={};
                if(entry->cancelled->load()){erase(entry);}
                else {entry->value=std::move(value);entry->error=std::move(error);entry->bytes=bytes;entry->done=true;}
            }
            wake_.notify_all();
        }
    }
public:
    explicit BoundedJobs(std::function<size_t(const T&)> measure,size_t workers=1,size_t capacity=32,size_t bytes=128*1024*1024):capacity_(capacity),limit_(bytes),measure_(std::move(measure)){
        if(!workers||workers>4||!capacity||!bytes||!measure_)throw std::invalid_argument("Invalid job limits");
        entries_.reserve(capacity);workers_.reserve(workers);
        try{for(size_t i=0;i<workers;++i)workers_.emplace_back([this]{loop();});}catch(...){shutdown();throw;}
    }
    ~BoundedJobs(){shutdown();}
    BoundedJobs(const BoundedJobs&)=delete;BoundedJobs& operator=(const BoundedJobs&)=delete;
    // Newer requests cancel prior work for this owner; rejected admission does not discard valid work.
    std::optional<uint64_t> submit(std::string owner,uint64_t epoch,size_t reservation,Work work){
        if(owner.empty()||owner.size()>256||!epoch||!reservation||!work)throw std::invalid_argument("Invalid job request");
        std::lock_guard lock(mutex_);
        if(stopping_||reservation>limit_)return {};
        for(const auto& e:entries_)if(e->owner==owner&&!e->cancelled->load()&&epoch<=e->epoch)return {};
        size_t reclaim=0,count=0;for(const auto& e:entries_)if(e->owner==owner&&(!e->running||e->done)){reclaim+=e->reservation;++count;}
        if(entries_.size()-count>=capacity_||reservation>limit_-(reserved_-reclaim))return {};
        if(next_==UINT64_MAX)throw std::overflow_error("Job ticket exhausted");
        auto entry=std::make_shared<Entry>();entry->ticket=next_++;entry->epoch=epoch;entry->owner=std::move(owner);entry->reservation=reservation;entry->work=std::move(work);
        for(auto i=entries_.begin();i!=entries_.end();){const auto& e=*i;if(e->owner==entry->owner){e->cancelled->store(true);if(!e->running||e->done){reserved_-=e->reservation;i=entries_.erase(i);continue;}}++i;}
        reserved_+=reservation;entries_.push_back(entry);wake_.notify_one();return entry->ticket;
    }
    void cancel(const std::string& owner){std::lock_guard lock(mutex_);for(auto i=entries_.begin();i!=entries_.end();){const auto& e=*i;if(e->owner==owner){e->cancelled->store(true);if(!e->running||e->done){reserved_-=e->reservation;i=entries_.erase(i);continue;}}++i;}wake_.notify_all();}
    std::optional<Completion> takeReady(UploadAdmission& budget){
        std::lock_guard lock(mutex_);
        for(const auto& e:entries_)if(e->done&&!e->cancelled->load()&&budget.admit(e->bytes)){
            Completion result{e->ticket,e->owner,e->epoch,std::move(e->value),std::move(e->error),e->bytes};auto owned=e;erase(owned);return result;
        }return {};
    }
    Stats stats(){std::lock_guard lock(mutex_);return {entries_.size(),reserved_,size_t(std::count_if(entries_.begin(),entries_.end(),[](const auto& e){return e->done;}))};}
    void shutdown(){
        {std::lock_guard lock(mutex_);stopping_=true;for(const auto& e:entries_)e->cancelled->store(true);}
        wake_.notify_all();for(auto& worker:workers_)if(worker.joinable())worker.join();
        std::lock_guard lock(mutex_);entries_.clear();reserved_=0;
    }
};
}
