#include "Core/WorldIdentity.h"
#include "Persistence/Document.h"
#include "Runtime/InspectionDocument.h"
#include "Runtime/AuthoringOperations.h"
#include <fstream>
#include <iostream>
#include <limits>
#include <stdexcept>
using namespace engine;
void require(bool value,const char* message) { if (!value) throw std::runtime_error(message); }
template<class F> void rejects(F f) { bool caught=false; try { f(); } catch (const std::exception&) { caught=true; } require(caught,"Expected rejection"); }
int main(int argc,char** argv) {
    try {
        require(argc==2,"Expected test output directory");
        const std::filesystem::path root=argv[1]; std::filesystem::create_directories(root);
        auto negative=WorldPosition{{0,0},{-1,7,-256}}.normalized(256);
        require(negative.region==Region{-1,-1} && negative.local==std::array<double,3>{255,7,0},"Negative region normalization");
        auto tiny=WorldPosition{{0,0},{-std::numeric_limits<double>::denorm_min(),0,0}}.normalized(256);
        require(tiny.region.x==-1 && tiny.local[0]<256,"Subnormal negative boundary retains prior region");
        auto large=WorldPosition{{INT64_MAX-1,INT64_MIN+1},{256,2,-1}}.normalized(256);
        require(large.region==Region{INT64_MAX,INT64_MIN},"Large integer regions");
        auto relative=large.relativeTo({{INT64_MAX,INT64_MIN},{0,0,250}},256,100);
        require(relative==std::array<float,3>{0,2,5},"Nearby large regions retain precision");
        rejects([] { WorldPosition{{INT64_MAX,0},{256,0,0}}.normalized(256); });
        rejects([] { WorldPosition{{0,0},{0,0,0}}.normalized(0); });
        rejects([] { WorldPosition{{0,0},{NAN,0,0}}.normalized(256); });
        rejects([&] { large.relativeTo({},256,1000); });
        GeneratedId id{UINT64_MAX,3,{-7,12},42};
        require(id.text()=="generated:v1:world:18446744073709551615:3:-7:12:42","Structural ID encoding independent of order");
        auto other=id; other.generator="rocks"; require(other.text()!=id.text(),"Generator namespaces prevent identity collisions");
        const auto document=root/"inspection.json";
        FixtureState original; original.mesh=2; original.color={.1f,.2f,.3f}; saveInspection(document,original);
        FixtureState loaded; loadInspection(document,loaded);
        require(loaded.color==original.color && loaded.mesh==2,"Inspection roundtrip");
        original.distance=11; saveInspection(document,original); loadInspection(document,loaded);
        require(loaded.distance==11,"Atomic replacement");
        { std::ofstream lock(document.string()+".writing"); lock<<"other writer"; }
        rejects([&] { saveInspection(document,FixtureState{}); });
        FixtureState retained; loadInspection(document,retained); require(retained.distance==11,"Writer contention retains prior document");
        std::filesystem::remove(document.string()+".writing");
        const auto valid=readDocument(document,"engine.inspection");
        auto old=valid;old.erase("lighting");writeDocument(document,"engine.inspection",old);
        loadInspection(document,loaded);require(loaded.shadows && loaded.roughness==.7f,"Old inspection receives lighting defaults");
        auto badLighting=valid;badLighting["lighting"]["roughness"]=-1;writeDocument(document,"engine.inspection",badLighting);
        rejects([&] {loadInspection(document,loaded);});require(loaded.roughness==.7f,"Bad lighting retains live state");
        auto authored=loaded;authored.shadows=false;authored.roughness=.2f;authored.normalStrength=0;
        authored.ambientOcclusion=true;authored.aoStrength=.6f;authored.aoRadius=2.25f;authored.contactShadows=true;authored.contactStrength=.8f;authored.contactDistance=1.5f;
        saveInspection(document,authored);loadInspection(document,loaded);
        require(!loaded.shadows && loaded.roughness==.2f && loaded.normalStrength==0 && loaded.ambientOcclusion && loaded.aoStrength==.6f && loaded.aoRadius==2.25f && loaded.contactShadows && loaded.contactStrength==.8f && loaded.contactDistance==1.5f,"Lighting settings roundtrip");
        auto authoredEnvironment=loaded;
        authoredEnvironment.environment.sunElevation=.2f;authoredEnvironment.environment.sunColor={.9f,.7f,.4f};
        authoredEnvironment.environment.zenith={.1f,.3f,.5f};authoredEnvironment.environment.horizon={.8f,.6f,.4f};
        authoredEnvironment.environment.ground={.2f,.1f,.05f};authoredEnvironment.environment.sky=false;authoredEnvironment.environment.toneMapping=false;
        authoredEnvironment.environment.fog=true;authoredEnvironment.environment.fogDensity=.025f;authoredEnvironment.environment.fogHeightFalloff=.12f;authoredEnvironment.environment.fogColor={.3f,.35f,.4f};
        saveInspection(document,authoredEnvironment);loadInspection(document,loaded);
        require(loaded==authoredEnvironment,"Environment roundtrip preserves all presentation settings");
        auto legacyFog=readDocument(document,"engine.inspection");legacyFog["environment"].at("version")=1;
        for(const auto& key:{"fog_color_linear","fog_density","fog_height_falloff","fog"})legacyFog["environment"].erase(key);
        writeDocument(document,"engine.inspection",legacyFog);loadInspection(document,loaded);
        require(!loaded.environment.fog&&loaded.environment.fogDensity==0,"Environment v1 receives fog defaults");
        auto legacyEnvironment=valid;legacyEnvironment.erase("environment");writeDocument(document,"engine.inspection",legacyEnvironment);
        loadInspection(document,loaded);require(loaded.environment==EnvironmentSettings{},"Legacy inspection gets explicit environment defaults");
        for(const auto& badEnvironment:{Json{{"sun_elevation",-1}},Json{{"sun_color_linear",Json::array({1,2,0})}},Json{{"version",3}},Json{{"sky",1}},Json{{"horizon_linear",Json::array({1,0})}}}) {
            auto malformed=valid;
            for(const auto& [key,value]:badEnvironment.items())malformed["environment"][key]=value;
            writeDocument(document,"engine.inspection",malformed);const auto before=loaded;
            rejects([&]{loadInspection(document,loaded);});require(loaded==before,"Bad environment retains live settings");
        }
        auto invalid=valid; invalid["distance"]=-1; writeDocument(document,"engine.inspection",invalid);
        rejects([&] { loadInspection(document,loaded); }); require(loaded.distance==11,"Invalid candidate retains live state");
        writeDocument(document,"engine.inspection",valid,2); rejects([&] { loadInspection(document,loaded); });
        auto raw=[&](const std::string& bytes) { std::ofstream file(document); file<<bytes; file.close(); rejects([&] { readDocument(document,"engine.inspection"); }); };
        raw("{\"kind\":\"x\",\"kind\":\"y\"}"); raw(std::string(documentLimit+1,' ')); raw("{");
        raw(std::string(40,'[')+"0"+std::string(40,']'));
        auto nonfinite=valid; nonfinite["distance"]=std::numeric_limits<double>::infinity();
        rejects([&] { writeDocument(document,"engine.inspection",nonfinite); });
        rejects([&] { contentPath(root,"../outside"); }); rejects([&] { contentPath(root,"C:\\outside"); });
        require(contentPath(root,"inspection.json")==std::filesystem::canonical(document),"Portable content path");
        AuthoringOperations operations(2,4);uint64_t authoringVersion=3;
        operations.registerHandler("set_value",[&](const AuthoringJson& payload,uint64_t expected,uint64_t& resulting) {
            if(expected!=authoringVersion)throw std::runtime_error("Stale authoring version");
            resulting=++authoringVersion;return AuthoringJson{{"accepted",payload.at("value")}};
        });
        const auto accepted=operations.enqueue({"set_value",AuthoringJson{{"value",7}},3});
        require(operations.pending()==1&&operations.process(1)==1,"Bounded authoring operation processing");
        const auto acceptedReceipt=operations.receipt(accepted);require(acceptedReceipt&&acceptedReceipt->status==AuthoringOperationStatus::Succeeded&&acceptedReceipt->resultingVersion==4,"Authoring operation receipt and version");
        const auto unknown=operations.enqueue({"missing",AuthoringJson::object(),0});operations.process();
        const auto unknownReceipt=operations.receipt(unknown);require(unknownReceipt&&unknownReceipt->status==AuthoringOperationStatus::Rejected,"Unknown authoring operation is rejected");
        const auto stale=operations.enqueue({"set_value",AuthoringJson{{"value",8}},3});operations.process();
        const auto staleReceipt=operations.receipt(stale);require(staleReceipt&&staleReceipt->status==AuthoringOperationStatus::Failed&&authoringVersion==4,"Stale authoring edit cannot mutate version");
        std::cout<<"PASS: stable identity, negative/large coordinates, bounded JSON, strict candidate load, atomic replacement, contention and content confinement\n";
    } catch(const std::exception& error) { std::cerr<<error.what()<<'\n'; return 1; }
}
