using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Explicit session-only trial. Stock is independent of creature health and source visuals.</summary>
    [DefaultExecutionOrder(-11000), DisallowMultipleComponent]
    public sealed class CampFirstToolScenario : MonoBehaviour
    {
        public const string ToolId = "author.tool.jawbone_salvage_digger";
        public const string ResetId = "camp.first-tool.v1";
        [SerializeField] private TopDown3DCampLoopLoadoutProfile loadout;
        [SerializeField] private TopDown3DCraftingRecipe[] recipes = Array.Empty<TopDown3DCraftingRecipe>();
        [SerializeField] private CampFirstToolSource[] sources = Array.Empty<CampFirstToolSource>();
        [SerializeField] private CampFirstToolStock stock = new();
        [SerializeField] private GameObject toolPrefab;
        private GameObject equippedView;
        private TopDown3DCampLoopInventoryTrial trial;
        private TopDown3DCampLoopEconomySession economy;
        private bool registered, locked;
        private static readonly Dictionary<int, CampFirstToolScenario> cache = new();
        public TopDown3DCampLoopLoadoutProfile Loadout => loadout;
        public IReadOnlyList<TopDown3DCraftingRecipe> Recipes => recipes;
        public IReadOnlyList<CampFirstToolSource> Sources => sources;
        public CampFirstToolStock Stock => stock;
        public bool Ready => registered && isActiveAndEnabled && economy != null && economy.Ready && stock.Valid;
        public string Failure { get; private set; }
        public static CampFirstToolScenario Find(Scene scene)
        {
            if (!scene.IsValid() || scene.path != TopDown3DAnimationPlaygroundSession.ScenePath) return null;
            if (cache.TryGetValue(scene.handle, out var found) && found != null && found.gameObject.scene == scene) return found;
            if (!scene.isLoaded) return null;
            var matches = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CampFirstToolScenario>(true)).ToArray();
            if (matches.Length > 1) throw new InvalidOperationException("Only one first-tool scenario may own a scene.");
            if (matches.Length == 1) { cache[scene.handle] = matches[0]; return matches[0]; }
            return null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCache() => cache.Clear();
        public void Configure(TopDown3DCampLoopLoadoutProfile profile, TopDown3DCraftingRecipe[] roster, CampFirstToolSource[] points)
        { loadout = profile; recipes = roster; sources = points; }
        public void ConfigureTool(GameObject prefab) => toolPrefab = prefab;
        private void Awake() { cache[gameObject.scene.handle] = this; }
        private void Update()
        {
            if(CampExpeditionFeature.Enabled(gameObject.scene))return;
            if (registered) return;
            economy = gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TopDown3DCampLoopEconomySession>(true)).SingleOrDefault();
            if (economy == null || !economy.Ready) return;
            trial = economy.Kit.GetComponent<TopDown3DCampLoopInventoryTrial>();
            if (trial == null || !trial.Ready) return;
            if (loadout == null || recipes == null || recipes.Length != 8 || sources == null || sources.Length != CampFirstToolStock.Count
                || sources.Any(s => s == null || s.gameObject.scene != gameObject.scene) || !stock.Valid
                || sources.Select(s => s.StableId).Distinct().Count() != sources.Length
                || recipes.Any(r => r == null || !r.TryValidate(economy.Kit.ItemCatalog, out _)))
            { Failure = "First-tool configuration requires recovery; stock and inventories preserved."; return; }
            trial.RegisterResetParticipant(ResetId, () => !Ready ? "First-tool sources are unavailable." : null, PrepareReset,
                () => stock.Revision + ":" + stock.Epoch, "Living critter and rock stocks also return; no tool or ingredients are granted.");
            registered = true;
            if (Application.isPlaying && GetComponent<CampFirstToolGuideHud>() == null) gameObject.AddComponent<CampFirstToolGuideHud>();
        }
        public bool Available(int index) => Ready && !locked && stock.Available(index);
        public bool TryConsume(int index) => Available(index) && stock.Consume(index);
        private TopDown3DCampResetPreparation PrepareReset()
        {
            if (!Ready || locked || stock.Revision == int.MaxValue || stock.Epoch == uint.MaxValue) return null;
            var expected = stock.Revision;
            var result = new TopDown3DCampResetPreparation();
            result.World.Add(new TopDown3DPreparedWorldMutation(
                () => { if (locked || stock.Revision != expected) return false; locked = true; return true; },
                () => stock.Reset(), () => { foreach (var source in sources) source.Refresh(); }, () => locked = false));
            return result;
        }
        private void OnDisable() { if (registered && trial != null) trial.UnregisterResetParticipant(ResetId); registered = false; }
        private void LateUpdate()
        {
            if (!Ready || toolPrefab == null) return;
            var fitted = economy.Kit.State.Slots[(int)TopDown3DEquipmentSlot.DiggingTool - 1].ItemId == ToolId;
            if (fitted && equippedView == null)
            {
                equippedView = Instantiate(toolPrefab, economy.Kit.transform);
                equippedView.name = "Fitted Bone Salvage Multi-tool";
                equippedView.transform.SetLocalPositionAndRotation(new Vector3(.38f, .7f, -.18f), Quaternion.Euler(0, 0, -25));
            }
            if (equippedView != null) equippedView.SetActive(fitted);
        }
        private void OnDestroy() { if (cache.TryGetValue(gameObject.scene.handle, out var value) && value == this) cache.Remove(gameObject.scene.handle); }
    }

    [Serializable]
    public sealed class CampFirstToolStock
    {
        public const int Count = 6;
        [SerializeField] private int version = 1, consumed, revision;
        [SerializeField] private uint epoch;
        public int Revision => revision;
        public uint Epoch => epoch;
        public bool Valid => version == 1 && consumed >= 0 && consumed < (1 << Count) && revision >= 0;
        public bool Available(int index) => Valid && index >= 0 && index < Count && (consumed & (1 << index)) == 0;
        public bool Consume(int index)
        { if (!Available(index) || revision == int.MaxValue) return false; consumed |= 1 << index; revision++; return true; }
        public void Reset() { consumed = 0; revision++; epoch = unchecked(epoch + 1); }
    }
}
