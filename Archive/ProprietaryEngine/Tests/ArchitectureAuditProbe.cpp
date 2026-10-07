// Observational audit probes, not acceptance tests. Run through Tools/audit_architecture.py.
// A reproduced_issue value of true means the engine needs correction.
#include "Authoring/SceneAuthoringAdapter.h"
#include <chrono>
#include <cmath>
#include <cstdlib>
#include <iostream>
#include <limits>
#include <new>

namespace {
long allocationCountdown = -1;
}
void* operator new(std::size_t size) {
    if (allocationCountdown == 0) throw std::bad_alloc();
    if (allocationCountdown > 0) --allocationCountdown;
    if (void* memory = std::malloc(size ? size : 1)) return memory;
    throw std::bad_alloc();
}
void operator delete(void* memory) noexcept { std::free(memory); }
void* operator new[](std::size_t size) { return ::operator new(size); }
void operator delete[](void* memory) noexcept { std::free(memory); }

using namespace engine;
SceneTransform translated(float x) { SceneTransform t; t.translation[0] = x; return t; }
void move(AuthoringSceneDocument& d, SceneEntityId id, SceneTransform t) {
    auto tx = d.beginTransaction(d.version()); tx.setTransform(id,t);
    if (!tx.commit().applied) throw std::runtime_error("Probe setup failed");
}

int main() {
    try {
        Json evidence = Json::object();
        {
            AuthoringSceneDocument d("hierarchy-probe");
            const auto p=d.createEntity("scaled-parent"), c=d.createEntity("rotated-child",p), g=d.createEntity("grandchild",c);
            SceneTransform scale; scale.scale={2,1,1}; move(d,p,scale);
            SceneTransform rotation; rotation.rotation={0,0,std::sqrt(.5f),std::sqrt(.5f)}; move(d,c,rotation);
            move(d,g,translated(1));
            const auto actual=d.worldTransform(g).translation;
            // Independent oracle: parent S * child Rz(90) * grandchild Tx(1).
            evidence["hierarchy_nonuniform_scale"]={{"expected_translation",{0,1,0}},{"actual_translation",actual},
                {"reproduced_issue",std::abs(actual[1]-1)>0.01f}};
        }
        {
            AuthoringSceneDocument d;
            auto a=d.createEntity("A"),b=d.createEntity("B");
            move(d,a,translated(1));move(d,b,translated(2));d.eraseEntity(b);
            bool undone=d.undo(d.version());
            evidence["deleted_entity_blocks_history"]={{"can_undo",d.canUndo()},{"undo_succeeded",undone},
                {"remaining_entity_x",d.find(a)->local.translation[0]},{"reproduced_issue",!undone&&d.canUndo()}};
        }
        {
            AuthoringSceneDocument d;auto id=d.createEntity("A");
            auto tx=d.beginTransaction(d.version());tx.setTransform(id,translated(3));
            bool rejected=false;try {tx.setTransform(999,translated(8));}catch(const std::exception&){rejected=true;}
            auto result=tx.commit();
            evidence["failed_direct_transaction_can_commit_prefix"]={{"invalid_edit_rejected",rejected},
                {"commit_succeeded",result.applied},{"reproduced_issue",rejected&&d.find(id)->local.translation[0]==3}};
        }
        {
            AuthoringSceneDocument d;auto id=d.createEntity("A");
            auto data=d.toJson();data["revision"]=UINT64_MAX;d=AuthoringSceneDocument::fromJson(data);
            SceneAuthoringAdapter adapter(d);
            Json payload={{"operations",Json::array({{{"type","set_transform"},{"entity",id},
                {"transform",AuthoringSceneDocument::transformToJson(translated(4))}}})}};
            auto request=adapter.enqueue({"apply_transaction",payload,d.version()});adapter.process();auto receipt=*adapter.receipt(request);
            evidence["revision_overflow"]={{"revision",d.version()},{"receipt_error",receipt.error},
                {"reproduced_issue",d.version()==0&&receipt.status==AuthoringOperationStatus::Failed&&d.find(id)->local.translation[0]==4}};
        }
        {
            AuthoringSceneDocument d;auto p=d.createEntity("P"),c=d.createEntity("C",p);
            auto huge=translated(std::numeric_limits<float>::max());move(d,p,huge);move(d,c,huge);
            const bool finite=std::isfinite(d.worldTransform(c).translation[0]);
            bool inspectionRejected=false;try{d.inspectionJson();}catch(const std::exception&){inspectionRejected=true;}
            evidence["composed_transform_overflow"]={{"world_finite",finite},{"inspection_rejected",inspectionRejected},
                {"reproduced_issue",!finite&&inspectionRejected}};
        }
        {
            Json failures=Json::array();
            // Fail each early allocation in commit, using a fresh document each time.
            // Fault injection is disabled before inspecting or recording the result.
            for (long failAt=0;failAt<12;++failAt) {
                AuthoringSceneDocument d;auto id=d.createEntity("A");auto revision=d.version();
                auto tx=d.beginTransaction(revision);tx.setTransform(id,translated(9));
                bool threw=false;allocationCountdown=failAt;
                try{tx.commit();}catch(const std::bad_alloc&){threw=true;}
                allocationCountdown=-1;
                if(threw&&d.find(id)->local.translation[0]==9)
                    failures.push_back({{"allocation_index",failAt},{"revision",d.version()},{"original_revision",revision},{"can_undo",d.canUndo()}});
            }
            evidence["commit_allocation_atomicity"]={{"reproduced_issue",!failures.empty()},{"mutated_after_exception",failures}};
        }
        {
            AuthoringSceneDocument d;auto id=d.createEntity("A");auto payload=d.toJson();
            payload["unknown_scene_field"]="preserve-me";
            payload["entities"][0]["components"]["lod"]=1.5;
            bool accepted=false;int lod=0;bool lost=false;
            try{auto loaded=AuthoringSceneDocument::fromJson(payload);accepted=true;lod=loaded.find(id)->lod;lost=!loaded.toJson().contains("unknown_scene_field");}
            catch(const std::exception&){}
            evidence["schema_silent_coercion"]={{"accepted_fractional_lod",accepted},{"result_lod",lod},
                {"dropped_unknown_field",lost},{"reproduced_issue",accepted&&lod==1&&lost}};
        }
        {
            AuthoringSceneDocument d;
            for(int i=0;i<2500;++i)d.createEntity("fixture");
            const auto bytes=Json{{"kind","engine.authoring_scene"},{"version",1u},{"payload",d.toJson()}}.dump(2).size()+1;
            evidence["scene_save_capacity"]={{"created_entities",2500},{"serialized_bytes",bytes},{"document_limit",documentLimit},
                {"reproduced_issue",bytes>documentLimit}};
        }
        Json timings=Json::array();
        for(int count:{128,512,1024}) {
            AuthoringSceneDocument d;std::optional<SceneEntityId> parent;
            for(int i=0;i<count;++i)parent=d.createEntity("chain",parent);
            const auto start=std::chrono::steady_clock::now();auto result=d.inspectionJson();
            const double ms=std::chrono::duration<double,std::milli>(std::chrono::steady_clock::now()-start).count();
            timings.push_back({{"entities",count},{"inspection_ms",ms},{"result_entities",result["entities"].size()}});
        }
        evidence["hierarchy_inspection_timings"]=timings;
        std::cout<<evidence.dump(2)<<'\n';
    }catch(const std::exception& error){allocationCountdown=-1;std::cerr<<error.what()<<'\n';return 1;}
}
