using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    public enum AnimationPlaygroundReadiness { Uninitialized, DependenciesChecked, ServicesBound, VisualReady, PlayableReady, Failed }

    [DefaultExecutionOrder(-10000), DisallowMultipleComponent]
    public sealed partial class TopDown3DAnimationPlaygroundSession : MonoBehaviour
    {
        public const string ScenePath = "Assets/_Project/Scenes/Reference/CampLoopTesting.unity";
        private static readonly Dictionary<int, TopDown3DAnimationPlaygroundSession> Sessions = new();
        [SerializeField] private TopDown3DAnimationPlaygroundBindings bindings;
        [SerializeField] private AnimationPlaygroundCatalog catalog;
        [SerializeField] private GameObject palettePrefab;
        [SerializeField] private AnimationPlaygroundForwardLocomotionProfile experimentalForwardReview;
        [SerializeField] private bool experimentalForwardReviewRequired;
        public AnimationPlaygroundForwardLocomotionProfile ExperimentalForwardReview => experimentalForwardReview;
        public string ForwardReviewStatus => experimentalForwardReview == null
            ? experimentalForwardReviewRequired ? "Experimental forward gait review unavailable: restore its private profile." : null : presentation?.LaboratoryForwardReviewStatus
            ?? "Experimental forward gait review — awaiting player startup.";
        public void ConfigureForwardReview(AnimationPlaygroundForwardLocomotionProfile value)
        {
            if (servicesBound) throw new InvalidOperationException("Author forward review before startup.");
            experimentalForwardReview = value;
            experimentalForwardReviewRequired = value != null;
        }
        public TopDown3DAnimationPlaygroundBindings Bindings => bindings;
        public AnimationPlaygroundCatalog Catalog => catalog;
        public GameObject PalettePrefab => palettePrefab;
        public void ConfigurePresentation(AnimationPlaygroundCatalog value,GameObject prefab){catalog=value;palettePrefab=prefab;}
        public AnimationPlaygroundReadiness Readiness { get; private set; }
        public bool PreviewReady => Readiness >= AnimationPlaygroundReadiness.VisualReady && Readiness <= AnimationPlaygroundReadiness.PlayableReady;
        public string UnavailableReason { get; private set; }
        public bool IsLab => MatchesLabIdentity(gameObject.scene.path, true);
        public string ControlsDirectory => Path.Combine(Application.persistentDataPath, "AnimationPlayground");
        public event Action ReadinessChanged;
        public AnimationPlaygroundResetRegistry ResetRegistry { get; } = new();

        public void Configure(TopDown3DAnimationPlaygroundBindings value)
        {
            if (Readiness > AnimationPlaygroundReadiness.DependenciesChecked && Readiness != AnimationPlaygroundReadiness.Failed)
                throw new InvalidOperationException("Bind playground assets before service/visual initialization.");
            bindings = value;
        }

        private void Awake()
        {
            // Scene.isLoaded remains false through Awake/OnEnable. Register before
            // player preparation, which needs this session to configure forward review.
            if (IsLab) Sessions[gameObject.scene.handle] = this;
            if (CheckDependencies()) PreparePlayer();
        }
        public bool CheckDependencies()
        {
            if (!IsLab) return Fail("Animation playground configuration is only valid in Camp Loop Testing.");
            if (bindings == null) return Fail("Private animation binding set is absent. Restore the playground dependencies first.");
            if (!bindings.ValidateImport(out var reason)) return Fail(reason);
            if (experimentalForwardReviewRequired && experimentalForwardReview == null)
                return Fail("The experimental forward review profile is missing. Restore the sealed forward draft dependencies.");
            SetReadiness(AnimationPlaygroundReadiness.DependenciesChecked, null);
            return true;
        }
        public bool Advance(AnimationPlaygroundReadiness next)
        {
            if (!IsLab || Readiness == AnimationPlaygroundReadiness.Failed || next != Readiness + 1 || next > AnimationPlaygroundReadiness.PlayableReady)
                return false;
            SetReadiness(next, null);
            return true;
        }
        public bool Fail(string reason)
        {
            SetReadiness(AnimationPlaygroundReadiness.Failed, reason);
            return false;
        }
        private void SetReadiness(AnimationPlaygroundReadiness value, string reason)
        {
            Readiness = value; UnavailableReason = reason;
            ReadinessChanged?.Invoke();
        }
        private void OnDisable()
        {
            ResetRegistry.CancelAll();
            SetReadiness(AnimationPlaygroundReadiness.Uninitialized, null);
        }
        private void OnDestroy()
        {
            DetachPlayerServices();
            if (Sessions.TryGetValue(gameObject.scene.handle, out var owner) && owner == this)
                Sessions.Remove(gameObject.scene.handle);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => Sessions.Clear();
        public static bool MatchesLabIdentity(string path, bool hasMarker) => hasMarker && path == ScenePath;

        // This policy depends on authored scene identity/marker, never arbitrary Awake order or binding availability.
        public static TopDown3DAnimationPlaygroundSession Find(Scene scene)
            => FindDuringSceneLoad(scene, scene.isLoaded);

        // Keep the load phase explicit so EditMode tests can cover early startup.
        private static TopDown3DAnimationPlaygroundSession FindDuringSceneLoad(Scene scene, bool sceneLoaded)
        {
            if (!scene.IsValid() || !MatchesLabIdentity(scene.path, true)) return null;
            if (Sessions.TryGetValue(scene.handle, out var cached) && cached != null && cached.gameObject.scene == scene)
                return cached;
            if (!sceneLoaded) return null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var session = root.GetComponentInChildren<TopDown3DAnimationPlaygroundSession>(true);
                if (session != null) { Sessions[scene.handle] = session; return session; }
            }
            return null;
        }
        public static bool IsSessionOnly(Scene scene) => Find(scene) != null;
        public static string ResolveControlsDirectory(Scene scene, string settingsDirectory)
            => Find(scene)?.ControlsDirectory ?? settingsDirectory;
    }
}
