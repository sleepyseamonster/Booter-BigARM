#pragma once
#include "Core/AffineTransform.h"
#include "Persistence/Document.h"
#include <memory>
#include <optional>
#include <string>
#include <vector>
namespace engine {
enum class SceneErrorCode {Validation,Hierarchy,Capacity,Conflict,Closed,UnsupportedTransform,Exhausted};
struct SceneDocumentError:std::runtime_error {
    SceneErrorCode code;
    explicit SceneDocumentError(const std::string& message,SceneErrorCode category=SceneErrorCode::Validation):std::runtime_error(message),code(category){}
};
using SceneEntityId=std::uint64_t;
struct SceneTransform {
    std::array<float,3> translation{0,0,0};
    std::array<float,4> rotation{0,0,0,1}; // x,y,z,w
    std::array<float,3> scale{1,1,1};
    bool operator==(const SceneTransform&)const=default;
};
struct SceneEntity {
    SceneEntityId id=0;std::string name;std::optional<SceneEntityId> parent;SceneTransform local;
    std::string mesh,material,collider;std::int32_t lod=0;bool visible=true;std::vector<std::string> tags;
    bool operator==(const SceneEntity&)const=default;
};
struct SceneTransactionResult {
    bool applied=false;uint64_t beforeVersion=0,afterVersion=0;std::vector<SceneEntityId> changedEntities;std::string error;
};
// One owner publishes immutable validated states. Copies are independent read snapshots.
// Renderer/physics handles never enter this domain. History is local to this load.
class AuthoringSceneDocument {
    struct State;
    struct HistoryEntry {std::shared_ptr<const State> before,after;};
public:
    static constexpr unsigned schemaVersion=1;
    static constexpr size_t maxEntities=1024,maxParentLinks=64,maxHistoryEntries=64,historyByteLimit=16*1024*1024;
    explicit AuthoringSceneDocument(std::string sceneId="authoring");
    const std::string& sceneId()const noexcept{return sceneId_;}
    uint64_t version()const noexcept{return version_;}
    uint64_t nextEntityId()const noexcept{return nextEntityId_;}
    SceneEntityId createEntity(std::string name,std::optional<SceneEntityId> parent={});
    bool eraseEntity(SceneEntityId); // Atomic subtree removal, undo restores it.
    const SceneEntity* find(SceneEntityId)const noexcept;
    bool updateEntityMetadata(SceneEntityId,std::string mesh,std::string material,std::string collider,int32_t lod,bool visible,std::vector<std::string> tags);
    std::vector<SceneEntity> entities()const;
    size_t entityCount()const noexcept;
    const SceneEntity& entityAt(size_t index)const;
    // Cheap immutable-state view without retaining undo/redo history.
    AuthoringSceneDocument readSnapshot()const;
    const AffineMatrix& worldMatrix(SceneEntityId)const;
    SceneTransform worldTransform(SceneEntityId)const; // Compatibility: rejects unrepresentable shear.
    static AffineMatrix matrixOf(const SceneTransform&);
    static SceneTransform transformOf(const AffineMatrix&);
    class PreparedEdit {
    public:
        PreparedEdit(PreparedEdit&&)noexcept=default;PreparedEdit& operator=(PreparedEdit&&)noexcept=default;
        PreparedEdit(const PreparedEdit&)=delete;PreparedEdit& operator=(const PreparedEdit&)=delete;
        const SceneTransactionResult& result()const noexcept{return result_;}
        bool apply()noexcept; // No allocation/callback/serialization; false means stale or consumed.
    private:
        friend class AuthoringSceneDocument;
        PreparedEdit()=default;
        AuthoringSceneDocument* owner_=nullptr;
        std::shared_ptr<const State> base_,next_;
        std::vector<HistoryEntry> undo_,redo_;
        SceneTransactionResult result_;
        uint64_t nextId_=0;
        bool consumed_=false,changes_=false;
    };
    class Transaction {
    public:
        Transaction(AuthoringSceneDocument&,uint64_t expectedVersion);
        Transaction(const Transaction&)=delete;Transaction& operator=(const Transaction&)=delete;
        // Candidate IDs are provisional until commit; committed IDs are never reused.
        SceneEntityId createEntity(std::string name,std::optional<SceneEntityId> parent={});
        void eraseEntity(SceneEntityId);
        void setTransform(SceneEntityId,SceneTransform);
        void setWorldTransform(SceneEntityId,const AffineMatrix&);
        void setWorldTransforms(const std::vector<std::pair<SceneEntityId,AffineMatrix>>&);
        void setMetadata(SceneEntityId,std::string mesh,std::string material,std::string collider,int32_t lod,bool visible,std::vector<std::string> tags);
        void reparent(SceneEntityId,std::optional<SceneEntityId>,bool preserveWorld=true);
        SceneEntityId duplicate(SceneEntityId); // Whole subtree, fresh IDs.
        PreparedEdit prepare();
        SceneTransactionResult commit();
        void rollback()noexcept{closed_=true;candidate_.clear();}
        bool closed()const noexcept{return closed_;}
    private:
        AuthoringSceneDocument* owner_;uint64_t expectedVersion_,nextId_;
        std::shared_ptr<const State> base_;std::vector<SceneEntity> candidate_;bool closed_=false;
        void checkOpen()const;
        SceneEntity& edit(SceneEntityId);
        template<class F> decltype(auto) stage(F&& f){try{checkOpen();return f();}catch(...){closed_=true;throw;}}
    };
    Transaction beginTransaction(uint64_t expectedVersion){return Transaction(*this,expectedVersion);}
    bool undo(uint64_t expectedVersion=0);bool redo(uint64_t expectedVersion=0);
    PreparedEdit prepareUndo(uint64_t expectedVersion);PreparedEdit prepareRedo(uint64_t expectedVersion);
    bool canUndo()const noexcept{return !undo_.empty();}bool canRedo()const noexcept{return !redo_.empty();}
    size_t historyBytes()const noexcept;
    Json toJson()const;Json inspectionJson(bool includeWorldTransforms=true)const;
    static AuthoringSceneDocument fromJson(const Json&);
    void save(const std::filesystem::path&)const;static AuthoringSceneDocument load(const std::filesystem::path&);
    static Json transformToJson(const SceneTransform&);static SceneTransform transformFromJson(const Json&);static Json entityToJson(const SceneEntity&);
private:
    std::string sceneId_;uint64_t version_=1,nextEntityId_=1;std::shared_ptr<const State> state_;
    std::vector<HistoryEntry> undo_,redo_;
    static void validateTransform(const SceneTransform&);static void validateEntity(const SceneEntity&);
    static std::shared_ptr<const State> buildState(std::vector<SceneEntity>);
    Json serialize(const State&,uint64_t revision,uint64_t nextId)const;
    void validateSave(const State&,uint64_t revision,uint64_t nextId)const;
    PreparedEdit prepareState(std::shared_ptr<const State>,uint64_t nextId,uint64_t expectedVersion);
    PreparedEdit prepareHistory(bool undo,uint64_t expectedVersion);
    static size_t historyBytes(const std::vector<HistoryEntry>&)noexcept;
};
}
