#include "Core/WorldIdentity.h"
#include "Persistence/Document.h"
#include "Runtime/InspectionDocument.h"
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
        saveInspection(document,authored);loadInspection(document,loaded);
        require(!loaded.shadows && loaded.roughness==.2f && loaded.normalStrength==0,"Lighting settings roundtrip");
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
        std::cout<<"PASS: stable identity, negative/large coordinates, bounded JSON, strict candidate load, atomic replacement, contention and content confinement\n";
    } catch(const std::exception& error) { std::cerr<<error.what()<<'\n'; return 1; }
}
