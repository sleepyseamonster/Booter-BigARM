using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    [DisallowMultipleComponent]
    public sealed class TopDown3DResourceCache : MonoBehaviour, ITopDown3DInteractable, ISerializationCallbackReceiver
    {
        public const string ResourceName = "GreaterWastelandResourceCache";
        [SerializeField] private string cacheId = "greater-wasteland:cache:starter:v1";
        [SerializeField] private TopDown3DItemCatalog itemCatalog;
        [SerializeField, Min(1)] private int capacity = 24;
        [System.NonSerialized] private TopDown3DInventoryState state;
        [SerializeField, HideInInspector] private TopDown3DInventorySnapshot reloadSnapshot;
        public TopDown3DInventoryState State
        {
            get
            {
                if (state != null && state.IsInitialized) return state;
                state = new TopDown3DInventoryState(itemCatalog, new TopDown3DInventoryPolicy(TopDown3DInventoryOwner.ResourceCache, capacity));
                if (reloadSnapshot != null && reloadSnapshot.Version > 0 && !state.ApplySnapshot(reloadSnapshot).Succeeded)
                {
                    state.RetainForRecovery();
                    Debug.LogWarning("Retained cache snapshot was incompatible and was preserved for recovery.", this);
                }
                return state;
            }
        }
        public TopDown3DItemCatalog ItemCatalog => itemCatalog;
        public string StableId => cacheId;
        public Object UnityObject => this;
        public string Prompt => "Open Resource Cache";
        public Vector3 InteractionPoint => transform.position;
        public float InteractionRange => 2.8f;
        public float ActionDuration => 0.01f;
        public TopDown3DItemAmount Reward => default;
        public bool IsAvailable => isActiveAndEnabled;
        public bool TryConsume(out int remainingUses) { remainingUses = 1; return false; }
        public bool CanAccess(Transform actor) => isActiveAndEnabled && actor != null
            && actor.gameObject.scene == gameObject.scene
            && Vector3.Distance(actor.position, InteractionPoint) <= InteractionRange
            && TopDown3DCargoAccess.HasLineOfSight(actor, transform);

        internal void Configure(TopDown3DItemCatalog catalog, int slots, string id)
        { itemCatalog = catalog; capacity = slots; cacheId = id; state = null; }
        public void OnBeforeSerialize()
        {
            if (Application.isPlaying && state != null && state.IsInitialized && !state.IsCommitting)
                reloadSnapshot = state.CaptureSnapshot();
        }
        public void OnAfterDeserialize() => state = null;

        public static TopDown3DResourceCache TryInstall(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "GreaterWasteland") return null;
            var existing = TopDown3DGameHudCanvas.FindInScene<TopDown3DResourceCache>(scene);
            if (existing != null) return existing;
            var prefab = Resources.Load<GameObject>(ResourceName);
            if (prefab == null) return null;
            var instance = Instantiate(prefab);
            instance.name = "Starting-area Resource Cache";
            SceneManager.MoveGameObjectToScene(instance, scene);
            return instance.GetComponent<TopDown3DResourceCache>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        { SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallLoaded()
        { for (var i = 0; i < SceneManager.sceneCount; i++) TryInstall(SceneManager.GetSceneAt(i)); }
        private static void Loaded(Scene scene, LoadSceneMode mode) => TryInstall(scene);
    }
}
