// Bounded offline probe, not a runtime loader or an untrusted-file validator.
#include <bimg/bimg.h>
#include <bx/allocator.h>
#include <bx/error.h>
#include <fstream>
#include <iostream>
#include <iterator>
#include <vector>

int main(int argc, char** argv) {
    if (argc != 2) return 2;
    std::ifstream input(argv[1], std::ios::binary);
    std::vector<char> bytes((std::istreambuf_iterator<char>(input)), {});
    if (bytes.empty() || bytes.size() > 64 * 1024 * 1024) return 3;
    bx::DefaultAllocator allocator;
    bx::Error error;
    bimg::ImageContainer container{};
    if (!bimg::imageParse(container, bytes.data(), uint32_t(bytes.size()), &error)
        || !error.isOk() || container.m_cubeMap || container.m_numLayers != 1
        || container.m_depth > 1 || container.m_width > 2048 || container.m_height > 2048) return 4;
    std::cout << "{\"width\":" << container.m_width << ",\"height\":" << container.m_height
              << ",\"format\":\"" << bimg::getName(container.m_format)
              << "\",\"srgb\":" << (container.m_srgb ? "true" : "false") << ",\"mips\":[";
    for (uint8_t level = 0; level < container.m_numMips; ++level) {
        bimg::ImageMip mip{};
        if (!bimg::imageGetRawData(container, 0, level, bytes.data(), uint32_t(bytes.size()), mip)) return 5;
        std::vector<uint8_t> rgba(size_t(mip.m_width) * mip.m_height * 4);
        bimg::imageDecodeToRgba8(&allocator, rgba.data(), mip.m_data,
                               mip.m_width, mip.m_height, mip.m_width * 4, mip.m_format);
        if (level) std::cout << ',';
        std::cout << "{\"width\":" << mip.m_width << ",\"height\":" << mip.m_height << ",\"mean\":[";
        for (size_t channel = 0; channel < 4; ++channel) {
            uint64_t sum = 0;
            for (size_t i = channel; i < rgba.size(); i += 4) sum += rgba[i];
            if (channel) std::cout << ',';
            std::cout << double(sum) / double(rgba.size() / 4);
        }
        std::cout << "],\"first\":[";
        for (size_t channel = 0; channel < 4; ++channel) {
            if (channel) std::cout << ',';
            std::cout << unsigned(rgba[channel]);
        }
        std::cout << "]}";
    }
    std::cout << "]}\n";
}
