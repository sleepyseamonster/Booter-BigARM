#include "Authoring/SceneDocument.h"
#include "Persistence/JsonLifetime.h"
#include <algorithm>
#include <cmath>
#include <limits>
#include <unordered_map>
#include <unordered_set>
#include <type_traits>

namespace engine {
namespace {
void fields(const Json& value,std::initializer_list<const char*> allowed,std::initializer_list<const char*> required={}){
    if(!value.is_object())throw SceneDocumentError("Expected object");
    for(const auto& [key,_]:value.items())if(std::none_of(allowed.begin(),allowed.end(),[&](const char* a){return key==a;}))throw SceneDocumentError("Unknown field: "+key);
    for(const char* key:required)if(!value.contains(key))throw SceneDocumentError(std::string("Missing field: ")+key);
}
template<size_t N> std::array<float,N> numbers(const Json& j){
    if(!j.is_array()||j.size()!=N)throw SceneDocumentError("Invalid transform array");
    std::array<float,N> out{};
    for(size_t i=0;i<N;++i){if(!j[i].is_number())throw SceneDocumentError("Expected numeric transform value");out[i]=j[i].get<float>();if(!std::isfinite(out[i]))throw SceneDocumentError("Non-finite transform value");}
    return out;
}
std::array<float,4> normalized(std::array<float,4> q){
    double sum=0;for(float x:q){if(!std::isfinite(x))throw SceneDocumentError("Invalid quaternion");sum+=double(x)*x;}
    const double length=std::sqrt(sum);if(length<1e-6)throw SceneDocumentError("Invalid quaternion");
    // Preserve already-normalized serialized floats across repeated save/load.
    if(std::abs(length-1)>1e-7)for(float& x:q)x=float(x/length);
    return q;
}
void boundedString(const std::string& value,size_t limit,bool empty,const char* label){
    if((!empty&&value.empty())||value.size()>limit||value.find('\0')!=value.npos)throw SceneDocumentError(std::string("Invalid ")+label);
}
void affineChecked(const AffineMatrix& m){try{validateRenderAffine(m);}catch(const std::invalid_argument& e){throw SceneDocumentError(e.what(),SceneErrorCode::UnsupportedTransform);}}
uint64_t identity(const Json& value){if(!value.is_number_unsigned()||value.get<uint64_t>()==0)throw SceneDocumentError("Expected nonzero unsigned identity");return value.get<uint64_t>();}
}
struct AuthoringSceneDocument::State {
    std::vector<SceneEntity> entities;
    std::unordered_map<SceneEntityId,size_t> index;
    std::vector<AffineMatrix> worlds;
    std::vector<size_t> order;
    size_t bytes=0;
};
AuthoringSceneDocument::AuthoringSceneDocument(std::string sceneId):sceneId_(std::move(sceneId)),state_(buildState({})){
    boundedString(sceneId_,128,false,"scene id");
}
void AuthoringSceneDocument::validateTransform(const SceneTransform& t){
    for(float x:t.translation)if(!std::isfinite(x))throw SceneDocumentError("Invalid translation");
    for(float x:t.scale)if(!std::isfinite(x)||std::abs(x)<1e-6f)throw SceneDocumentError("Invalid scale");
    (void)normalized(t.rotation);
}
void AuthoringSceneDocument::validateEntity(const SceneEntity& e){
    if(!e.id)throw SceneDocumentError("Invalid entity id");
    boundedString(e.name,128,false,"entity name");
    for(const auto* ref:{&e.mesh,&e.material,&e.collider})boundedString(*ref,256,true,"asset reference");
    if(e.lod<0||e.lod>31)throw SceneDocumentError("LOD must be an integer in 0..31");
    if(e.tags.size()>64)throw SceneDocumentError("Too many tags");
    for(const auto& tag:e.tags)boundedString(tag,64,false,"tag");
    validateTransform(e.local);
}
AffineMatrix AuthoringSceneDocument::matrixOf(const SceneTransform& t){
    validateTransform(t);const auto q=normalized(t.rotation);
    // Normalize in double for orthonormal derived axes, even when stored floats round.
    double n=0;for(float a:q)n+=double(a)*a;n=std::sqrt(n);
    const double x=q[0]/n,y=q[1]/n,z=q[2]/n,w=q[3]/n;
    AffineMatrix m={(1-2*(y*y+z*z))*t.scale[0],2*(x*y+z*w)*t.scale[0],2*(x*z-y*w)*t.scale[0],0,
                    2*(x*y-z*w)*t.scale[1],(1-2*(x*x+z*z))*t.scale[1],2*(y*z+x*w)*t.scale[1],0,
                    2*(x*z+y*w)*t.scale[2],2*(y*z-x*w)*t.scale[2],(1-2*(x*x+y*y))*t.scale[2],0,
                    t.translation[0],t.translation[1],t.translation[2],1};
    return m;
}
SceneTransform AuthoringSceneDocument::transformOf(const AffineMatrix& m){
    affineChecked(m);SceneTransform t;auto r=m;
    for(size_t c=0;c<3;++c){double s=std::hypot(m[c*4],m[c*4+1],m[c*4+2]);if(c==0&&affineDeterminant(m)<0)s=-s;t.scale[c]=float(s);for(size_t k=0;k<3;++k)r[c*4+k]/=s;t.translation[c]=float(m[12+c]);}
    for(size_t a=0;a<3;++a)for(size_t b=a+1;b<3;++b){double dot=0;for(size_t k=0;k<3;++k)dot+=r[a*4+k]*r[b*4+k];if(std::abs(dot)>1e-6)throw SceneDocumentError("Affine transform contains shear; local TRS cannot preserve it",SceneErrorCode::UnsupportedTransform);}
    std::array<double,4> q{};const double trace=r[0]+r[5]+r[10];
    if(trace>0){const double s=std::sqrt(trace+1)*2;q={ (r[6]-r[9])/s,(r[8]-r[2])/s,(r[1]-r[4])/s,.25*s};}
    else if(r[0]>r[5]&&r[0]>r[10]){const double s=std::sqrt(1+r[0]-r[5]-r[10])*2;q={.25*s,(r[4]+r[1])/s,(r[8]+r[2])/s,(r[6]-r[9])/s};}
    else if(r[5]>r[10]){const double s=std::sqrt(1+r[5]-r[0]-r[10])*2;q={(r[4]+r[1])/s,.25*s,(r[9]+r[6])/s,(r[8]-r[2])/s};}
    else {const double s=std::sqrt(1+r[10]-r[0]-r[5])*2;q={(r[8]+r[2])/s,(r[9]+r[6])/s,.25*s,(r[1]-r[4])/s};}
    for(size_t k=0;k<4;++k)t.rotation[k]=float(q[k]);t.rotation=normalized(t.rotation);validateTransform(t);
    const auto rebuilt=matrixOf(t);
    for(size_t c=0;c<4;++c){double scale=1;for(size_t k=0;k<4;++k)scale=std::max(scale,std::abs(m[c*4+k]));for(size_t k=0;k<4;++k)if(std::abs(rebuilt[c*4+k]-m[c*4+k])>scale*1e-6)throw SceneDocumentError("TRS conversion exceeds representability tolerance",SceneErrorCode::UnsupportedTransform);}
    return t;
}
std::shared_ptr<const AuthoringSceneDocument::State> AuthoringSceneDocument::buildState(std::vector<SceneEntity> entities){
    if(entities.size()>maxEntities)throw SceneDocumentError("Scene entity limit exceeded",SceneErrorCode::Capacity);
    auto s=std::make_shared<State>();s->entities=std::move(entities);
    std::sort(s->entities.begin(),s->entities.end(),[](const auto& a,const auto& b){return a.id<b.id;});
    const auto count=s->entities.size();s->index.reserve(count);s->worlds.resize(count);s->order.reserve(count);
    std::vector<std::vector<size_t>> children(count);std::vector<size_t> depth(count);
    for(size_t i=0;i<count;++i){auto& e=s->entities[i];validateEntity(e);e.local.rotation=normalized(e.local.rotation);if(!s->index.emplace(e.id,i).second)throw SceneDocumentError("Duplicate entity id");}
    for(size_t i=0;i<count;++i){const auto& e=s->entities[i];if(!e.parent)s->order.push_back(i);else {auto p=s->index.find(*e.parent);if(p==s->index.end())throw SceneDocumentError("Entity parent does not exist",SceneErrorCode::Hierarchy);children[p->second].push_back(i);}}
    for(size_t cursor=0;cursor<s->order.size();++cursor){const auto i=s->order[cursor];const auto& e=s->entities[i];auto m=matrixOf(e.local);
        if(e.parent){const auto p=s->index.at(*e.parent);depth[i]=depth[p]+1;if(depth[i]>maxParentLinks)throw SceneDocumentError("Scene hierarchy depth limit exceeded",SceneErrorCode::Capacity);m=multiplyAffine(s->worlds[p],m);}
        affineChecked(m);s->worlds[i]=m;s->order.insert(s->order.end(),children[i].begin(),children[i].end());
    }
    if(s->order.size()!=count)throw SceneDocumentError("Scene hierarchy contains a cycle",SceneErrorCode::Hierarchy);
    // Conservative retained payload accounting (shared states counted again per history entry).
    s->bytes=sizeof(State)+s->entities.capacity()*sizeof(SceneEntity)+s->worlds.capacity()*sizeof(AffineMatrix)+s->order.capacity()*sizeof(size_t)+s->index.bucket_count()*sizeof(void*)+count*64;
    for(const auto& e:s->entities){s->bytes+=e.name.capacity()+e.mesh.capacity()+e.material.capacity()+e.collider.capacity()+4+e.tags.capacity()*sizeof(std::string);for(const auto& t:e.tags)s->bytes+=t.capacity()+1;}
    return s;
}
const SceneEntity* AuthoringSceneDocument::find(SceneEntityId id)const noexcept{const auto it=state_->index.find(id);return it==state_->index.end()?nullptr:&state_->entities[it->second];}
const AffineMatrix& AuthoringSceneDocument::worldMatrix(SceneEntityId id)const{const auto it=state_->index.find(id);if(it==state_->index.end())throw SceneDocumentError("Entity does not exist");return state_->worlds[it->second];}
SceneTransform AuthoringSceneDocument::worldTransform(SceneEntityId id)const{return transformOf(worldMatrix(id));}
std::vector<SceneEntity> AuthoringSceneDocument::entities()const{return state_->entities;}
Json AuthoringSceneDocument::transformToJson(const SceneTransform& t){
    validateTransform(t);PreparedJson out;
    out.value["translation"]=t.translation;out.value["rotation"]=t.rotation;out.value["scale"]=t.scale;
    return std::move(out.value);
}
SceneTransform AuthoringSceneDocument::transformFromJson(const Json& j){
    fields(j,{"translation","rotation","scale"},{"translation","rotation","scale"});
    SceneTransform t{numbers<3>(j.at("translation")),numbers<4>(j.at("rotation")),numbers<3>(j.at("scale"))};validateTransform(t);t.rotation=normalized(t.rotation);return t;
}
Json AuthoringSceneDocument::entityToJson(const SceneEntity& e){
    validateEntity(e);PreparedJson out,components;
    out.value["id"]=e.id;out.value["name"]=e.name;out.value["parent"]=e.parent?Json(*e.parent):Json(nullptr);
    out.value["transform"]=transformToJson(e.local);
    components.value["mesh"]=e.mesh;components.value["material"]=e.material;components.value["collider"]=e.collider;
    components.value["lod"]=e.lod;components.value["visible"]=e.visible;
    out.value["components"]=std::move(components.value);out.value["tags"]=e.tags;
    return std::move(out.value);
}
Json AuthoringSceneDocument::serialize(const State& state,uint64_t revision,uint64_t next)const{
    PreparedJson out;
    out.value["scene_id"]=sceneId_;out.value["revision"]=revision;out.value["next_entity_id"]=next;
    auto& entities=out.value["entities"];entities=Json::array();
    for(const auto& e:state.entities){auto value=entityToJson(e);JsonReleaseGuard guard{value};entities.push_back(std::move(value));}
    return std::move(out.value);
}
void AuthoringSceneDocument::validateSave(const State& state,uint64_t revision,uint64_t next)const{
    if(!revision||revision==UINT64_MAX||!next||next==UINT64_MAX)throw SceneDocumentError("Scene identity/revision exhausted",SceneErrorCode::Exhausted);
    if(!state.entities.empty()&&next<=state.entities.back().id)throw SceneDocumentError("Next entity id must remain monotonic");
    // Reserve counter-width growth now so an old inverse remains saveable after
    // later revisions/ID allocation. The actual written envelope is no larger.
    PreparedJson envelope;
    envelope.value["kind"]="engine.authoring_scene";envelope.value["version"]=schemaVersion;
    envelope.value["payload"]=serialize(state,UINT64_MAX-1,UINT64_MAX-1);
    if(envelope.value.dump(2).size()+1>documentLimit)throw SceneDocumentError("Scene exceeds serialized document budget",SceneErrorCode::Capacity);
}
Json AuthoringSceneDocument::toJson()const{return serialize(*state_,version_,nextEntityId_);}
Json AuthoringSceneDocument::inspectionJson(bool world)const{PreparedJson out;out.value=toJson();if(world)for(auto& e:out.value["entities"])e["world_matrix"]=worldMatrix(e["id"].get<SceneEntityId>());return std::move(out.value);}
AuthoringSceneDocument AuthoringSceneDocument::fromJson(const Json& p){
    fields(p,{"scene_id","revision","next_entity_id","entities"},{"scene_id","revision","next_entity_id","entities"});
    if(!p.at("scene_id").is_string()||!p.at("entities").is_array())throw SceneDocumentError("Invalid scene schema");
    AuthoringSceneDocument out(p.at("scene_id").get<std::string>());out.version_=identity(p.at("revision"));out.nextEntityId_=identity(p.at("next_entity_id"));
    if(p.at("entities").size()>maxEntities)throw SceneDocumentError("Scene entity limit exceeded",SceneErrorCode::Capacity);
    std::vector<SceneEntity> entities;
    for(const auto& j:p.at("entities")){
        fields(j,{"id","name","parent","transform","components","tags"},{"id","name","transform"});
        SceneEntity e;e.id=identity(j.at("id"));if(!j.at("name").is_string())throw SceneDocumentError("Expected entity name");e.name=j.at("name").get<std::string>();
        if(j.contains("parent")&&!j.at("parent").is_null())e.parent=identity(j.at("parent"));e.local=transformFromJson(j.at("transform"));
        if(j.contains("components")){const auto& c=j.at("components");fields(c,{"mesh","material","collider","lod","visible"});
            for(const auto& [key,target]:{std::pair{"mesh",&e.mesh},{"material",&e.material},{"collider",&e.collider}})if(c.contains(key)){if(!c.at(key).is_string())throw SceneDocumentError("Expected asset reference string");*target=c.at(key).get<std::string>();}
            if(c.contains("lod")){const auto& lod=c.at("lod");if(!lod.is_number_integer()||lod<0||lod>31)throw SceneDocumentError("LOD must be an integer in 0..31");e.lod=lod.get<int32_t>();}
            if(c.contains("visible")){if(!c.at("visible").is_boolean())throw SceneDocumentError("Expected visibility boolean");e.visible=c.at("visible").get<bool>();}
        }
        if(j.contains("tags")){if(!j.at("tags").is_array()||j.at("tags").size()>64)throw SceneDocumentError("Invalid tags");for(const auto& tag:j.at("tags")){if(!tag.is_string())throw SceneDocumentError("Expected tag string");e.tags.push_back(tag.get<std::string>());}}
        entities.push_back(std::move(e));
    }
    out.state_=buildState(std::move(entities));out.validateSave(*out.state_,out.version_,out.nextEntityId_);return out;
}
void AuthoringSceneDocument::save(const std::filesystem::path& path)const{validateSave(*state_,version_,nextEntityId_);writeDocument(path,"engine.authoring_scene",toJson(),schemaVersion);}
AuthoringSceneDocument AuthoringSceneDocument::load(const std::filesystem::path& path){return fromJson(readDocument(path,"engine.authoring_scene",schemaVersion));}
size_t AuthoringSceneDocument::historyBytes(const std::vector<HistoryEntry>& entries)noexcept{size_t bytes=entries.size()*sizeof(HistoryEntry);for(const auto& e:entries)bytes+=e.before->bytes+e.after->bytes;return bytes;}
size_t AuthoringSceneDocument::historyBytes()const noexcept{return historyBytes(undo_)+historyBytes(redo_);}
AuthoringSceneDocument::PreparedEdit AuthoringSceneDocument::prepareState(std::shared_ptr<const State> state,uint64_t next,uint64_t expected){
    if(!expected||expected!=version_)throw SceneDocumentError("Scene version mismatch",SceneErrorCode::Conflict);
    PreparedEdit p;p.owner_=this;p.base_=state_;p.next_=std::move(state);p.nextId_=next;p.result_={true,version_,version_,{}, {}};
    p.changes_=p.next_->entities!=state_->entities||next!=nextEntityId_;
    if(!p.changes_)return p;
    if(version_>=UINT64_MAX-1)throw SceneDocumentError("Scene revision exhausted",SceneErrorCode::Exhausted);
    p.result_.afterVersion=version_+1;validateSave(*p.next_,p.result_.afterVersion,next);
    // Include descendants whose derived matrices changed and IDs removed by a structural edit.
    for(const auto& e:state_->entities){auto i=p.next_->index.find(e.id);if(i==p.next_->index.end()||e!=p.next_->entities[i->second]||worldMatrix(e.id)!=p.next_->worlds[i->second])p.result_.changedEntities.push_back(e.id);}
    for(const auto& e:p.next_->entities)if(!state_->index.contains(e.id))p.result_.changedEntities.push_back(e.id);
    std::sort(p.result_.changedEntities.begin(),p.result_.changedEntities.end());
    p.undo_=undo_;
    if(p.next_->entities!=state_->entities)p.undo_.push_back({state_,p.next_});
    while(p.undo_.size()>1&&(p.undo_.size()>maxHistoryEntries||historyBytes(p.undo_)>historyByteLimit))p.undo_.erase(p.undo_.begin());
    if(historyBytes(p.undo_)>historyByteLimit)throw SceneDocumentError("Required inverse exceeds history budget",SceneErrorCode::Capacity);
    return p;
}
bool AuthoringSceneDocument::PreparedEdit::apply()noexcept{
    if(consumed_||!owner_||!base_||!next_)return false;consumed_=true;
    if(owner_->state_!=base_||owner_->version_!=result_.beforeVersion)return false;
    if(changes_){owner_->state_.swap(next_);owner_->undo_.swap(undo_);owner_->redo_.swap(redo_);owner_->version_=result_.afterVersion;owner_->nextEntityId_=nextId_;}
    return true;
}
AuthoringSceneDocument::PreparedEdit AuthoringSceneDocument::prepareHistory(bool undo,uint64_t expected){
    if(!expected||expected!=version_)throw SceneDocumentError("Scene version mismatch",SceneErrorCode::Conflict);
    const auto& source=undo?undo_:redo_;if(source.empty())throw SceneDocumentError("History is empty");
    const auto entry=source.back();auto p=prepareState(undo?entry.before:entry.after,nextEntityId_,expected);
    p.undo_=undo_;p.redo_=redo_;
    if(undo){p.undo_.pop_back();p.redo_.push_back(entry);}else{p.redo_.pop_back();p.undo_.push_back(entry);}
    if(historyBytes(p.undo_)+historyBytes(p.redo_)>historyByteLimit)throw SceneDocumentError("History transfer exceeds budget",SceneErrorCode::Capacity);
    return p;
}
AuthoringSceneDocument::PreparedEdit AuthoringSceneDocument::prepareUndo(uint64_t version){return prepareHistory(true,version);}
AuthoringSceneDocument::PreparedEdit AuthoringSceneDocument::prepareRedo(uint64_t version){return prepareHistory(false,version);}
bool AuthoringSceneDocument::undo(uint64_t version){if(!version||version!=version_||undo_.empty())return false;return prepareUndo(version).apply();}
bool AuthoringSceneDocument::redo(uint64_t version){if(!version||version!=version_||redo_.empty())return false;return prepareRedo(version).apply();}
AuthoringSceneDocument::Transaction::Transaction(AuthoringSceneDocument& owner,uint64_t expected):owner_(&owner),expectedVersion_(expected),nextId_(owner.nextEntityId_),base_(owner.state_),candidate_(base_->entities){}
void AuthoringSceneDocument::Transaction::checkOpen()const{if(closed_)throw SceneDocumentError("Transaction is closed",SceneErrorCode::Closed);}
SceneEntity& AuthoringSceneDocument::Transaction::edit(SceneEntityId id){auto it=std::find_if(candidate_.begin(),candidate_.end(),[&](const auto& e){return e.id==id;});if(it==candidate_.end())throw SceneDocumentError("Entity does not exist");return *it;}
SceneEntityId AuthoringSceneDocument::Transaction::createEntity(std::string name,std::optional<SceneEntityId> parent){return stage([&]{
    if(nextId_>=UINT64_MAX-1)throw SceneDocumentError("Entity id exhausted",SceneErrorCode::Exhausted);
    if(candidate_.size()>=maxEntities)throw SceneDocumentError("Scene entity limit exceeded",SceneErrorCode::Capacity);
    if(parent)(void)edit(*parent);SceneEntity e;e.id=nextId_;e.name=std::move(name);e.parent=parent;validateEntity(e);candidate_.push_back(std::move(e));return nextId_++;
});}
void AuthoringSceneDocument::Transaction::setTransform(SceneEntityId id,SceneTransform t){stage([&]{validateTransform(t);t.rotation=normalized(t.rotation);edit(id).local=t;});}
void AuthoringSceneDocument::Transaction::setMetadata(SceneEntityId id,std::string mesh,std::string material,std::string collider,int32_t lod,bool visible,std::vector<std::string> tags){stage([&]{auto e=edit(id);e.mesh=std::move(mesh);e.material=std::move(material);e.collider=std::move(collider);e.lod=lod;e.visible=visible;e.tags=std::move(tags);validateEntity(e);edit(id)=std::move(e);});}
void AuthoringSceneDocument::Transaction::eraseEntity(SceneEntityId id){stage([&]{
    (void)edit(id);const auto s=buildState(candidate_);std::unordered_set<SceneEntityId> removed{id};
    for(size_t i:s->order){const auto& e=s->entities[i];if(e.parent&&removed.contains(*e.parent))removed.insert(e.id);}
    std::erase_if(candidate_,[&](const auto& e){return removed.contains(e.id);});
});}
void AuthoringSceneDocument::Transaction::reparent(SceneEntityId id,std::optional<SceneEntityId> parent,bool preserve){stage([&]{
    const auto s=buildState(candidate_);const auto world=s->worlds.at(s->index.at(edit(id).id));if(parent)(void)edit(*parent);
    for(auto ancestor=parent;ancestor;ancestor=s->entities[s->index.at(*ancestor)].parent)
        if(*ancestor==id)throw SceneDocumentError("Scene hierarchy contains a cycle",SceneErrorCode::Hierarchy);
    edit(id).parent=parent;
    if(preserve){const auto local=parent?multiplyAffine(inverseAffine(s->worlds.at(s->index.at(*parent))),world):world;edit(id).local=transformOf(local);}
});}
void AuthoringSceneDocument::Transaction::setWorldTransform(SceneEntityId id,const AffineMatrix& m){setWorldTransforms({{id,m}});}
void AuthoringSceneDocument::Transaction::setWorldTransforms(const std::vector<std::pair<SceneEntityId,AffineMatrix>>& edits){stage([&]{
    const auto s=buildState(candidate_);std::unordered_map<SceneEntityId,AffineMatrix> desired;
    for(const auto& [id,m]:edits){(void)edit(id);affineChecked(m);if(!desired.emplace(id,m).second)throw SceneDocumentError("Duplicate world-space edit");}
    auto worlds=s->worlds;
    // Parent-first desired worlds prevent double-applying selected-parent motion.
    for(size_t i:s->order){const auto& e=s->entities[i];const auto parent=e.parent?worlds[s->index.at(*e.parent)]:identityAffine();
        if(auto target=desired.find(e.id);target!=desired.end()){auto local=transformOf(multiplyAffine(inverseAffine(parent),target->second));edit(e.id).local=local;worlds[i]=multiplyAffine(parent,matrixOf(local));}
        else worlds[i]=multiplyAffine(parent,matrixOf(e.local));
    }
});}
SceneEntityId AuthoringSceneDocument::Transaction::duplicate(SceneEntityId id){return stage([&]{
    (void)edit(id);const auto s=buildState(candidate_);std::unordered_map<SceneEntityId,SceneEntityId> copied;
    for(size_t i:s->order){const auto& e=s->entities[i];if(e.id!=id&&(!e.parent||!copied.contains(*e.parent)))continue;
        if(nextId_>=UINT64_MAX-1||candidate_.size()>=maxEntities)throw SceneDocumentError("Duplicate exceeds identity/entity capacity",SceneErrorCode::Capacity);
        auto copy=e;copy.id=nextId_++;if(e.parent&&copied.contains(*e.parent))copy.parent=copied.at(*e.parent);copied.emplace(e.id,copy.id);candidate_.push_back(std::move(copy));
    }return copied.at(id);
});}
AuthoringSceneDocument::PreparedEdit AuthoringSceneDocument::Transaction::prepare(){return stage([&]{
    closed_=true;if(owner_->state_!=base_)throw SceneDocumentError("Scene changed during transaction",SceneErrorCode::Conflict);
    return owner_->prepareState(buildState(std::move(candidate_)),nextId_,expectedVersion_);
});}
SceneTransactionResult AuthoringSceneDocument::Transaction::commit(){
    try{auto prepared=prepare();auto result=prepared.result();if(!prepared.apply())return {false,owner_->version_,owner_->version_,{},"Scene changed during preparation"};return result;}
    catch(const SceneDocumentError& e){return {false,owner_->version_,owner_->version_,{},e.what()};}
}
SceneEntityId AuthoringSceneDocument::createEntity(std::string name,std::optional<SceneEntityId> parent){auto tx=beginTransaction(version_);const auto id=tx.createEntity(std::move(name),parent);auto prepared=tx.prepare();if(!prepared.apply())throw SceneDocumentError("Scene changed during creation",SceneErrorCode::Conflict);return id;}
bool AuthoringSceneDocument::eraseEntity(SceneEntityId id){if(!find(id))return false;auto tx=beginTransaction(version_);tx.eraseEntity(id);auto prepared=tx.prepare();return prepared.apply();}
bool AuthoringSceneDocument::updateEntityMetadata(SceneEntityId id,std::string mesh,std::string material,std::string collider,int32_t lod,bool visible,std::vector<std::string> tags){
    auto tx=beginTransaction(version_);tx.setMetadata(id,std::move(mesh),std::move(material),std::move(collider),lod,visible,std::move(tags));auto prepared=tx.prepare();const bool changed=prepared.result().beforeVersion!=prepared.result().afterVersion;return prepared.apply()&&changed;
}
static_assert(std::is_nothrow_move_constructible_v<SceneTransactionResult>);
}
