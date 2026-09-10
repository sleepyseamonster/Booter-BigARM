#pragma once
#include "Assets/TextureCatalog.h"
#include <bgfx/bgfx.h>
#include <memory>
namespace engine {
struct TextureStoreState;
struct TextureToken {uint32_t slot=UINT32_MAX;uint64_t generation=0;bool operator==(const TextureToken&)const=default;};
class TextureLease {
public:
    TextureLease()=default;
    ~TextureLease();
    TextureLease(TextureLease&&) noexcept;
    TextureLease& operator=(TextureLease&&) noexcept;
    TextureLease(const TextureLease&)=delete;
    TextureLease& operator=(const TextureLease&)=delete;
    TextureToken token() const {return token_;}
    void reset();
private:
    friend class TextureStore;
    std::weak_ptr<TextureStoreState> owner_;
    TextureToken token_;
};
// Render-thread owner. Synchronous first-pass admission; logical IDs never store these tokens.
class TextureStore {
public:
    explicit TextureStore(size_t budget=256*1024*1024);
    ~TextureStore();
    TextureStore(const TextureStore&)=delete;
    TextureStore& operator=(const TextureStore&)=delete;
    TextureLease acquire(const TextureRecord& record);
    TextureLease fallback(TextureRole role);
    bgfx::TextureHandle resolve(TextureToken token) const;
    size_t bytes() const;
    size_t count() const;
    size_t references(TextureToken token) const;
    void stop();
private:
    std::shared_ptr<TextureStoreState> state_;
    TextureLease upload(const std::string& key,const TextureData& data);
};
}
