#pragma once
#include "Authoring/SceneDocument.h"
#include <array>
#include <atomic>
#include <condition_variable>
#include <deque>
#include <map>
#include <mutex>
#include <thread>

namespace engine {
struct AuthoringLimits {
    size_t requestBytes=256*1024,pendingCount=128,pendingBytes=8*1024*1024;
    size_t resultBytes=64*1024,receiptCount=512,receiptBytes=8*1024*1024,pageEntities=64;
};
// All preparation/serialization runs outside the frame loop. One owner publishes
// a complete candidate and a pre-serialized receipt in the same critical section.
// Consumers retain read snapshots; they never borrow mutable document pointers.
class AuthoringHost {
public:
    explicit AuthoringHost(AuthoringSceneDocument initial=AuthoringSceneDocument{},AuthoringLimits limits={});
    ~AuthoringHost();
    AuthoringHost(const AuthoringHost&)=delete;AuthoringHost& operator=(const AuthoringHost&)=delete;
    void start();
    void stop()noexcept;
    std::string handle(const Json& request);
    std::shared_ptr<const AuthoringSceneDocument> snapshot()const;
    const std::string& epoch()const noexcept{return epoch_;}
    const AuthoringLimits& limits()const noexcept{return limits_;}
private:
    struct Record;
    AuthoringLimits limits_;
    std::string epoch_;
    mutable std::mutex mutex_;
    std::condition_variable wake_;
    std::shared_ptr<const AuthoringSceneDocument> document_;
    std::shared_ptr<const AuthoringSceneDocument> published_;
    std::array<std::shared_ptr<const AuthoringSceneDocument>,4> snapshots_{};
    size_t snapshotSlot_=0;
    std::map<uint64_t,std::shared_ptr<Record>> receipts_;
    std::deque<std::shared_ptr<Record>> queue_;
    uint64_t highWater_=0,prepareNs_=0,adoptNs_=0;
    size_t pendingCount_=0,pendingBytes_=0,receiptBytes_=0;
    bool started_=false,stopping_=false;
    std::thread worker_;
    void run()noexcept;
    void process(const std::shared_ptr<Record>&)noexcept;
    void finish(const std::shared_ptr<Record>&,std::string&)noexcept;
    void identity(const Json&)const;
    std::string submit(const Json&);
    std::string inspect(const Json&);
    std::string describe()const;
};
// Strict bounded JSON at the transport boundary, including duplicate keys/depth.
Json parseAuthoringRequest(const std::string& wire,size_t maxBytes=256*1024);
}
