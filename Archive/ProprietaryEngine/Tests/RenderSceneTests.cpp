#include "Authoring/SceneAuthoringAdapter.h"
#include "Authoring/SceneManipulation.h"
#include "Rendering/RenderScene.h"
#include <cmath>
#include <iostream>
#include <stdexcept>

using namespace engine;
namespace {
void require(bool value,const char* message){if(!value)throw std::runtime_error(message);}
bool near(double a,double b,double tolerance=1e-6){return std::abs(a-b)<tolerance;}
SceneTransform transform(float x,float y,float z){SceneTransform out;out.translation={x,y,z};return out;}
struct LeaseProbe {explicit LeaseProbe(int* value):destroyed(value){}int* destroyed;~LeaseProbe(){++*destroyed;}};
}

int main(){try{
    auto source=std::make_shared<AuthoringSceneDocument>("render-scene");
    const auto parent=source->createEntity("Parent");
    const auto child=source->createEntity("Child",parent);
    const auto missing=source->createEntity("Missing");
    auto edit=source->beginTransaction(source->version());
    SceneTransform p=transform(10,0,0);p.rotation={0,0,float(std::sin(.25)),float(std::cos(.25))};p.scale={2,1,0.5f};
    edit.setTransform(parent,p);edit.setTransform(child,transform(1,2,0));
    edit.setMetadata(parent,"engine://mesh/sloped-solid","engine://material/default","",0,true,{"render"});
    edit.setMetadata(child,"engine://mesh/cube","engine://material/default","",0,true,{});
    edit.setMetadata(missing,"content://missing/mesh","content://missing/material","",0,true,{});
    require(edit.commit().applied,"Render fixture transaction");

    RenderAssetCatalog catalog;
    int destroyed=0;
    catalog.publish(RenderMeshAsset{{"engine://mesh/sloped-solid"},BuiltinRenderMesh::SlopedSolid,
        {-1,-2,-3},{1,2,3},true,true,std::make_shared<LeaseProbe>(&destroyed)});
    auto snapshot=extractRenderScene(source,catalog,true);
    require(snapshot.sceneId==source->sceneId()&&snapshot.revision==source->version()&&snapshot.draws.size()==3,"Snapshot identity/draw extraction");
    require(snapshot.errors.size()==1&&snapshot.draws[2].diagnostic&&snapshot.draws[2].diagnosticMessage.find("content://missing/mesh")!=std::string::npos,"Missing assets are actionable diagnostics");
    const auto& parentDraw=snapshot.draws[0];
    require(parentDraw.world==source->worldMatrix(parent)&&parentDraw.normal==normalAffine(parentDraw.world),"Snapshot affine and normal oracle");
    // Independent eight-corner bounds oracle for the asymmetric local box.
    std::array<double,3> low{1e30,1e30,1e30},high{-1e30,-1e30,-1e30};
    for(unsigned corner=0;corner<8;++corner){auto point=affinePoint(parentDraw.world,{corner&1?1.:-1.,corner&2?2.:-2.,corner&4?3.:-3.});for(size_t i=0;i<3;++i){low[i]=std::min(low[i],point[i]);high[i]=std::max(high[i],point[i]);}}
    double radiusSquared=0;for(size_t i=0;i<3;++i){require(near(parentDraw.boundsCenter[i],(low[i]+high[i])*.5),"Transformed bounds center");const auto extent=(high[i]-low[i])*.5;radiusSquared+=extent*extent;}
    require(near(parentDraw.boundsRadius,std::sqrt(radiusSquared),1e-5),"Transformed bounds radius");
    const auto hit=pickRenderScene(snapshot,{{10,0,20},{0,0,-1}});require(hit&&hit->entity==parent,"Affine local-bounds picking");

    // Snapshot retains old resource leases across catalog replacement/removal.
    catalog.publish(RenderMeshAsset{{"engine://mesh/sloped-solid"},BuiltinRenderMesh::Sphere,{-1,-1,-1},{1,1,1},true,true,{}});
    require(destroyed==0&&snapshot.draws[0].mesh->builtin==BuiltinRenderMesh::SlopedSolid,"In-flight snapshot retains replaced asset lease");
    snapshot={};require(destroyed==1,"Retired asset releases after last snapshot");

    RenderViewportRegistry views;
    views.add({"scene",20,21,1280,720,1});views.add({"game",30,31,1920,1080,1});
    views.resize("scene",640,480);
    require(views.get("scene").width==640&&views.get("scene").targetGeneration==2&&views.get("game").width==1920&&views.get("game").targetGeneration==1,"Viewport resize ownership is independent");
    bool overlap=false;try{views.add({"bad",21,40,100,100,1});}catch(const std::invalid_argument&){overlap=true;}require(overlap,"Overlapping view IDs reject");
    views.remove("scene");require(views.get("game").height==1080,"Viewport removal preserves peers");

    // Parent and child selected together receive one world delta, with no doubled child motion.
    SceneGizmoSession gizmo;gizmo.begin(source,{child,parent},GizmoTool::Translate,GizmoSpace::World,{.5,0,0});
    const auto preview=gizmo.update({{2.24,0,0},{0,0,0,1},{1,1,1}});
    require(preview.worlds.size()==2&&near(preview.worlds[0].second[12],source->worldMatrix(parent)[12]+2)&&near(preview.worlds[1].second[12],source->worldMatrix(child)[12]+2),"Parent-aware preview and translation snap");
    const auto beforeRevision=source->version();const auto committed=gizmo.commitDirect(*source);
    require(committed.applied&&source->version()==beforeRevision+1,"Completed drag is one transaction");
    const auto movedParent=source->worldMatrix(parent),movedChild=source->worldMatrix(child);
    require(source->undo(source->version()),"One undo accepts completed drag");
    require(source->worldMatrix(parent)[12]<movedParent[12]&&source->worldMatrix(child)[12]<movedChild[12],"One undo restores entire selection");
    require(source->redo(source->version()),"One redo reapplies completed drag");

    // Local translation follows the selected object's orientation.
    gizmo.begin(source,{parent},GizmoTool::Translate,GizmoSpace::Local);
    const auto local=gizmo.update({{1,0,0},{0,0,0,1},{1,1,1}});
    require(local.worlds[0].second[13]>source->worldMatrix(parent)[13]+.4,"Local-axis translation follows rotated basis");
    const auto unchanged=source->toJson();gizmo.focusLost();require(!gizmo.active()&&source->toJson()==unchanged,"Focus loss cancels preview without source mutation");

    gizmo.begin(source,{parent},GizmoTool::Rotate,GizmoSpace::World,{0,.25,0});
    gizmo.update({{}, {0,0,std::sin(.14),std::cos(.14)}, {1,1,1}});
    const auto payload=gizmo.commitPayload();
    auto receipt=SceneAuthoringAdapter::execute(*source,"apply_transaction",payload,source->version());
    require(receipt.at("changed_entities").size()>=1,"Shared adapter accepts gizmo world transaction");gizmo.cancel();

    gizmo.begin(source,{child},GizmoTool::Scale,GizmoSpace::Local,{0,0,.25});
    gizmo.update({{}, {0,0,0,1}, {1.24,1,1}});
    source->createEntity("Concurrent AI edit");
    bool conflict=false;try{gizmo.commitDirect(*source);}catch(const SceneDocumentError& error){conflict=error.code==SceneErrorCode::Conflict;}
    require(conflict&&!gizmo.active(),"Concurrent edit conflicts and closes drag");

    std::cout<<"PASS: typed asset snapshots, affine bounds/picking, lease retention, viewport isolation, parent-aware gizmo preview/commit/cancel/conflict and shared adapter\n";
    return 0;
}catch(const std::exception& error){std::cerr<<"FAIL: "<<error.what()<<'\n';return 1;}}
