#include "Rendering/TextureChecks.h"
#include <stdexcept>
namespace engine {
Json verifyTextureStore(TextureStore& store,const TextureRecord& record) {
    auto require=[](bool condition,const char* message) {if(!condition)throw std::runtime_error(message);};
    auto rejects=[&](auto action) {bool caught=false;try {action();}catch(const std::exception&){caught=true;}require(caught,"Expected texture rejection");};
    bgfx::frame();bgfx::frame();
    const auto gpuBefore=bgfx::getStats()->numTextures;
    const auto bytesBefore=store.bytes(),countBefore=store.count();
    auto a=store.acquire(record),b=store.acquire(record);const auto token=a.token();
    require(a.token()==b.token() && store.references(token)==2,"Shared texture requests must share storage");
    require(store.bytes()==bytesBefore+record.residentBytes && store.count()==countBefore+1,"Shared byte accounting");
    a.reset();require(store.references(token)==1 && bgfx::isValid(store.resolve(token)),"One owner release must retain GPU texture");
    auto bad=record;bad.crc32^=1;
    rejects([&]{b=store.acquire(bad);});require(b.token()==token,"Failed replacement must preserve prior lease");
    bad=record;bad.role=record.role==TextureRole::Color?TextureRole::Normal:TextureRole::Color;
    rejects([&]{store.acquire(bad);});
    bad=record;bad.file+=".missing";rejects([&]{store.acquire(bad);});
    b.reset();rejects([&]{store.resolve(token);});
    for (int i=0;i<100;++i) {
        auto replacement=store.acquire(record);require(replacement.token().generation!=token.generation,"Slot reuse must change generation");
        replacement.reset();bgfx::frame();bgfx::frame();
        require(store.bytes()==bytesBefore && store.count()==countBefore,"Release must restore logical residency");
    }
    for (auto role:{TextureRole::Color,TextureRole::Normal,TextureRole::Surface,TextureRole::Height,TextureRole::Mask}) {
        auto fallback=store.fallback(role);require(bgfx::isValid(store.resolve(fallback.token())),"Role fallback allocation failed");
    }
    {TextureStore limited(1);rejects([&]{limited.acquire(record);});require(!limited.count() && !limited.bytes(),"Budget rejection allocated texture");}
    bgfx::frame();bgfx::frame();
    const auto gpuAfter=bgfx::getStats()->numTextures;
    require(gpuAfter==gpuBefore,"GPU texture count did not return after release cycles");
    return {{"shared_references",2},{"replacement_cycles",100},{"gpu_textures_before",gpuBefore},{"gpu_textures_after",gpuAfter},
        {"bytes_after",store.bytes()},{"fallback_roles",5},{"failure_preserves_prior",true},{"stale_tokens_rejected",true},{"passed",true}};
}
}
