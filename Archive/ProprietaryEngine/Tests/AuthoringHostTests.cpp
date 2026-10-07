#include "Authoring/AuthoringHost.h"
#include "Authoring/SceneAuthoringAdapter.h"
#include <chrono>
#include <iostream>
#include <stdexcept>
using namespace engine;
namespace {
void check(bool value,const char* message){if(!value)throw std::runtime_error(message);}
Json call(AuthoringHost& host,Json request){return Json::parse(host.handle(request));}
Json identity(AuthoringHost& host,const char* action){auto scene=host.snapshot();return {{"action",action},{"document_id",scene->sceneId()},{"epoch",host.epoch()}};}
Json command(AuthoringHost& host,uint64_t id,const std::string& operation,Json payload,uint64_t revision=0){
    auto request=identity(host,"submit");request["request_id"]=id;request["expected_revision"]=revision?revision:host.snapshot()->version();request["operation"]=operation;request["payload"]=std::move(payload);return request;
}
Json receipt(AuthoringHost& host,uint64_t id){auto request=identity(host,"receipt");request["request_id"]=id;return call(host,request);}
Json done(AuthoringHost& host,uint64_t id){
    for(int i=0;i<4000;++i){auto result=receipt(host,id);const auto status=result.at("status").get<std::string>();if(status!="queued"&&status!="preparing")return result;std::this_thread::sleep_for(std::chrono::milliseconds(1));}
    throw std::runtime_error("Authoring worker timed out");
}
Json create(){return {{"operations",Json::array({{{"type","create"},{"name","Neutral object"}}})}};}
Json page(AuthoringHost& host,uint64_t revision,uint64_t offset=0){auto request=identity(host,"inspect_scene");request["revision"]=revision;request["offset"]=offset;return call(host,request);}
void expectThrow(auto&& f){bool threw=false;try{f();}catch(...){threw=true;}check(threw,"Expected parser rejection");}
}
int main(){try{
    AuthoringLimits small;small.pendingCount=2;small.receiptCount=3;
    AuthoringHost host(AuthoringSceneDocument("shared"),small);
    const auto original=host.snapshot();
    auto first=command(host,1,"apply_transaction",create());
    check(call(host,first).at("status")=="queued","Accept queued transaction before worker starts");
    check(call(host,first).at("status")=="queued","Retry does not duplicate queued work");
    auto conflicting=first;conflicting["payload"]["operations"][0]["name"]="Different intent";
    check(call(host,conflicting).at("status")=="request_id_conflict","Different request cannot claim retained ID");
    auto second=command(host,2,"apply_transaction",create());check(call(host,second).at("status")=="queued","Second admitted");
    check(call(host,command(host,3,"apply_transaction",create())).at("status")=="rejected","Count bound");
    auto cancel=identity(host,"cancel");cancel["request_id"]=uint64_t(2);
    check(call(host,cancel).at("status")=="cancellation_requested","Cancel acknowledged before apply");
    host.start();
    const auto firstResult=done(host,1);
    check(firstResult.at("status")=="applied","Worker applied");check(done(host,2).at("status")=="cancelled_before_apply","Queued cancellation terminal");
    check(host.snapshot()->entityCount()==1&&original->entityCount()==0,"Snapshots own immutable states");
    AuthoringSceneDocument direct("shared");auto directResult=SceneAuthoringAdapter::execute(direct,"apply_transaction",create(),1);
    check(firstResult.at("result")==directResult&&host.snapshot()->toJson()==direct.toJson(),"Host/direct identical domain results");
    check(call(host,first)==firstResult,"Retry retained applied receipt");
    cancel["request_id"]=uint64_t(1);check(call(host,cancel).at("status")=="applied","Late cancel cannot unapply");
    auto staleEpoch=command(host,3,"apply_transaction",create());staleEpoch["epoch"]="old-load-epoch";
    check(call(host,staleEpoch).at("status")=="rejected","Stale load epoch rejected");
    auto staleId=command(host,3,"apply_transaction",create());staleId["document_id"]="another";
    check(call(host,staleId).at("status")=="rejected","Wrong document rejected");
    call(host,command(host,3,"apply_transaction",create(),1));check(done(host,3).at("status")=="rejected","Stale revision at prepare");
    const auto beforePreview=host.snapshot()->toJson();call(host,command(host,4,"preview_transaction",create()));
    check(done(host,4).at("status")=="previewed"&&host.snapshot()->toJson()==beforePreview,"Preview is nonmutating");
    check(receipt(host,1).at("status")=="expired_or_unknown","Receipt retention expires old ID");
    check(call(host,first).at("status")=="expired_or_unknown"&&host.snapshot()->entityCount()==1,"Expired retry never replays");
    call(host,command(host,5,"undo",Json::object()));check(done(host,5).at("status")=="applied"&&host.snapshot()->entityCount()==0,"Shared undo");
    call(host,command(host,6,"redo",Json::object()));check(done(host,6).at("status")=="applied"&&host.snapshot()->entityCount()==1,"Shared redo");
    const uint64_t pinnedRevision=host.snapshot()->version();
    for(uint64_t id=7;id<10;++id){call(host,command(host,id,"apply_transaction",create()));check(done(host,id).at("status")=="applied","Snapshot rotation edit");}
    check(page(host,pinnedRevision).at("total")==1,"Paged reads retain old revision across edits");
    call(host,command(host,10,"apply_transaction",create()));check(done(host,10).at("status")=="applied","Fourth snapshot edit");
    check(page(host,pinnedRevision).at("status")=="rejected","Expired snapshot rejects instead of mixing revisions");
    Json structure={{"operations",Json::array({
        {{"type","reparent"},{"entity",uint64_t(2)},{"parent",uint64_t(1)},{"preserve_world",true}},
        {{"type","metadata"},{"entity",uint64_t(2)},{"mesh","mesh/unit"},{"material","material/unit"},{"collider",""},{"lod",0},{"visible",true},{"tags",{"technical"}}},
        {{"type","duplicate"},{"entity",uint64_t(1)}}})}};
    call(host,command(host,11,"apply_transaction",structure));
    const auto structuralReceipt=done(host,11);check(structuralReceipt.at("status")=="applied","Structural adapter operations");
    const auto duplicate=structuralReceipt.at("result").at("created_entities").at(0).get<uint64_t>();
    check(host.snapshot()->find(2)->parent==1&&host.snapshot()->find(2)->mesh=="mesh/unit"&&host.snapshot()->find(duplicate),"Reparent, metadata and duplicate visible in shared document");
    Json remove={{"operations",Json::array({{{"type","delete"},{"entity",duplicate}}})}};
    call(host,command(host,12,"apply_transaction",remove));check(done(host,12).at("status")=="applied"&&!host.snapshot()->find(duplicate),"Delete through shared adapter");
    call(host,command(host,13,"undo",Json::object()));check(done(host,13).at("status")=="applied"&&host.snapshot()->find(duplicate),"Structural delete undo");
    host.stop();host.stop();check(call(host,command(host,14,"apply_transaction",create())).at("status")=="rejected","Closed host rejects new work");
    AuthoringHost fresh(AuthoringSceneDocument("shared"));check(fresh.epoch()!=host.epoch(),"New owner gets fresh epoch");
    // Deterministic pending shutdown without scheduling races.
    call(fresh,command(fresh,1,"apply_transaction",create()));fresh.stop();
    check(receipt(fresh,1).at("status")=="cancelled_before_apply"&&fresh.snapshot()->entityCount()==0,"Shutdown cancels pending work");
    // Aggregate pending and receipt reservations independently cap admission.
    AuthoringLimits bytes;bytes.pendingBytes=400;
    AuthoringHost byteHost(AuthoringSceneDocument{},bytes);
    auto byteRequest=command(byteHost,1,"apply_transaction",create());
    check(byteRequest.dump().size()<400&&call(byteHost,byteRequest).at("status")=="queued","First byte-budget request");
    check(call(byteHost,command(byteHost,2,"apply_transaction",create())).at("status")=="rejected","Aggregate pending byte bound");
    AuthoringLimits reservation;reservation.receiptBytes=reservation.resultBytes+500;
    AuthoringHost reserveHost(AuthoringSceneDocument{},reservation);
    check(call(reserveHost,command(reserveHost,1,"apply_transaction",create())).at("status")=="queued","Reserved terminal receipt");
    check(call(reserveHost,command(reserveHost,2,"apply_transaction",create())).at("status")=="rejected","Never evict pending receipt");
    // Flat/deep extraction, byte-limited pages, and independent expected positions.
    AuthoringSceneDocument large("bounded");auto tx=large.beginTransaction(1);std::optional<SceneEntityId> parent;
    for(size_t i=0;i<1024;++i){auto id=tx.createEntity("Entity",i<65?parent:std::nullopt);if(i<65){parent=id;SceneTransform transform;transform.translation={1,0,0};tx.setTransform(id,transform);}}
    check(tx.commit().applied,"Maximum scene seed");
    AuthoringHost big(large);const auto pinned=big.snapshot();check(pinned->worldMatrix(65)[12]==65,"Cached deep chain independent oracle");
    const auto readStart=std::chrono::steady_clock::now();uint64_t offset=0,total=0;
    do{auto result=page(big,pinned->version(),offset);check(result.dump().size()<=64*1024&&result.at("entities").size()<=64,"Bounded extraction page");total+=result.at("entities").size();if(result.at("next_offset").is_null())break;offset=result.at("next_offset").get<uint64_t>();}while(true);
    check(total==1024,"Every flat/deep entity inspected once");
    const auto readNs=std::chrono::duration_cast<std::chrono::nanoseconds>(std::chrono::steady_clock::now()-readStart).count();
    // Stop the active owner with accepted work; no request remains pending and
    // no mutation may happen after join. Any operation already applied stays applied.
    Json move={{"operations",Json::array({{{"type","set_transform"},{"entity",uint64_t(1)},
        {"transform",AuthoringSceneDocument::transformToJson(SceneTransform{})}}})}};
    for(uint64_t id=1;id<=16;++id)check(call(big,command(big,id,"apply_transaction",move)).at("status")=="queued","Shutdown workload admitted");
    big.start();big.stop();const auto stoppedRevision=big.snapshot()->version();
    for(uint64_t id=1;id<=16;++id){const auto state=receipt(big,id).at("status").get<std::string>();check(state=="applied"||state=="cancelled_before_apply"||state=="rejected","Active-owner shutdown has terminal receipts");}
    check(big.snapshot()->version()==stoppedRevision&&call(big,{{"action","describe"}}).at("usage").at("pending_count")==0,"Joined owner cannot publish late work");
    AuthoringLimits pageBytes;pageBytes.resultBytes=1024;pageBytes.receiptBytes=1024*4;
    AuthoringHost tinyPages(large,pageBytes);auto tiny=page(tinyPages,large.version());check(tiny.at("entities").size()<64&&tiny.dump().size()<=1024,"Page byte limit wins before entity count");
    // Candidate can succeed in the domain yet exceed a host receipt budget. The
    // outer publication boundary must preserve the live document in that case.
    Json deletes={{"operations",Json::array({{{"type","delete"},{"entity",uint64_t(1)}}})}};
    // Deleting one root changes 1,024 IDs: valid domain edit, oversized receipt.
    AuthoringSceneDocument tree("receipt-capacity");auto treeTx=tree.beginTransaction(1);auto root=treeTx.createEntity("Root");
    for(size_t i=1;i<1024;++i)treeTx.createEntity("Child",root);check(treeTx.commit().applied,"Receipt capacity tree");
    AuthoringHost receiptCap(tree,pageBytes);receiptCap.start();call(receiptCap,command(receiptCap,1,"apply_transaction",deletes));
    check(done(receiptCap,1).at("status")=="rejected"&&receiptCap.snapshot()->entityCount()==1024,"Oversized receipt does not publish candidate");
    expectThrow([]{parseAuthoringRequest(R"({"a":1,"a":2})");});
    expectThrow([]{parseAuthoringRequest(std::string(33,'[')+"0"+std::string(33,']'));});
    expectThrow([]{parseAuthoringRequest(std::string(256*1024+1,' '));});
    std::cout<<"PASS: direct/live equivalence, epochs, revision conflicts, retry/cancel/expiry, queue/receipt/page bounds, pinned snapshots, shutdown\n";
    std::cout<<"flat_deep_1024_page_inspection_ns="<<readNs<<"\n";
    std::cout<<"last_owner_timing="<<call(host,{{"action","describe"}}).at("timing").dump()<<"\n";
}catch(const std::exception& error){std::cerr<<"FAIL: "<<error.what()<<'\n';return 1;}}
