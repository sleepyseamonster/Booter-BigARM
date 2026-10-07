#include "Rendering/TextureStore.h"
#include <atomic>
#include <stdexcept>
#include <utility>
#include <vector>
namespace engine {
struct TextureStoreState {
    struct Slot {std::string key;uint64_t generation=0;size_t references=0,bytes=0;bgfx::TextureHandle gpu=BGFX_INVALID_HANDLE;};
    bool alive=true;size_t bytes=0,budget=0;std::vector<Slot> slots;
};
namespace {
// Process-wide monotonic tokens also reject tokens accidentally passed between stores.
std::atomic<uint64_t> nextGeneration{1};
TextureStoreState::Slot& checked(TextureStoreState& state,TextureToken token) {
    if (!state.alive || token.slot>=state.slots.size()) throw std::runtime_error("Invalid texture token");
    auto& slot=state.slots[token.slot];
    if (!slot.references || slot.generation!=token.generation) throw std::runtime_error("Stale texture generation");
    return slot;
}
}
TextureLease::~TextureLease() {reset();}
TextureLease::TextureLease(TextureLease&& other) noexcept:owner_(std::move(other.owner_)),token_(std::exchange(other.token_,{})) {}
TextureLease& TextureLease::operator=(TextureLease&& other) noexcept {
    if (this!=&other) {reset();owner_=std::move(other.owner_);token_=std::exchange(other.token_,{});}return *this;
}
void TextureLease::reset() {
    if (auto state=owner_.lock();state && state->alive && token_.slot<state->slots.size()) {
        auto& slot=state->slots[token_.slot];
        if (slot.generation==token_.generation && slot.references && --slot.references==0) {
            bgfx::destroy(slot.gpu);slot.gpu=BGFX_INVALID_HANDLE;state->bytes-=slot.bytes;slot.bytes=0;slot.key.clear();
        }
    }
    token_={};owner_.reset();
}
TextureStore::TextureStore(size_t budget):state_(std::make_shared<TextureStoreState>()) {state_->budget=budget;}
TextureStore::~TextureStore() {stop();}
TextureLease TextureStore::upload(const std::string& key,const TextureData& data) {
    if (!state_->alive) throw std::runtime_error("Texture store is stopped");
    for (uint32_t i=0;i<state_->slots.size();++i) {
        auto& slot=state_->slots[i];
        if (slot.references && slot.key==key) {
            ++slot.references;TextureLease lease;lease.owner_=state_;lease.token_={i,slot.generation};return lease;
        }
    }
    const size_t bytes=data.bytes();
    if (bytes>textureFileLimit || bytes>state_->budget-state_->bytes) throw std::runtime_error("Texture residency budget exceeded");
    uint32_t index=0;while(index<state_->slots.size() && state_->slots[index].references)++index;
    if (index==state_->slots.size()) state_->slots.emplace_back();
    const uint64_t flags=data.srgb?BGFX_TEXTURE_SRGB:0;
    if (!bgfx::isTextureValid(0,false,1,bgfx::TextureFormat::RGBA8,flags)) throw std::runtime_error("RGBA8 texture profile unsupported");
    std::vector<uint8_t> packed;packed.reserve(bytes);
    for (const auto& level:data.levels) packed.insert(packed.end(),level.rgba.begin(),level.rgba.end());
    const auto& base=data.levels.at(0);
    const auto gpu=bgfx::createTexture2D(uint16_t(base.width),uint16_t(base.height),data.levels.size()>1,1,bgfx::TextureFormat::RGBA8,flags,
        bgfx::copy(packed.data(),uint32_t(packed.size())));
    if (!bgfx::isValid(gpu)) throw std::runtime_error("Texture upload failed");
    auto& slot=state_->slots[index];slot={key,nextGeneration.fetch_add(1),1,bytes,gpu};state_->bytes+=bytes;
    bgfx::setName(gpu,key.c_str());
    TextureLease lease;lease.owner_=state_;lease.token_={index,slot.generation};return lease;
}
TextureLease TextureStore::acquire(const TextureRecord& record) {
    // Validate even a shared request: edited/corrupt records cannot alias an existing allocation.
    auto data=loadTexture(record);
    return upload(record.key+":"+record.sha256+":"+std::to_string(int(record.role)),data);
}
TextureLease TextureStore::fallback(TextureRole role) {
    std::vector<uint8_t> pixel;
    switch(role) {
        case TextureRole::Color:pixel={255,0,255,255};break;
        case TextureRole::Normal:pixel={128,128,255,255};break;
        case TextureRole::Surface:pixel={255,128,0,255};break;
        default:pixel={0,0,0,255};break;
    }
    return upload("fallback:"+std::to_string(int(role)),{role==TextureRole::Color,{{1,1,pixel}}});
}
bgfx::TextureHandle TextureStore::resolve(TextureToken token) const {return checked(*state_,token).gpu;}
size_t TextureStore::bytes() const {return state_->bytes;}
size_t TextureStore::count() const {size_t count=0;for(const auto& slot:state_->slots)count+=slot.references>0;return count;}
size_t TextureStore::references(TextureToken token) const {return checked(*state_,token).references;}
void TextureStore::stop() {
    if (!state_->alive)return;
    for (auto& slot:state_->slots)if(slot.references) {bgfx::destroy(slot.gpu);slot.gpu=BGFX_INVALID_HANDLE;slot.references=0;}
    state_->bytes=0;state_->alive=false;
}
}
