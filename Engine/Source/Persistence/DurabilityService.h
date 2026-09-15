#pragma once
#include "Persistence/WorldSave.h"
#include <condition_variable>
#include <deque>
#include <map>
#include <mutex>
#include <thread>

namespace engine {

enum class DurableOperation : uint8_t { Save,Load };
enum class DurableState : uint8_t { Queued,Writing,Durable,Loaded,Superseded,Rejected };

struct DurableReceipt {
    uint64_t requestId=0;
    DurableOperation operation=DurableOperation::Save;
    DurableState state=DurableState::Queued;
    uint64_t sessionEpoch=0,sourceRevision=0,diskGeneration=0;
    std::string error;
    bool terminal()const{return state==DurableState::Durable||state==DurableState::Loaded||state==DurableState::Superseded||state==DurableState::Rejected;}
};

struct DurabilityHooks {
    std::function<void(uint64_t)> beforeSave;
    std::function<void(uint64_t)> beforeLoad;
};

// One bounded worker owns disk I/O. The frame thread submits immutable values
// and polls receipts; durable completion is distinct from request admission.
class DurabilityService {
public:
    explicit DurabilityService(DurabilityHooks hooks={});
    ~DurabilityService();
    DurabilityService(const DurabilityService&)=delete;
    DurabilityService& operator=(const DurabilityService&)=delete;
    uint64_t save(std::filesystem::path profile,WorldSave,uint64_t expectedDiskGeneration,
                  uint64_t sessionEpoch,uint64_t sourceRevision,bool explicitSave=true);
    uint64_t load(std::filesystem::path profile,WorldConfiguration,uint64_t sessionEpoch,uint64_t sourceRevision);
    DurableReceipt receipt(uint64_t requestId)const;
    std::optional<WorldSaveRead> takeLoaded(uint64_t requestId,uint64_t expectedSessionEpoch,uint64_t expectedSourceRevision);
    void shutdown()noexcept;
private:
    struct Request;
    mutable std::mutex mutex_;
    std::condition_variable wake_;
    std::deque<std::shared_ptr<Request>> queue_;
    std::map<uint64_t,std::shared_ptr<Request>> receipts_;
    DurabilityHooks hooks_;
    std::thread worker_;
    uint64_t nextId_=1;
    bool stopping_=false;
    void run()noexcept;
};

}
