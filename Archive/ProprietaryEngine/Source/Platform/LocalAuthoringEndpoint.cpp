#include "Platform/LocalAuthoringEndpoint.h"
#include "Persistence/JsonLifetime.h"
#include <chrono>
#include <stdexcept>
#if defined(__APPLE__)
#include <cerrno>
#include <cstring>
#include <fcntl.h>
#include <poll.h>
#include <sys/socket.h>
#include <sys/stat.h>
#include <sys/un.h>
#include <unistd.h>
#endif
namespace engine {
#if defined(__APPLE__)
namespace {
using Clock=std::chrono::steady_clock;
struct Descriptor {int value;~Descriptor(){if(value>=0)::close(value);}};
void nonblocking(int fd){if(fcntl(fd,F_SETFL,O_NONBLOCK)<0||fcntl(fd,F_SETFD,FD_CLOEXEC)<0)throw std::runtime_error("Cannot configure local socket");}
bool ready(int fd,short event,Clock::time_point deadline,const std::atomic<bool>& stopped){
    while(!stopped&&Clock::now()<deadline){pollfd wait{fd,event,0};const int result=::poll(&wait,1,50);
        if(result>0)return (wait.revents&event)!=0;
        if(result<0&&errno!=EINTR)return false;
    }return false;
}
}
LocalAuthoringEndpoint::LocalAuthoringEndpoint(AuthoringHost& host,std::filesystem::path socket):host_(host),path_(std::move(socket)){
    if(!path_.is_absolute())throw std::invalid_argument("Authoring socket must be an absolute path");
    const auto name=path_.string();sockaddr_un address{};
    if(name.size()>=sizeof(address.sun_path)||name.find('\0')!=std::string::npos)throw std::invalid_argument("Authoring socket path too long or invalid");
    struct stat parent{};
    if(::lstat(path_.parent_path().c_str(),&parent)!=0||!S_ISDIR(parent.st_mode)||parent.st_uid!=::geteuid()||(parent.st_mode&077)!=0)
        throw std::invalid_argument("Authoring socket needs an existing user-owned private directory (0700)");
    listener_=::socket(AF_UNIX,SOCK_STREAM,0);
    if(listener_<0)throw std::runtime_error("Cannot create authoring socket");
    try{
        nonblocking(listener_);address.sun_family=AF_UNIX;std::memcpy(address.sun_path,name.c_str(),name.size()+1);
        // Never remove/replace an existing endpoint, including stale ones.
        if(::bind(listener_,reinterpret_cast<sockaddr*>(&address),sizeof(address))!=0)throw std::runtime_error("Cannot bind authoring socket; select an unused endpoint");
        struct stat own{};
        if(::lstat(path_.c_str(),&own)!=0)throw std::runtime_error("Cannot identify authoring socket");
        device_=own.st_dev;inode_=own.st_ino;
        if(::chmod(path_.c_str(),0600)!=0||::listen(listener_,16)!=0)throw std::runtime_error("Cannot protect/listen on authoring socket");
        thread_=std::thread([this]{run();});
    }catch(...){::close(listener_);listener_=-1;removeOwnedSocket();throw;}
}
void LocalAuthoringEndpoint::removeOwnedSocket()noexcept{
    struct stat own{};
    if(inode_&&::lstat(path_.c_str(),&own)==0&&S_ISSOCK(own.st_mode)&&uint64_t(own.st_dev)==device_&&uint64_t(own.st_ino)==inode_)::unlink(path_.c_str());
}
LocalAuthoringEndpoint::~LocalAuthoringEndpoint(){
    stopping_=true;if(thread_.joinable())thread_.join();if(listener_>=0)::close(listener_);removeOwnedSocket();
}
void LocalAuthoringEndpoint::run()noexcept{
    while(!stopping_){
        if(!ready(listener_,POLLIN,Clock::now()+std::chrono::milliseconds(100),stopping_))continue;
        Descriptor client{::accept(listener_,nullptr,nullptr)};if(client.value<0)continue;
        try{nonblocking(client.value);int enabled=1;::setsockopt(client.value,SOL_SOCKET,SO_NOSIGPIPE,&enabled,sizeof(enabled));serve(client.value);}catch(...){}
    }
}
void LocalAuthoringEndpoint::serve(int client){
    const auto deadline=Clock::now()+std::chrono::seconds(2);
    std::string request;request.reserve(4096);bool complete=false;
    while(ready(client,POLLIN,deadline,stopping_)){
        char buffer[4096];const auto count=::recv(client,buffer,sizeof(buffer),0);
        if(count<=0){if(count<0&&(errno==EAGAIN||errno==EINTR))continue;return;}
        request.append(buffer,size_t(count));
        if(request.size()>host_.limits().requestBytes+1)return;
        if(auto end=request.find('\n');end!=std::string::npos){if(end+1!=request.size())return;request.resize(end);complete=true;break;}
    }
    if(!complete)return;
    std::string response;
    try{auto parsed=parseAuthoringRequest(request,host_.limits().requestBytes);JsonReleaseGuard release{parsed};response=host_.handle(parsed);}
    catch(...){response=R"({"status":"rejected","error":"Invalid bounded JSON request"})";}
    if(response.size()>host_.limits().resultBytes)return;
    response.push_back('\n');size_t sent=0;
    while(sent<response.size()&&ready(client,POLLOUT,deadline,stopping_)){
        const auto count=::send(client,response.data()+sent,response.size()-sent,0);
        if(count<0&&(errno==EAGAIN||errno==EINTR))continue;
        if(count<=0)return;sent+=size_t(count);
    }
}
#else
// Explicit unsupported transport until native W01/R6 named-pipe validation. The
// domain/owner API remains portable; never silently create a TCP listener.
LocalAuthoringEndpoint::LocalAuthoringEndpoint(AuthoringHost& host,std::filesystem::path socket):host_(host),path_(std::move(socket)){
    throw std::runtime_error("Local authoring transport is not yet implemented for this platform");
}
LocalAuthoringEndpoint::~LocalAuthoringEndpoint()=default;
void LocalAuthoringEndpoint::run()noexcept{}
void LocalAuthoringEndpoint::serve(int){}
void LocalAuthoringEndpoint::removeOwnedSocket()noexcept{}
#endif
}
