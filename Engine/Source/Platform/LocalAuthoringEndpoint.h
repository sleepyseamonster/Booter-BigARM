#pragma once
#include "Authoring/AuthoringHost.h"
#include <atomic>
#include <filesystem>
#include <thread>
namespace engine {
// Platform transport only. The host is the domain owner; no socket code reaches
// the renderer, simulation or physics. Destroy endpoint before its host.
class LocalAuthoringEndpoint {
public:
    LocalAuthoringEndpoint(AuthoringHost&,std::filesystem::path socket);
    ~LocalAuthoringEndpoint();
    LocalAuthoringEndpoint(const LocalAuthoringEndpoint&)=delete;
    LocalAuthoringEndpoint& operator=(const LocalAuthoringEndpoint&)=delete;
private:
    AuthoringHost& host_;
    std::filesystem::path path_;
    int listener_=-1;
    uint64_t device_=0,inode_=0;
    std::atomic<bool> stopping_{false};
    std::thread thread_;
    void run()noexcept;
    void serve(int client);
    void removeOwnedSocket()noexcept;
};
}
