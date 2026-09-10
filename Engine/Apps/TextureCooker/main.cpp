#include "Assets/TextureData.h"
#include <bimg/decode.h>
#include <bx/allocator.h>
#include <bx/error.h>
#include <bx/file.h>
#include <fstream>
#include <iostream>
#include <memory>
#include <stdexcept>
int main(int argc,char** argv) {
    try {
        if (argc!=4) throw std::runtime_error("Usage: engine_texture_cook input.png output.ktx semantic-role");
        if (std::filesystem::exists(argv[2])) throw std::runtime_error("Cook output already exists");
        const auto role=engine::textureRole(argv[3]);
        const auto source=engine::readTextureFile(argv[1]);
        // Inspect dimensions before invoking the general decoder; only transferred PNG8 inputs are admitted.
        const uint8_t signature[]={137,80,78,71,13,10,26,10};
        if (source.size()<33 || !std::equal(std::begin(signature),std::end(signature),source.begin()) ||
            source[8]!=0 || source[9]!=0 || source[10]!=0 || source[11]!=13 ||
            source[12]!='I' || source[13]!='H' || source[14]!='D' || source[15]!='R' || source[26]!=0 || source[27]!=0 ||
            source[24]!=8 || (source[25]!=0 && source[25]!=2 && source[25]!=4 && source[25]!=6) || source[28]!=0)
            throw std::runtime_error("Cooker accepts noninterlaced PNG8 grayscale/RGB/RGBA only");
        auto be=[&](int at) {return uint32_t(source[at])<<24|uint32_t(source[at+1])<<16|uint32_t(source[at+2])<<8|source[at+3];};
        const auto width=be(16),height=be(20);
        if (!width || !height || width>2048 || height>2048) throw std::runtime_error("PNG dimensions exceed cook profile");
        bx::DefaultAllocator allocator;bx::Error error;
        std::unique_ptr<bimg::ImageContainer,decltype(&bimg::imageFree)> image(
            bimg::imageParse(&allocator,source.data(),uint32_t(source.size()),bimg::TextureFormat::RGBA8,&error),&bimg::imageFree);
        if (!image || !error.isOk() || image->m_width!=width || image->m_height!=height || image->m_size!=width*height*4)
            throw std::runtime_error("PNG decode failed or dimensions disagree");
        const auto* pixels=static_cast<const uint8_t*>(image->m_data);
        const auto data=engine::semanticMips({width,height,{pixels,pixels+width*height*4}},role);
        std::vector<uint8_t> packed; packed.reserve(data.bytes());
        for (const auto& level:data.levels)packed.insert(packed.end(),level.rgba.begin(),level.rgba.end());
        bx::FileWriter writer;
        if (!bx::open(&writer,argv[2],false,&error)) throw std::runtime_error("Cannot open cooked texture output");
        bimg::imageWriteKtx(&writer,bimg::TextureFormat::RGBA8,false,width,height,0,uint8_t(data.levels.size()),0,data.srgb,packed.data(),&error);
        bx::close(&writer);
        if (!error.isOk()) throw std::runtime_error("KTX write failed");
        auto bytes=engine::readTextureFile(argv[2]);
        // This pinned bimg writer leaves glType/glFormat zero for RGBA8. Canonicalize
        // the uncompressed KTX1 header; payload and mip packing still come from bimg.
        auto word=[&](size_t at,uint32_t value) {for (int i=0;i<4;++i)bytes.at(at+i)=uint8_t(value>>(i*8));};
        word(16,0x1401);word(24,0x1908);
        {std::ofstream output(argv[2],std::ios::binary|std::ios::trunc);output.write(reinterpret_cast<const char*>(bytes.data()),std::streamsize(bytes.size()));
         if (!output) throw std::runtime_error("Cannot finalize canonical KTX header");}
        const auto parsed=engine::parseTexture(bytes,role);
        bimg::ImageContainer independent{};
        if (!bimg::imageParse(independent,bytes.data(),uint32_t(bytes.size()),&error) || !error.isOk() ||
            independent.m_width!=width || independent.m_height!=height || independent.m_numMips!=data.levels.size())
            throw std::runtime_error("Generic bimg KTX readback failed");
        std::cout<<"{\"width\":"<<width<<",\"height\":"<<height<<",\"mips\":"<<parsed.levels.size()<<",\"resident_bytes\":"<<data.bytes()<<"}\n";
    } catch(const std::exception& error) {std::cerr<<error.what()<<'\n';return 1;}
}
