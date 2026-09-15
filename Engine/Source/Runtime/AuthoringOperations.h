#pragma once
#include <nlohmann/json.hpp>
#include <cstdint>
#include <deque>
#include <functional>
#include <optional>
#include <stdexcept>
#include <string>
#include <unordered_map>

namespace engine {
using AuthoringJson=nlohmann::json;

enum class AuthoringOperationStatus { Queued, Succeeded, Rejected, Failed };

struct AuthoringOperationRequest {
    std::string type;
    AuthoringJson payload=AuthoringJson::object();
    uint64_t expectedVersion=0;
};

struct AuthoringOperationReceipt {
    uint64_t id=0;
    AuthoringOperationStatus status=AuthoringOperationStatus::Queued;
    uint64_t resultingVersion=0;
    AuthoringJson result=AuthoringJson::object();
    std::string error;
};

// UI-independent command boundary for AI authoring. Handlers run only when the
// owner calls process(), so the real-time loop can choose a bounded budget.
class AuthoringOperations {
public:
    using Handler=std::function<AuthoringJson(const AuthoringJson&,uint64_t,uint64_t&)>;

    explicit AuthoringOperations(size_t maxPending=128,size_t maxReceipts=512)
        :maxPending_(maxPending),maxReceipts_(maxReceipts) {
        if(!maxPending_||!maxReceipts_||maxReceipts_<maxPending_)throw std::invalid_argument("Authoring operation limits are invalid");
    }
    void registerHandler(std::string type,Handler handler) {
        if(type.empty()||type.size()>128||!handler)throw std::invalid_argument("Invalid authoring operation handler");
        handlers_[std::move(type)]=std::move(handler);
    }
    uint64_t enqueue(AuthoringOperationRequest request) {
        if(request.type.empty()||request.type.size()>128||!request.payload.is_object())throw std::invalid_argument("Invalid authoring operation request");
        if(request.payload.dump().size()>256*1024)throw std::invalid_argument("Authoring operation payload exceeds 256 KiB");
        if(queue_.size()>=maxPending_)throw std::runtime_error("Authoring operation queue full");
        if(nextId_==UINT64_MAX)throw std::overflow_error("Authoring operation id exhausted");
        const uint64_t id=nextId_++;
        queue_.push_back({id,std::move(request)});
        receipts_[id]=AuthoringOperationReceipt{id,AuthoringOperationStatus::Queued,0,AuthoringJson::object(),{}};
        receiptOrder_.push_back(id);trimReceipts();
        return id;
    }
    size_t process(size_t budget=8) {
        if(budget==0)return 0;
        size_t completed=0;
        while(completed<budget&&!queue_.empty()) {
            auto work=std::move(queue_.front());queue_.pop_front();
            auto receipt=receipts_.find(work.id);if(receipt==receipts_.end())continue;
            const auto handler=handlers_.find(work.request.type);
            if(handler==handlers_.end()) {
                receipt->second.status=AuthoringOperationStatus::Rejected;
                receipt->second.error="Unknown authoring operation type";
                ++completed;continue;
            }
            try {
                uint64_t version=work.request.expectedVersion;
                receipt->second.result=handler->second(work.request.payload,work.request.expectedVersion,version);
                if(!receipt->second.result.is_object())throw std::runtime_error("Authoring operation result must be an object");
                if(version<work.request.expectedVersion)throw std::runtime_error("Authoring operation version moved backwards");
                receipt->second.resultingVersion=version;
                receipt->second.status=AuthoringOperationStatus::Succeeded;
            } catch(const std::exception& error) {
                receipt->second.status=AuthoringOperationStatus::Failed;
                receipt->second.error=error.what();
            } catch(...) {
                receipt->second.status=AuthoringOperationStatus::Failed;
                receipt->second.error="Unknown authoring operation failure";
            }
            ++completed;
        }
        return completed;
    }
    std::optional<AuthoringOperationReceipt> receipt(uint64_t id)const {
        const auto found=receipts_.find(id);if(found==receipts_.end())return {};
        return found->second;
    }
    size_t pending()const{return queue_.size();}
private:
    struct Work {uint64_t id;AuthoringOperationRequest request;};
    size_t maxPending_,maxReceipts_;uint64_t nextId_=1;
    std::deque<Work> queue_;std::unordered_map<std::string,Handler> handlers_;
    std::unordered_map<uint64_t,AuthoringOperationReceipt> receipts_;std::deque<uint64_t> receiptOrder_;
    void trimReceipts() {
        while(receiptOrder_.size()>maxReceipts_) {receipts_.erase(receiptOrder_.front());receiptOrder_.pop_front();}
    }
};
}
