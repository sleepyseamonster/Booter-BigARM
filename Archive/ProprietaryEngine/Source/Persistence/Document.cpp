#include "Persistence/Document.h"
#include <fstream>
#include <set>
#include <vector>
#include <stdexcept>
#include <cstdio>
#ifdef _WIN32
#define NOMINMAX
#include <windows.h>
#else
#include <fcntl.h>
#include <unistd.h>
#endif

namespace engine {
namespace {
Json parseBounded(const std::string& bytes) {
    if (bytes.size()>documentLimit) throw std::runtime_error("Document exceeds 1 MiB limit");
    std::vector<std::set<std::string>> keys;
    return Json::parse(bytes,[&](int depth,Json::parse_event_t event,Json& value) {
        if (depth>32) throw std::runtime_error("Document nesting exceeds 32 levels");
        if (event==Json::parse_event_t::object_start) keys.emplace_back();
        if (event==Json::parse_event_t::key && !keys.back().insert(value.get<std::string>()).second)
            throw std::runtime_error("Duplicate document key");
        if (event==Json::parse_event_t::object_end) keys.pop_back();
        return true;
    });
}
void replace(const std::filesystem::path& target,const std::string& bytes) {
    if (target.filename().empty()) throw std::runtime_error("Document needs a filename");
    // Exclusive sibling lock prevents two engine writers from racing or truncating each other.
    auto temporary=target; temporary += ".writing";
#ifdef _WIN32
    HANDLE file=CreateFileW(temporary.c_str(),GENERIC_WRITE,0,nullptr,CREATE_NEW,FILE_ATTRIBUTE_NORMAL,nullptr);
    if (file==INVALID_HANDLE_VALUE) throw std::runtime_error("Cannot create document staging file (writer active or stale .writing file)");
    DWORD written=0;
    const bool saved=WriteFile(file,bytes.data(),DWORD(bytes.size()),&written,nullptr) && written==bytes.size() && FlushFileBuffers(file);
    CloseHandle(file);
    const bool moved=saved && MoveFileExW(temporary.c_str(),target.c_str(),MOVEFILE_REPLACE_EXISTING|MOVEFILE_WRITE_THROUGH);
#else
    int file=::open(temporary.c_str(),O_WRONLY|O_CREAT|O_EXCL,0600);
    if (file<0) throw std::runtime_error("Cannot create document staging file (writer active or stale .writing file)");
    size_t position=0;
    while (position<bytes.size()) {
        const auto count=::write(file,bytes.data()+position,bytes.size()-position);
        if (count<0 && errno==EINTR) continue;
        if (count<=0) break;
        position+=size_t(count);
    }
    const bool saved=position==bytes.size() && ::fsync(file)==0;
    const bool closed=::close(file)==0;
    const bool moved=saved && closed && ::rename(temporary.c_str(),target.c_str())==0;
#endif
    if (!moved) { std::error_code ignored; std::filesystem::remove(temporary,ignored); throw std::runtime_error("Document replacement failed; previous document retained"); }
    // Atomic replacement, not a multi-file save transaction or power-loss recovery guarantee.
}
}
Json readDocument(const std::filesystem::path& path,std::string_view kind,unsigned version) {
    std::ifstream input(path,std::ios::binary);
    if (!input) throw std::runtime_error("Cannot open document: "+path.string());
    std::string bytes(documentLimit+1,'\0');
    input.read(bytes.data(),static_cast<std::streamsize>(bytes.size())); bytes.resize(size_t(input.gcount()));
    if (input.bad()) throw std::runtime_error("Document read failed");
    const auto document=parseBounded(bytes);
    if (!document.is_object() || document.size()!=3 || !document.at("kind").is_string() ||
        !document.at("version").is_number_unsigned() || !document.at("payload").is_object())
        throw std::runtime_error("Invalid document envelope");
    if(document.at("kind")!=kind || document.at("version")!=version)
        throw DocumentCompatibilityError("Unsupported document kind or version");
    return document.at("payload");
}
void writeDocument(const std::filesystem::path& path,std::string_view kind,const Json& payload,unsigned version) {
    if (kind.empty() || !version || !payload.is_object()) throw std::invalid_argument("Invalid document envelope");
    Json document={{"kind",kind},{"version",version},{"payload",payload}};
    const std::string bytes=document.dump(2)+"\n";
    // dump replaces nonfinite values with null; reject that lossy conversion and excessive nesting.
    if (parseBounded(bytes)!=document) throw std::invalid_argument("Document contains nonfinite values");
    replace(path,bytes);
}
std::filesystem::path contentPath(const std::filesystem::path& root,std::string_view relative) {
    if (relative.empty() || relative.find('\\')!=relative.npos || relative.find(':')!=relative.npos || relative.find('\0')!=relative.npos)
        throw std::invalid_argument("Invalid content reference");
    const std::filesystem::path part(relative);
    if (part.is_absolute()) throw std::invalid_argument("Content reference must be relative");
    for (const auto& component:part) if (component==".." || component==".") throw std::invalid_argument("Content traversal rejected");
    const auto base=std::filesystem::canonical(root), target=std::filesystem::weakly_canonical(base/part);
    auto b=base.begin(), t=target.begin();
    for (;b!=base.end();++b,++t) if (t==target.end() || *b!=*t) throw std::invalid_argument("Content reference escapes root");
    return target;
}
}
