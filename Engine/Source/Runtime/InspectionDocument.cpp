#include "Runtime/InspectionDocument.h"
#include "Persistence/Document.h"
#include <cmath>
#include <stdexcept>
namespace engine {
namespace {
Json encode(const FixtureState& s) {
    return {{"yaw",s.yaw},{"pitch",s.pitch},{"distance",s.distance},{"fov",s.fieldOfView},
        {"object_yaw",s.objectYaw},{"mesh",s.mesh},{"scale",s.objectScale},{"normals",s.showNormals},
        {"color_srgb",s.color},{"light_azimuth",s.lightAzimuth},{"light_intensity",s.lightIntensity},{"exposure",s.exposure},
        {"lighting",{{"shadows",s.shadows},{"shadow_bias",s.shadowBias},{"roughness",s.roughness},{"metallic",s.metallic},
            {"normal_strength",s.normalStrength},{"texture_scale",s.textureScale},{"ambient",s.ambient},{"surface_textures",s.surfaceTextures}}}};
}
void validate(const FixtureState& s) {
    auto range=[](float v,float low,float high) { if (!std::isfinite(v) || v<low || v>high) throw std::runtime_error("Inspection value outside supported range"); };
    range(s.yaw,-10000,10000); range(s.pitch,-.15f,1.25f); range(s.distance,2.5f,30); range(s.fieldOfView,30,90);
    range(s.objectYaw,-10000,10000); range(s.lightAzimuth,-10000,10000); range(s.lightIntensity,0,1.5f);
    range(s.exposure,-4,4);
    range(s.shadowBias,0,.02f);range(s.roughness,.045f,1);range(s.metallic,0,1);
    range(s.normalStrength,0,2);range(s.textureScale,.1f,8);range(s.ambient,0,1);
    for (float v:s.color) range(v,0,1);
    for (float v:s.objectScale) range(v,.2f,3);
    if (s.mesh<0 || s.mesh>2) throw std::runtime_error("Unknown inspection mesh");
}
}
void saveInspection(const std::filesystem::path& path,const FixtureState& state) {
    validate(state); writeDocument(path,"engine.inspection",encode(state));
}
void loadInspection(const std::filesystem::path& path,FixtureState& state) {
    auto p=readDocument(path,"engine.inspection");
    FixtureState next;
    const auto expected=encode(next);
    // Additive v1 extension: old inspection documents receive explicit defaults.
    if (!p.contains("lighting")) p["lighting"]=expected.at("lighting");
    if (p.size()!=expected.size()) throw std::runtime_error("Inspection fields differ from schema");
    for (const auto& [key,value]:expected.items()) {
        const auto& input=p.at(key);
        if (value.is_number_float() ? !input.is_number() : value.type()!=input.type() && !(value.is_number_integer() && input.is_number_integer()))
            throw std::runtime_error("Incorrect inspection field type: "+key);
    }
    auto triple=[&](const char* key) {
        const auto& v=p.at(key);
        if (v.size()!=3) throw std::runtime_error("Inspection vector must have three components");
        for (const auto& n:v) if (!n.is_number()) throw std::runtime_error("Inspection vector must contain numbers");
        return v.get<std::array<float,3>>();
    };
    next.yaw=p.at("yaw"); next.pitch=p.at("pitch"); next.distance=p.at("distance"); next.fieldOfView=p.at("fov");
    next.objectYaw=p.at("object_yaw");
    if (p.at("mesh")<0 || p.at("mesh")>2) throw std::runtime_error("Unknown inspection mesh");
    next.mesh=p.at("mesh"); next.objectScale=triple("scale"); next.showNormals=p.at("normals");
    next.color=triple("color_srgb"); next.lightAzimuth=p.at("light_azimuth"); next.lightIntensity=p.at("light_intensity");
    next.exposure=p.at("exposure");
    const auto& lighting=p.at("lighting");
    const auto& defaults=expected.at("lighting");
    if(lighting.size()!=defaults.size()) throw std::runtime_error("Lighting fields differ from schema");
    for(const auto& [key,value]:defaults.items()) {
        const auto& input=lighting.at(key);
        if(value.is_boolean()?!input.is_boolean():!input.is_number()) throw std::runtime_error("Incorrect lighting field type: "+key);
    }
    next.shadows=lighting.at("shadows");next.shadowBias=lighting.at("shadow_bias");next.roughness=lighting.at("roughness");
    next.metallic=lighting.at("metallic");next.normalStrength=lighting.at("normal_strength");next.textureScale=lighting.at("texture_scale");
    next.ambient=lighting.at("ambient");next.surfaceTextures=lighting.at("surface_textures");
    validate(next); state=next;
}
}
