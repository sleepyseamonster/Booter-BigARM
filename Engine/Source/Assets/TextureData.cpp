#include "Assets/TextureData.h"
#include "Core/Color.h"
#include <algorithm>
#include <array>
#include <cmath>
#include <fstream>
#include <stdexcept>
namespace engine {
TextureRole textureRole(const std::string& name) {
    if (name=="base_color") return TextureRole::Color;
    if (name=="normal") return TextureRole::Normal;
    if (name=="packed_surface") return TextureRole::Surface;
    if (name=="height") return TextureRole::Height;
    if (name=="mask") return TextureRole::Mask;
    throw std::invalid_argument("Unsupported texture role: "+name);
}
size_t TextureData::bytes() const { size_t size=0; for (const auto& level:levels) size+=level.rgba.size(); return size; }
std::vector<uint8_t> readTextureFile(const std::filesystem::path& path) {
    std::ifstream file(path,std::ios::binary|std::ios::ate);
    if (!file) throw std::runtime_error("Missing texture: "+path.string());
    const auto size=file.tellg();
    if (size<=0 || size>std::streamoff(textureFileLimit)) throw std::runtime_error("Texture file exceeds bounded input profile");
    std::vector<uint8_t> result(static_cast<size_t>(size)); file.seekg(0);file.read(reinterpret_cast<char*>(result.data()),size);
    if (!file) throw std::runtime_error("Truncated texture input");
    return result;
}
TextureData semanticMips(const ImageLevel& base,TextureRole role) {
    if (!base.width || !base.height || base.width>2048 || base.height>2048 || base.rgba.size()!=size_t(base.width)*base.height*4)
        throw std::invalid_argument("Invalid source dimensions or payload; profile limit is 2048 per axis");
    TextureData result; result.srgb=role==TextureRole::Color;
    using Pixel=std::array<double,4>;
    std::vector<Pixel> pixels(size_t(base.width)*base.height);
    for (size_t i=0;i<pixels.size();++i) for (size_t c=0;c<4;++c) {
        const float value=base.rgba[i*4+c]/255.0f;
        pixels[i][c]=c<3 && result.srgb?srgbToLinear(value):c<3 && role==TextureRole::Normal?2*value-1:value;
    }
    uint32_t width=base.width,height=base.height;
    for (;;) {
        ImageLevel level{width,height,{}}; level.rgba.resize(size_t(width)*height*4);
        for (size_t i=0;i<pixels.size();++i) {
            auto& p=pixels[i];
            if (role==TextureRole::Normal) {
                const double length=std::sqrt(p[0]*p[0]+p[1]*p[1]+p[2]*p[2]);
                if (length<1e-6) {p[0]=0;p[1]=0;p[2]=1;}
                else for (int c=0;c<3;++c) p[c]/=length;
            }
            for (int c=0;c<4;++c) {
                double value=p[c];
                if (c<3 && result.srgb) value=linearToSrgb(float(value));
                else if (c<3 && role==TextureRole::Normal) value=value*.5+.5;
                level.rgba[i*4+c]=uint8_t(std::clamp(std::lround(value*255),0l,255l));
            }
        }
        result.levels.push_back(std::move(level));
        if (width==1 && height==1) break;
        const uint32_t nw=std::max(1u,width/2),nh=std::max(1u,height/2);
        std::vector<Pixel> next(size_t(nw)*nh);
        // Exact area footprints cover every source texel, including NPOT tails and 1D chains.
        // No taps outside the full period: aligned box filtering has identical repeat/clamp edge footprints.
        for (uint32_t y=0;y<nh;++y) for (uint32_t x=0;x<nw;++x) {
            const double x0=double(x)*width/nw,x1=double(x+1)*width/nw;
            const double y0=double(y)*height/nh,y1=double(y+1)*height/nh;
            auto& p=next[size_t(y)*nw+x];
            for (uint32_t sy=uint32_t(y0);sy<std::min(height,uint32_t(std::ceil(y1)));++sy)
                for (uint32_t sx=uint32_t(x0);sx<std::min(width,uint32_t(std::ceil(x1)));++sx) {
                    const double weight=(std::min(x1,double(sx+1))-std::max(x0,double(sx)))*
                        (std::min(y1,double(sy+1))-std::max(y0,double(sy)))/((x1-x0)*(y1-y0));
                    for (int c=0;c<4;++c) p[c]+=pixels[size_t(sy)*width+sx][c]*weight;
                }
        }
        pixels=std::move(next);width=nw;height=nh;
    }
    return result;
}
TextureData parseTexture(std::span<const uint8_t> bytes,TextureRole role) {
    constexpr uint8_t magic[]={0xab,0x4b,0x54,0x58,0x20,0x31,0x31,0xbb,0x0d,0x0a,0x1a,0x0a};
    if (bytes.size()<68 || bytes.size()>textureFileLimit || !std::equal(std::begin(magic),std::end(magic),bytes.begin()))
        throw std::runtime_error("Invalid bounded KTX1 texture");
    auto word=[&](size_t offset) {
        if (offset>bytes.size()-4) throw std::runtime_error("Truncated KTX word");
        return uint32_t(bytes[offset])|uint32_t(bytes[offset+1])<<8|uint32_t(bytes[offset+2])<<16|uint32_t(bytes[offset+3])<<24;
    };
    const bool srgb=word(28)==0x8c43;
    if (word(12)!=0x04030201 || word(16)!=0x1401 || word(20)!=1 || word(24)!=0x1908 ||
        (word(28)!=0x8058 && !srgb) || word(32)!=0x1908 || word(44)!=0 || word(48)!=0 || word(52)!=1 || word(60)!=0)
        throw std::runtime_error("Unsupported KTX profile (expected RGBA8 2D, no arrays or metadata)");
    if (srgb!=(role==TextureRole::Color)) throw std::runtime_error("Texture transfer function disagrees with semantic role");
    uint32_t w=word(36),h=word(40),count=word(56),expected=1;
    if (!w || !h || w>2048 || h>2048) throw std::runtime_error("Texture dimensions exceed profile");
    for (uint32_t side=std::max(w,h);side>1;side/=2) ++expected;
    if (count!=expected) throw std::runtime_error("Incomplete or excessive mip chain");
    TextureData result; result.srgb=srgb;size_t position=64;
    for (uint32_t mip=0;mip<count;++mip) {
        const size_t size=size_t(w)*h*4;
        if (position>bytes.size()-4 || word(position)!=size || size>bytes.size()-position-4)
            throw std::runtime_error("KTX mip dimensions/payload disagree");
        position+=4;result.levels.push_back({w,h,{bytes.begin()+position,bytes.begin()+position+size}});position+=size;
        w=std::max(1u,w/2);h=std::max(1u,h/2);
    }
    if (position!=bytes.size()) throw std::runtime_error("Trailing KTX payload");
    return result;
}
uint32_t textureCrc(std::span<const uint8_t> bytes) {
    uint32_t value=0xffffffff;
    for (uint8_t byte:bytes) {value^=byte;for (int bit=0;bit<8;++bit)value=(value>>1)^(0xedb88320u&uint32_t(-int32_t(value&1)));}
    return ~value;
}
}
