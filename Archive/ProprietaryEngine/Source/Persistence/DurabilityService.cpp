#include "Persistence/DurabilityService.h"
#include <algorithm>

namespace engine {

struct DurabilityService::Request {
    DurableReceipt receipt;
    std::filesystem::path profile;
    WorldConfiguration configuration;
    std::optional<WorldSave> save;
    std::optional<WorldSaveRead> loaded;
    uint64_t expectedGeneration=0;
    bool explicitSave=true;
};

DurabilityService::DurabilityService(DurabilityHooks hooks):hooks_(std::move(hooks)),worker_([this]{run();}){}
DurabilityService::~DurabilityService(){shutdown();}

uint64_t DurabilityService::save(std::filesystem::path profile,WorldSave state,uint64_t expectedGeneration,
                                 uint64_t sessionEpoch,uint64_t sourceRevision,bool explicitSave){
    if(profile.empty()||!sessionEpoch||!sourceRevision)throw std::invalid_argument("Durable save identity is invalid");
    validateTerrainRecipe(state.configuration.terrain);validateRecipe(state.configuration.rock);validateConstraints(state.configuration.constraints);
    validateSnapshot(state.player);validateDeltas(state.deltas);
    std::lock_guard lock(mutex_);if(stopping_)throw std::runtime_error("Durability service is closed");
    while(receipts_.size()>=64){auto old=std::find_if(receipts_.begin(),receipts_.end(),[](const auto& item){return item.second->receipt.terminal();});if(old==receipts_.end())throw std::runtime_error("Durability receipt capacity exceeded");receipts_.erase(old);}
    if(nextId_==UINT64_MAX)throw std::runtime_error("Durability request ID exhausted");
    if(!explicitSave)for(auto& candidate:queue_)if(!candidate->explicitSave&&candidate->receipt.operation==DurableOperation::Save&&candidate->profile==profile){
        candidate->receipt.state=DurableState::Superseded;candidate->receipt.error="Superseded by newer autosave";
    }
    std::erase_if(queue_,[](const auto& request){return request->receipt.state==DurableState::Superseded;});
    if(queue_.size()>=16)throw std::runtime_error("Durability queue capacity exceeded");
    const auto id=nextId_++;
    auto request=std::make_shared<Request>();request->receipt={id,DurableOperation::Save,DurableState::Queued,sessionEpoch,sourceRevision,expectedGeneration,{}};
    request->profile=std::move(profile);request->configuration=state.configuration;request->save=std::move(state);request->expectedGeneration=expectedGeneration;request->explicitSave=explicitSave;
    receipts_.emplace(id,request);queue_.push_back(request);wake_.notify_one();return id;
}

uint64_t DurabilityService::load(std::filesystem::path profile,WorldConfiguration configuration,uint64_t sessionEpoch,uint64_t sourceRevision){
    if(profile.empty()||!sessionEpoch||!sourceRevision)throw std::invalid_argument("Durable load identity is invalid");
    validateTerrainRecipe(configuration.terrain);validateRecipe(configuration.rock);validateConstraints(configuration.constraints);
    std::lock_guard lock(mutex_);if(stopping_)throw std::runtime_error("Durability service is closed");
    while(receipts_.size()>=64){auto old=std::find_if(receipts_.begin(),receipts_.end(),[](const auto& item){return item.second->receipt.terminal();});if(old==receipts_.end())throw std::runtime_error("Durability receipt capacity exceeded");receipts_.erase(old);}
    if(queue_.size()>=16||nextId_==UINT64_MAX)throw std::runtime_error("Durability queue capacity exceeded");
    const auto id=nextId_++;auto request=std::make_shared<Request>();
    request->receipt={id,DurableOperation::Load,DurableState::Queued,sessionEpoch,sourceRevision,0,{}};
    request->profile=std::move(profile);request->configuration=std::move(configuration);
    receipts_.emplace(id,request);queue_.push_back(request);wake_.notify_one();return id;
}

DurableReceipt DurabilityService::receipt(uint64_t id)const{
    std::lock_guard lock(mutex_);const auto found=receipts_.find(id);if(found==receipts_.end())throw std::out_of_range("Durability receipt expired or unknown");return found->second->receipt;
}

std::optional<WorldSaveRead> DurabilityService::takeLoaded(uint64_t id,uint64_t epoch,uint64_t revision){
    std::lock_guard lock(mutex_);const auto found=receipts_.find(id);if(found==receipts_.end())throw std::out_of_range("Durability receipt expired or unknown");
    auto& request=*found->second;if(request.receipt.operation!=DurableOperation::Load)throw std::invalid_argument("Receipt is not a load");
    if(request.receipt.sessionEpoch!=epoch||request.receipt.sourceRevision!=revision)throw std::runtime_error("Stale load result rejected");
    if(request.receipt.state==DurableState::Rejected)throw std::runtime_error(request.receipt.error);
    if(request.receipt.state!=DurableState::Loaded)return {};
    auto result=std::move(request.loaded);receipts_.erase(found);return result;
}

void DurabilityService::run()noexcept{
    for(;;){std::shared_ptr<Request> request;
        {std::unique_lock lock(mutex_);wake_.wait(lock,[&]{return stopping_||!queue_.empty();});if(queue_.empty())return;request=queue_.front();queue_.pop_front();request->receipt.state=DurableState::Writing;}
        try{
            if(request->receipt.operation==DurableOperation::Save){
                if(hooks_.beforeSave)hooks_.beforeSave(request->receipt.requestId);
                const auto generation=saveWorld(request->profile,*request->save,request->expectedGeneration);
                std::lock_guard lock(mutex_);request->save.reset();request->receipt.diskGeneration=generation;request->receipt.state=DurableState::Durable;
            }else{
                if(hooks_.beforeLoad)hooks_.beforeLoad(request->receipt.requestId);
                auto loaded=loadWorldSave(request->profile,request->configuration);
                std::lock_guard lock(mutex_);request->receipt.diskGeneration=loaded.generation;request->loaded=std::move(loaded);request->receipt.state=DurableState::Loaded;
            }
        }catch(const std::exception& error){std::lock_guard lock(mutex_);request->save.reset();request->receipt.state=DurableState::Rejected;request->receipt.error=std::string(error.what()).substr(0,512);}
        catch(...){std::lock_guard lock(mutex_);request->save.reset();request->receipt.state=DurableState::Rejected;request->receipt.error="Unknown durability failure";}
    }
}

void DurabilityService::shutdown()noexcept{
    {std::lock_guard lock(mutex_);if(stopping_)return;stopping_=true;}
    wake_.notify_all();if(worker_.joinable())worker_.join();
}

}
