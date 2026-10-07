#include "Authoring/SceneAuthoringAdapter.h"
#include <cmath>
#include <cstdlib>
#include <fstream>
#include <iostream>
#include <limits>
#include <new>
#ifdef __APPLE__
#include <execinfo.h>
#endif
namespace {long failAt=-1;bool injected=false;const char* activeCase="none";int activeIndex=-1;}
void* operator new(size_t n){if(failAt==0){failAt=-1;injected=true;throw std::bad_alloc();}if(failAt>0)--failAt;if(auto p=std::malloc(n?n:1))return p;throw std::bad_alloc();}
void operator delete(void* p)noexcept{std::free(p);}
void* operator new[](size_t n){return ::operator new(n);}void operator delete[](void* p)noexcept{std::free(p);}
using namespace engine;
void require(bool yes,const char* reason){if(!yes)throw std::runtime_error(reason);}
template<class F>void rejects(F&& f,SceneErrorCode code){bool caught=false;try{f();}catch(const SceneDocumentError& e){caught=e.code==code;}require(caught,"Expected exact scene error category");}
SceneTransform translated(float x,float y=0,float z=0){SceneTransform t;t.translation={x,y,z};return t;}
void move(AuthoringSceneDocument& d,SceneEntityId id,SceneTransform t){auto tx=d.beginTransaction(d.version());tx.setTransform(id,t);require(tx.commit().applied,"Move fixture");}
void matrixNear(const AffineMatrix& a,const AffineMatrix& b){for(size_t i=0;i<16;++i)require(std::abs(a[i]-b[i])<1e-5,"Independent affine matrix expectation");}
Json allocationCases(){
    Json output=Json::object();
    for(const std::string mode:{"commit","undo","redo","adapter","enqueue"}){
        int failures=0;bool completed=false;
        for(int index=0;index<4096;++index){
            AuthoringSceneDocument d;const auto id=d.createEntity("entity");move(d,id,translated(1));if(mode=="redo")require(d.undo(d.version()),"Prepare redo");
            const auto before=d.toJson();const bool undo=d.canUndo(),redo=d.canRedo();const size_t bytes=d.historyBytes();
            auto tx=d.beginTransaction(d.version());tx.setTransform(id,translated(9));SceneAuthoringAdapter adapter(d);
            Json payload={{"operations",Json::array({{{"type","set_transform"},{"entity",id},{"transform",AuthoringSceneDocument::transformToJson(translated(9))}}})}};
            uint64_t request=0;if(mode=="adapter")request=adapter.enqueue({"apply_transaction",payload,d.version()});
            activeCase=mode.c_str();activeIndex=index;injected=false;failAt=index;
            try{
                if(mode=="commit")tx.commit();else if(mode=="undo")d.undo(d.version());else if(mode=="redo")d.redo(d.version());else if(mode=="adapter")adapter.process();else adapter.enqueue({"apply_transaction",payload,d.version()});
            }catch(const std::bad_alloc&){}
            failAt=-1;
            if(injected){++failures;require(d.toJson()==before&&d.canUndo()==undo&&d.canRedo()==redo&&d.historyBytes()==bytes,"Fault changed scene/revision/history");
                if(mode=="adapter"){const auto receipt=adapter.receipt(request);require(receipt&&receipt->status!=AuthoringOperationStatus::Succeeded,"Failed preparation cannot succeed");}
                if(mode=="enqueue")require(adapter.pending()==0,"Failed enqueue retains no orphan request");
            }else{completed=true;break;}
        }
        require(completed&&failures>0,"Exhausted bounded allocation fault enumeration");output[mode]={{"injected_boundaries",failures},{"eventual_success",completed}};
    }return output;
}
int main(int argc,char** argv)try{
#ifdef __APPLE__
    std::set_terminate([]{failAt=-1;std::cerr<<"Allocation case "<<activeCase<<" index "<<activeIndex<<'\n';void* frames[40];const int count=backtrace(frames,40);backtrace_symbols_fd(frames,count,2);std::_Exit(2);});
#endif
    require(argc==2,"Expected temporary test directory");const std::filesystem::path root=argv[1];std::filesystem::create_directories(root);
    Json evidence;
    {
        AuthoringSceneDocument d;auto tx=d.beginTransaction(d.version());auto p=tx.createEntity("parent"),c=tx.createEntity("child",p),g=tx.createEntity("grandchild",c);
        SceneTransform scale;scale.scale={2,1,1};SceneTransform rotate;rotate.rotation={0,0,std::sqrt(.5f),std::sqrt(.5f)};
        tx.setTransform(p,scale);tx.setTransform(c,rotate);tx.setTransform(g,translated(1));require(tx.commit().applied,"Affine hierarchy fixture");
        const AffineMatrix expected={0,1,0,0,-2,0,0,0,0,0,1,0,0,1,0,1};matrixNear(d.worldMatrix(g),expected);
        scale.scale={-2,1,1};move(d,p,scale);auto mirrored=expected;mirrored[4]=2;matrixNear(d.worldMatrix(g),mirrored);require(affineDeterminant(d.worldMatrix(g))<0,"Mirrored determinant retained");
        rotate.rotation={0,0,float(std::sin(.392699081698724)),float(std::cos(.392699081698724))};move(d,c,rotate);
        rejects([&]{d.worldTransform(c);},SceneErrorCode::UnsupportedTransform);const auto before=d.toJson();auto reparent=d.beginTransaction(d.version());
        rejects([&]{reparent.reparent(c,{},true);},SceneErrorCode::UnsupportedTransform);require(!reparent.commit().applied&&d.toJson()==before,"Reject reparent shear without mutation");
        auto normal=normalAffine(d.worldMatrix(c));auto tangent=affinePoint(d.worldMatrix(c),{1,0,0});auto origin=affinePoint(d.worldMatrix(c),{});
        // Local normal Y remains perpendicular to transformed local tangent X.
        double dot=0;for(size_t i=0;i<3;++i)dot+=(tangent[i]-origin[i])*normal[4+i];require(std::abs(dot)<1e-8,"Inverse-transpose normal oracle");
        evidence["affine_hierarchy_mirror_shear_and_normals"]=true;
    }
    {
        AuthoringSceneDocument d;auto p=d.createEntity("P"),c=d.createEntity("C",p),g=d.createEntity("G",c);move(d,p,translated(2));move(d,c,translated(3));
        auto t=d.beginTransaction(d.version());auto wp=d.worldMatrix(p),wc=d.worldMatrix(c);wp[12]+=5;wc[12]+=5;t.setWorldTransforms({{c,wc},{p,wp}});require(t.commit().applied,"World multi-selection");require(d.worldMatrix(c)[12]==10&&d.find(c)->local.translation[0]==3,"Selected child not transformed twice");
        auto rp=d.beginTransaction(d.version());rp.reparent(c,{},true);require(rp.commit().applied&&d.worldMatrix(c)[12]==10,"Preserve world reparent");
        auto cycle=d.beginTransaction(d.version());rejects([&]{cycle.reparent(c,g,false);},SceneErrorCode::Hierarchy);require(!cycle.commit().applied,"Invalid staging aborts");
        const auto before=d.toJson();const auto high=d.nextEntityId();require(d.eraseEntity(c)&&!d.find(g),"Delete subtree");require(d.undo(d.version())&&d.find(g)&&d.find(c),"Undo restores subtree");require(d.nextEntityId()==high,"Undo preserves high water");
        require(d.redo(d.version())&&!d.find(c),"Redo subtree removal");require(d.undo(d.version()),"Restore for duplication");
        auto dup=d.beginTransaction(d.version());const auto copy=dup.duplicate(c);require(dup.commit().applied&&copy>=high&&d.find(copy),"Fresh duplicate identity");const auto next=d.nextEntityId();require(d.undo(d.version()),"Undo duplicate");const auto newId=d.createEntity("New");require(newId>=next&&!d.canRedo(),"No committed ID reuse and new edit clears redo");
        auto mixed=d.beginTransaction(d.version());mixed.setMetadata(p,"mesh","material","collider",2,false,{"anchor"});mixed.setTransform(p,translated(9));require(mixed.commit().applied,"Mixed metadata/transform");require(d.undo(d.version())&&d.find(p)->mesh.empty()&&d.find(p)->local.translation[0]==7,"Mixed inverse atomic");
        require(!d.undo()&&!d.redo(),"Undo/redo require explicit revision");evidence["structural_history_and_parent_aware_edits"]=true;(void)before;
    }
    {
        AuthoringSceneDocument d;auto id=d.createEntity("A");const auto before=d.toJson();auto t=d.beginTransaction(d.version());t.setTransform(id,translated(2));
        rejects([&]{t.setTransform(999,translated(3));},SceneErrorCode::Validation);require(!t.commit().applied&&d.toJson()==before,"Failed staging cannot commit prefix");
        auto edit=d.beginTransaction(d.version());edit.setTransform(id,translated(1));auto prepared=edit.prepare();move(d,id,translated(4));require(!prepared.apply()&&d.find(id)->local.translation[0]==4,"Stale prepared candidate rejected");
        {auto zero=d.beginTransaction(d.version());zero.setTransform(id,translated(5));auto ready=zero.prepare();injected=false;failAt=0;const bool applied=ready.apply();failAt=-1;require(applied&&!injected,"Publication performs no allocation");}
        {auto transient=d.beginTransaction(d.version());const auto temporary=transient.createEntity("provisional");transient.eraseEntity(temporary);require(transient.commit().applied,"Allocate/delete within transaction");require(d.undo(d.version()),"No empty history barrier after transient object");require(d.find(id)->local.translation[0]==4,"Undo reaches prior real edit");}
        auto j=d.toJson();j["revision"]=UINT64_MAX;rejects([&]{AuthoringSceneDocument::fromJson(j);},SceneErrorCode::Exhausted);
        j=d.toJson();j["revision"]=UINT64_MAX-1;auto last=AuthoringSceneDocument::fromJson(j);const auto old=last.toJson();auto exhausted=last.beginTransaction(last.version());exhausted.setTransform(id,translated(8));require(!exhausted.commit().applied&&last.toJson()==old,"Revision cannot wrap");last.save(root/"last-revision.json");require(AuthoringSceneDocument::load(root/"last-revision.json").toJson()==old,"Last admitted revision remains readable");
        j=d.toJson();j["entities"][0]["components"]["lod"]=1.5;rejects([&]{AuthoringSceneDocument::fromJson(j);},SceneErrorCode::Validation);
        j=d.toJson();j["unknown"]=true;rejects([&]{AuthoringSceneDocument::fromJson(j);},SceneErrorCode::Validation);
        j=d.toJson();j["entities"][0]["transform"]["unknown"]=true;rejects([&]{AuthoringSceneDocument::fromJson(j);},SceneErrorCode::Validation);
        auto p=d.createEntity("P"),c=d.createEntity("C",p);move(d,p,translated(std::numeric_limits<float>::max()));const auto hugeBefore=d.toJson();auto huge=d.beginTransaction(d.version());huge.setTransform(c,translated(std::numeric_limits<float>::max()));require(!huge.commit().applied&&d.toJson()==hugeBefore,"Composed renderer range overflow rejected before adoption");
        evidence["strict_schema_exhaustion_and_prepared_conflicts"]=true;
    }
    {
        AuthoringSceneDocument d;auto tx=d.beginTransaction(d.version());for(size_t i=0;i<AuthoringSceneDocument::maxEntities;++i)tx.createEntity("bounded");require(tx.commit().applied,"Maximum entity profile admitted");
        d.save(root/"bounded.json");require(AuthoringSceneDocument::load(root/"bounded.json").toJson()==d.toJson(),"Maximum count roundtrip");const auto before=d.toJson();rejects([&]{d.createEntity("excess");},SceneErrorCode::Capacity);require(d.toJson()==before,"Entity capacity rejection atomic");
        const auto id=d.entities()[0].id;const std::vector<std::string> tags(64,std::string(64,'t'));size_t accepted=0;
        for(const auto& entity:d.entities()){
            auto edit=d.beginTransaction(d.version());edit.setMetadata(entity.id,"","","",0,true,tags);
            try{auto ready=edit.prepare();require(ready.apply(),"Metadata candidate apply");++accepted;}catch(const SceneDocumentError& error){require(error.code==SceneErrorCode::Capacity,"Expected byte capacity");break;}
        }
        require(accepted>0&&accepted<AuthoringSceneDocument::maxEntities,"Exact byte bound independently reached");const auto saved=d.toJson();d.save(root/"byte-bound.json");require(std::filesystem::file_size(root/"byte-bound.json")<=documentLimit&&AuthoringSceneDocument::load(root/"byte-bound.json").toJson()==saved,"Largest admitted byte profile roundtrip");
        auto oversize=d.toJson();for(auto& e:oversize["entities"])e["tags"]=tags;rejects([&]{auto candidate=AuthoringSceneDocument::fromJson(oversize);candidate.save(root/"byte-bound.json");},SceneErrorCode::Capacity);require(AuthoringSceneDocument::load(root/"byte-bound.json").toJson()==saved,"Rejected candidate preserves last valid file");
        // Repeated edits near the budget must retain a usable inverse, not poison history.
        for(int i=0;i<12;++i)move(d,id,translated(float(i+1)));require(d.historyBytes()<=AuthoringSceneDocument::historyByteLimit,"History budget retained");while(d.canUndo())require(d.undo(d.version()),"Budgeted history remains traversable");while(d.canRedo())require(d.redo(d.version()),"Budgeted redo remains traversable");
        AuthoringSceneDocument chain;auto chainTx=chain.beginTransaction(chain.version());std::optional<SceneEntityId> parent;for(size_t i=0;i<=AuthoringSceneDocument::maxParentLinks;++i)parent=chainTx.createEntity("chain",parent);require(chainTx.commit().applied,"Maximum hierarchy admitted");auto over=chain.beginTransaction(chain.version());over.createEntity("too deep",parent);rejects([&]{over.prepare();},SceneErrorCode::Capacity);
        evidence["capacity_roundtrip_and_history_budget"]={{"entities",d.entities().size()},{"large_metadata_entities",accepted},{"saved_bytes",std::filesystem::file_size(root/"byte-bound.json")}};
    }
    evidence["allocation_atomicity"]=allocationCases();evidence["passed"]=true;
    std::cout<<evidence.dump(2)<<'\n';
}catch(const std::exception& e){failAt=-1;std::cerr<<e.what()<<'\n';return 1;}
