#include "Authoring/AuthoringHost.h"
#include "Authoring/SceneAuthoringAdapter.h"
#include "Persistence/JsonLifetime.h"
#include <chrono>
#include <iomanip>
#include <random>
#include <set>
#include <sstream>

namespace engine {
namespace {
using Clock=std::chrono::steady_clock;
uint64_t ns(Clock::time_point start){return uint64_t(std::chrono::duration_cast<std::chrono::nanoseconds>(Clock::now()-start).count());}
void fields(const Json& value,std::initializer_list<const char*> allowed){
    if(!value.is_object())throw std::invalid_argument("Expected command object");
    for(const auto& entry:value.items())
        if(std::none_of(allowed.begin(),allowed.end(),[&](const char* key){return entry.key()==key;}))throw std::invalid_argument("Unknown command field: "+entry.key());
}
uint64_t number(const Json& value){
    if(!value.is_number_unsigned())throw std::invalid_argument("Expected unsigned integer");
    return value.get<uint64_t>();
}
std::string newEpoch(){
    std::random_device random;std::ostringstream out;
    for(int i=0;i<4;++i)out<<std::hex<<std::setw(8)<<std::setfill('0')<<uint32_t(random());
    return out.str();
}
std::string status(uint64_t id,const std::string& epoch,const char* state,const char* message=nullptr){
    PreparedJson out;out.value["request_id"]=id;out.value["epoch"]=epoch;out.value["status"]=state;
    if(message)out.value["error"]=std::string(message).substr(0,512);
    return out.value.dump();
}
}
Json parseAuthoringRequest(const std::string& wire,size_t maxBytes){
    if(wire.size()>maxBytes)throw std::invalid_argument("Request byte limit exceeded");
    std::vector<std::set<std::string>> keys;
    auto callback=[&](int depth,Json::parse_event_t event,Json& value){
        if(depth>=32)throw std::invalid_argument("Request nesting limit exceeded");
        if(event==Json::parse_event_t::object_start)keys.emplace_back();
        if(event==Json::parse_event_t::key&&!keys.back().insert(value.get<std::string>()).second)throw std::invalid_argument("Duplicate request key");
        if(event==Json::parse_event_t::object_end)keys.pop_back();
        return true;
    };
    auto result=Json::parse(wire,callback);
    if(!result.is_object())throw std::invalid_argument("Expected request object");
    return result;
}
struct AuthoringHost::Record {
    uint64_t id=0,expected=0;
    std::string operation,requestWire,wire,preparing,cancelled,failure;
    Json payload;
    size_t bytes=0,accounted=0;
    bool cancel=false,done=false;
    ~Record(){clearBoundedJson(payload);}
};
AuthoringHost::AuthoringHost(AuthoringSceneDocument initial,AuthoringLimits limits):limits_(limits),epoch_(newEpoch()),document_(std::make_shared<AuthoringSceneDocument>(std::move(initial))){
    const AuthoringLimits maximum;
    if(!limits_.requestBytes||limits_.requestBytes>maximum.requestBytes||!limits_.pendingCount||limits_.pendingCount>maximum.pendingCount||
       !limits_.pendingBytes||limits_.pendingBytes>maximum.pendingBytes||limits_.resultBytes<1024||limits_.resultBytes>maximum.resultBytes||
       limits_.receiptCount<limits_.pendingCount||limits_.receiptCount>maximum.receiptCount||limits_.receiptBytes<limits_.resultBytes||
       limits_.receiptBytes>maximum.receiptBytes||!limits_.pageEntities||limits_.pageEntities>64)throw std::invalid_argument("Invalid authoring limits");
    snapshots_[0]=std::make_shared<AuthoringSceneDocument>(document_->readSnapshot());
    published_=snapshots_[0];
}
AuthoringHost::~AuthoringHost(){stop();}
void AuthoringHost::start(){
    std::lock_guard lock(mutex_);
    if(started_||stopping_)throw std::logic_error("Authoring host already started or closed");
    worker_=std::thread([this]{run();});started_=true;
}
void AuthoringHost::stop()noexcept{
    {
        std::lock_guard lock(mutex_);stopping_=true;
        for(auto& entry:receipts_)if(!entry.second->done)entry.second->cancel=true;
        if(!started_){
            for(auto& record:queue_){clearBoundedJson(record->payload);finish(record,record->cancelled);}
            queue_.clear();
        }
    }
    wake_.notify_all();
    if(worker_.joinable())worker_.join();
}
std::shared_ptr<const AuthoringSceneDocument> AuthoringHost::snapshot()const{
    return std::atomic_load_explicit(&published_,std::memory_order_acquire);
}
void AuthoringHost::identity(const Json& request)const{
    if(request.at("epoch").get_ref<const std::string&>()!=epoch_||request.at("document_id").get_ref<const std::string&>()!=document_->sceneId())
        throw SceneDocumentError("Document identity or live epoch mismatch",SceneErrorCode::Conflict);
}
std::string AuthoringHost::describe()const{
    PreparedJson out;
    // Copy primitives and stable pointers only while holding the owner lock.
    std::shared_ptr<const AuthoringSceneDocument> current;
    uint64_t high,prepare,adopt;size_t pending,bytes,receipts,receiptBytes;bool closed;
    {std::lock_guard lock(mutex_);current=snapshots_[snapshotSlot_];high=highWater_;prepare=prepareNs_;adopt=adoptNs_;pending=pendingCount_;bytes=pendingBytes_;receipts=receipts_.size();receiptBytes=receiptBytes_;closed=stopping_;}
    out.value={{"protocol",1},{"epoch",epoch_},{"document_id",current->sceneId()},{"revision",current->version()},
        {"next_entity_id",current->nextEntityId()},{"request_id_high_water",high},{"closed",closed},
        {"operations",{"describe","inspect_scene","inspect_entity","submit","receipt","cancel"}},
        {"commands",{"apply_transaction","preview_transaction","undo","redo"}},
        {"limits",{{"request_bytes",limits_.requestBytes},{"pending_count",limits_.pendingCount},{"pending_bytes",limits_.pendingBytes},
            {"result_bytes",limits_.resultBytes},{"receipt_count",limits_.receiptCount},{"receipt_bytes",limits_.receiptBytes},{"page_entities",limits_.pageEntities},{"pinned_revisions",4}}},
        {"usage",{{"pending_count",pending},{"pending_bytes",bytes},{"receipt_count",receipts},{"receipt_bytes",receiptBytes}}},
        {"timing",{{"last_prepare_ns",prepare},{"last_adopt_ns",adopt}}}};
    return out.value.dump();
}
std::string AuthoringHost::submit(const Json& request){
    fields(request,{"action","epoch","document_id","request_id","expected_revision","operation","payload"});
    auto record=std::make_shared<Record>();record->id=number(request.at("request_id"));record->expected=number(request.at("expected_revision"));
    if(!record->id||record->id==UINT64_MAX||!record->expected)throw std::invalid_argument("Nonzero request ID and revision required; maximum ID is reserved");
    record->operation=request.at("operation").get<std::string>();
    if(record->operation!="apply_transaction"&&record->operation!="preview_transaction"&&record->operation!="undo"&&record->operation!="redo")throw std::invalid_argument("Unsupported authoring operation");
    record->payload=request.at("payload");
    if(!record->payload.is_object())throw std::invalid_argument("Expected operation payload object");
    record->requestWire=request.dump();record->bytes=record->requestWire.size();
    // Retain exact request identity, not a collision-prone hash. Retained request
    // bytes and the largest terminal receipt share the same finite retention pool.
    record->accounted=limits_.resultBytes+record->bytes;
    record->wire=status(record->id,epoch_,"queued");record->preparing=status(record->id,epoch_,"preparing");
    record->cancelled=status(record->id,epoch_,"cancelled_before_apply");record->failure=status(record->id,epoch_,"rejected","Preparation failed; scene unchanged");
    std::unique_lock lock(mutex_);identity(request);
    if(auto found=receipts_.find(record->id);found!=receipts_.end()){
        if(found->second->requestWire!=record->requestWire)return status(record->id,epoch_,"request_id_conflict","A retained ID belongs to a different request; reconcile its receipt");
        return found->second->wire;
    }
    if(record->id<=highWater_)return status(record->id,epoch_,"expired_or_unknown");
    if(stopping_)throw std::runtime_error("Authoring host is closed");
    if(pendingCount_>=limits_.pendingCount||record->bytes>limits_.pendingBytes-pendingBytes_)throw std::runtime_error("Authoring pending capacity exceeded");
    while(receipts_.size()>=limits_.receiptCount||record->accounted>limits_.receiptBytes-receiptBytes_){
        auto old=std::find_if(receipts_.begin(),receipts_.end(),[](const auto& pair){return pair.second->done;});
        if(old==receipts_.end())throw std::runtime_error("Authoring receipt capacity exceeded");
        receiptBytes_-=old->second->accounted;receipts_.erase(old);
    }
    // Prepare the caller's response before admission too. Delivery may still fail;
    // the retained receipt remains authoritative in that case.
    auto response=record->wire;
    receipts_.emplace(record->id,record);
    try{queue_.push_back(record);}catch(...){receipts_.erase(record->id);throw;}
    highWater_=record->id;++pendingCount_;pendingBytes_+=record->bytes;receiptBytes_+=record->accounted;
    lock.unlock();wake_.notify_one();return response;
}
std::string AuthoringHost::inspect(const Json& request){
    const bool single=request.at("action").get_ref<const std::string&>()=="inspect_entity";
    if(single)fields(request,{"action","epoch","document_id","revision","entity"});
    else fields(request,{"action","epoch","document_id","revision","offset","limit"});
    const auto revision=number(request.at("revision"));
    std::shared_ptr<const AuthoringSceneDocument> pinned;
    {std::lock_guard lock(mutex_);identity(request);for(const auto& candidate:snapshots_)if(candidate&&candidate->version()==revision){pinned=candidate;break;}}
    if(!pinned)throw SceneDocumentError("Snapshot expired or unknown; restart inspection at the advertised revision",SceneErrorCode::Conflict);
    PreparedJson out;out.value={{"document_id",pinned->sceneId()},{"epoch",epoch_},{"revision",revision}};
    if(single){
        const auto id=number(request.at("entity"));const auto* entity=pinned->find(id);
        if(!entity)throw std::invalid_argument("Entity does not exist");
        out.value["entity"]=AuthoringSceneDocument::entityToJson(*entity);out.value["world_matrix"]=pinned->worldMatrix(id);
    }else{
        const auto offset=request.contains("offset")?number(request.at("offset")):0;
        const auto limit=request.contains("limit")?number(request.at("limit")):limits_.pageEntities;
        if(!limit||limit>limits_.pageEntities||offset>pinned->entityCount())throw std::invalid_argument("Invalid inspection page bounds");
        out.value["entities"]=Json::array();out.value["total"]=pinned->entityCount();out.value["next_offset"]=nullptr;
        size_t index=offset;
        // Reserve envelope and worst-case cursor width. Serialize each entity once,
        // then the final page once; no full-scene JSON or recursive hierarchy reads.
        size_t bytes=out.value.dump().size()+32;
        while(index<pinned->entityCount()&&index-offset<limit){
            PreparedJson entry;const auto& entity=pinned->entityAt(index);
            entry.value=AuthoringSceneDocument::entityToJson(entity);entry.value["world_matrix"]=pinned->worldMatrix(entity.id);
            const size_t entryBytes=entry.value.dump().size()+1;
            if(bytes+entryBytes>limits_.resultBytes){if(index==offset)throw std::runtime_error("Entity exceeds page byte capacity");break;}
            bytes+=entryBytes;out.value["entities"].push_back(std::move(entry.value));++index;
        }
        if(index<pinned->entityCount())out.value["next_offset"]=index;
    }
    auto wire=out.value.dump();if(wire.size()>limits_.resultBytes)throw std::runtime_error("Inspection result byte capacity exceeded");return wire;
}
std::string AuthoringHost::handle(const Json& request){
    try{
        if(!request.is_object()||!boundedJsonDepth(request)||request.dump().size()>limits_.requestBytes)throw std::invalid_argument("Invalid or oversized request");
        const auto& action=request.at("action").get_ref<const std::string&>();
        if(action=="describe"){fields(request,{"action"});return describe();}
        if(action=="submit")return submit(request);
        if(action=="inspect_scene"||action=="inspect_entity")return inspect(request);
        if(action=="receipt"||action=="cancel"){
            fields(request,{"action","epoch","document_id","request_id"});const auto id=number(request.at("request_id"));
            std::lock_guard lock(mutex_);identity(request);
            const auto found=receipts_.find(id);if(found==receipts_.end())return status(id,epoch_,"expired_or_unknown");
            if(action=="cancel"&&!found->second->done){
                auto response=status(id,epoch_,"cancellation_requested");found->second->cancel=true;wake_.notify_one();return response;
            }
            return found->second->wire;
        }
        throw std::invalid_argument("Unsupported endpoint action");
    }catch(const std::exception& error){return status(0,epoch_,"rejected",error.what());}
}
void AuthoringHost::finish(const std::shared_ptr<Record>& record,std::string& wire)noexcept{
    record->wire.swap(wire);record->done=true;receiptBytes_-=record->accounted;record->accounted=record->wire.size()+record->requestWire.size();receiptBytes_+=record->accounted;
    --pendingCount_;pendingBytes_-=record->bytes;
}
void AuthoringHost::run()noexcept{
    for(;;){
        std::shared_ptr<Record> record;
        {std::unique_lock lock(mutex_);wake_.wait(lock,[&]{return stopping_||!queue_.empty();});
         if(queue_.empty())return;record=queue_.front();queue_.pop_front();}
        process(record);
        // Payload memory is no longer needed by retained receipts.
        clearBoundedJson(record->payload);
    }
}
void AuthoringHost::process(const std::shared_ptr<Record>& record)noexcept{
    std::shared_ptr<const AuthoringSceneDocument> base;
    {std::lock_guard lock(mutex_);if(record->cancel){finish(record,record->cancelled);return;}record->wire.swap(record->preparing);base=document_;}
    const auto prepareStart=Clock::now();
    try{
        auto candidate=std::make_shared<AuthoringSceneDocument>(*base);
        PreparedJson result;result.value=SceneAuthoringAdapter::execute(*candidate,record->operation,record->payload,record->expected);
        const bool preview=record->operation=="preview_transaction";
        std::shared_ptr<const AuthoringSceneDocument> next=candidate;
        auto read=preview?std::shared_ptr<const AuthoringSceneDocument>{}:std::make_shared<const AuthoringSceneDocument>(candidate->readSnapshot());
        PreparedJson terminal;terminal.value={{"request_id",record->id},{"epoch",epoch_},{"document_id",base->sceneId()},
            {"status",preview?"previewed":"applied"},{"revision",preview?base->version():candidate->version()},{"result",std::move(result.value)}};
        auto wire=terminal.value.dump();
        if(wire.size()>limits_.resultBytes)throw std::runtime_error("Receipt result byte capacity exceeded");
        const auto prepareTime=ns(prepareStart);
        // Candidate, read snapshot and serialized receipt are complete. The owner
        // checks cancellation/current identity again at the no-allocation boundary.
        {std::lock_guard lock(mutex_);const auto adoptStart=Clock::now();
         if(record->cancel||stopping_){finish(record,record->cancelled);return;}
         if(document_!=base||base->version()!=record->expected)throw SceneDocumentError("Scene changed before adoption",SceneErrorCode::Conflict);
         if(!preview){document_.swap(next);snapshotSlot_=(snapshotSlot_+1)%snapshots_.size();snapshots_[snapshotSlot_].swap(read);
             std::atomic_store_explicit(&published_,snapshots_[snapshotSlot_],std::memory_order_release);}
         finish(record,wire);prepareNs_=prepareTime;adoptNs_=ns(adoptStart);}
        // Retired documents/history and result storage release outside the lock.
    }catch(const std::exception& error){
        std::string failure;
        try{failure=status(record->id,epoch_,"rejected",error.what());}catch(...){failure.swap(record->failure);}
        std::lock_guard lock(mutex_);if(record->cancel||stopping_)finish(record,record->cancelled);else finish(record,failure);
    }catch(...){std::lock_guard lock(mutex_);finish(record,record->failure);}
}
}
