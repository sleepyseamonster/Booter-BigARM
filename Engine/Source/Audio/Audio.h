#pragma once
#include <cstdint>
#include <memory>
#include <vector>
namespace engine {
class Audio {
public:
    explicit Audio(bool offline=false);
    ~Audio();
    Audio(const Audio&)=delete;
    Audio& operator=(const Audio&)=delete;
    bool cue(uint64_t sequence);
    uint64_t delivered() const;
    // Device-free output for a bounded native audio check; not speaker proof.
    std::vector<float> render(uint32_t frames);
private:
    struct Impl;std::unique_ptr<Impl> impl_;
};
}
