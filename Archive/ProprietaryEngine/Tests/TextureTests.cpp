#include "Assets/TextureData.h"
#include "Assets/TextureCatalog.h"
#include "Persistence/Document.h"
#include <cmath>
#include <fstream>
#include <iostream>
#include <stdexcept>
using namespace engine;
void require(bool condition,const char* message){if(!condition)throw std::runtime_error(message);}
template<class F>void rejects(F action){bool rejected=false;try{action();}catch(const std::exception&){rejected=true;}require(rejected,"Expected texture rejection");}
std::vector<uint8_t> container(const TextureData& data) {
    std::vector<uint8_t> out{0xab,0x4b,0x54,0x58,0x20,0x31,0x31,0xbb,0x0d,0x0a,0x1a,0x0a};
    auto word=[&](uint32_t v){for(int i=0;i<4;++i)out.push_back(uint8_t(v>>(8*i)));};
    for(auto v:{0x04030201u,0x1401u,1u,0x1908u,data.srgb?0x8c43u:0x8058u,0x1908u,data.levels[0].width,data.levels[0].height,0u,0u,1u,uint32_t(data.levels.size()),0u})word(v);
    for(const auto& mip:data.levels){word(uint32_t(mip.rgba.size()));out.insert(out.end(),mip.rgba.begin(),mip.rgba.end());}return out;
}
int main(int argc,char** argv) {
    try {
        require(argc==2,"Expected output directory");std::filesystem::path root=argv[1];std::filesystem::create_directories(root);
        ImageLevel checker{2,1,{0,0,0,0,255,255,255,255}};
        auto color=semanticMips(checker,TextureRole::Color),linear=semanticMips(checker,TextureRole::Surface);
        require(color.levels.back().rgba==std::vector<uint8_t>({188,188,188,128}),"sRGB RGB and independent linear alpha");
        require(linear.levels.back().rgba==std::vector<uint8_t>({128,128,128,128}),"Packed numerical channels stay linear");
        auto normals=semanticMips({2,1,{255,128,128,255,0,127,127,255}},TextureRole::Normal);
        require(normals.levels.back().rgba==std::vector<uint8_t>({128,128,255,255}),"Opposed normal vectors use +Z fallback");
        auto blended=semanticMips({2,1,{255,128,128,255,128,128,255,255}},TextureRole::Normal).levels.back().rgba;
        require(std::abs(int(blended[0])-218)<=1 && std::abs(int(blended[2])-218)<=1,"Normal mip renormalization");
        ImageLevel odd{5,3,std::vector<uint8_t>(5*3*4,0)};odd.rgba[56]=255;
        auto tail=semanticMips(odd,TextureRole::Height);
        require(tail.levels.size()==3 && tail.levels[1].width==2 && tail.levels[1].height==1 && tail.levels.back().rgba[0]==17,"Odd tail contributes exact area average");
        auto vertical=semanticMips({1,3,{0,0,0,255,0,0,0,255,255,255,255,255}},TextureRole::Height);
        require(vertical.levels.back().rgba[0]==85,"One by N tail coverage");
        auto horizontal=semanticMips({3,1,{0,0,0,255,0,0,0,255,255,255,255,255}},TextureRole::Height);
        require(horizontal.levels.back().rgba==vertical.levels.back().rgba,"N by one symmetry");
        auto tiled=semanticMips({4,1,{0,0,0,0,255,255,255,255,0,0,0,0,255,255,255,255}},TextureRole::Color);
        require(tiled.levels[1].rgba==std::vector<uint8_t>({188,188,188,128,188,188,188,128}),"Periodic box footprints repeat without edge clamp bias");
        rejects([]{semanticMips({0,1,{}},TextureRole::Color);});rejects([]{textureRole("orm");});
        auto bytes=container(color);require(parseTexture(bytes,TextureRole::Color).levels.back().rgba==color.levels.back().rgba,"Strict KTX roundtrip");
        rejects([&]{parseTexture(bytes,TextureRole::Normal);});
        for(size_t count=0;count<bytes.size();++count)rejects([&]{parseTexture(std::span(bytes).first(count),TextureRole::Color);});
        for(size_t offset:{size_t(0),size_t(12),size_t(16),size_t(20),size_t(24),size_t(28),size_t(32),size_t(44),size_t(48),size_t(52),size_t(56),size_t(60),size_t(64)}){
            auto bad=bytes;bad[offset]^=0x40;rejects([&]{parseTexture(bad,TextureRole::Color);});
        }
        auto trailing=bytes;trailing.push_back(0);rejects([&]{parseTexture(trailing,TextureRole::Color);});
        const std::string standard="123456789";require(textureCrc({reinterpret_cast<const uint8_t*>(standard.data()),standard.size()})==0xcbf43926u,"CRC32 standard vector");
        {std::ofstream file(root/"reference.ktx",std::ios::binary);file.write(reinterpret_cast<const char*>(bytes.data()),bytes.size());}
        Json row={{"id","reference/color"},{"key",std::string(64,'a')},{"sha256",std::string(64,'b')},{"file","reference.ktx"},{"role","base_color"},{"srgb",true},
            {"width",2u},{"height",1u},{"mips",2u},{"resident_bytes",color.bytes()},{"file_bytes",bytes.size()},{"crc32",textureCrc(bytes)}};
        writeDocument(root/"catalog.json","engine.texture-catalog",{{"textures",Json::array({row})}});
        auto records=loadTextureCatalog(root/"catalog.json");require(loadTexture(records[0]).bytes()==12,"Catalog/payload agreement");
        auto badRecord=records[0];badRecord.crc32^=1;rejects([&]{loadTexture(badRecord);});
        badRecord=records[0];badRecord.width=8;rejects([&]{loadTexture(badRecord);});
        writeDocument(root/"catalog.json","engine.texture-catalog",{{"textures",Json::array({row,row})}});rejects([&]{loadTextureCatalog(root/"catalog.json");});
        row["file"]="../escape.ktx";writeDocument(root/"catalog.json","engine.texture-catalog",{{"textures",Json::array({row})}});rejects([&]{loadTextureCatalog(root/"catalog.json");});
        std::cout<<"PASS: semantic color/data/alpha/vector mips, NPOT/1D/periodic footprints, strict KTX truncation/profile/CRC and catalog confinement\n";
    } catch(const std::exception& error){std::cerr<<error.what()<<'\n';return 1;}
}
