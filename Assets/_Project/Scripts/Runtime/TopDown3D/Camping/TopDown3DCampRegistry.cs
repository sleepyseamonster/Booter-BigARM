using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.TopDown3D
{
    public sealed class TopDown3DCampStoreBinding
    {
        public string Id { get; }
        public string Label { get; }
        public TopDown3DInventoryState State { get; }
        public Component Owner { get; }
        public bool AcceptsTransfers { get; }
        private readonly Func<bool> access;
        private readonly Func<TopDown3DInventoryState> resolve;
        public bool Current => Owner != null && (resolve == null || ReferenceEquals(State, resolve()));
        public bool Accessible => Current && Owner.gameObject.activeInHierarchy
            && (Owner is not Behaviour behaviour || behaviour.isActiveAndEnabled) && access();
        internal TopDown3DCampStoreBinding(string id, string label, Component owner, TopDown3DInventoryState state, bool accepts, Func<bool> canAccess,
            Func<TopDown3DInventoryState> currentState = null)
        { Id = id; Label = label; Owner = owner; State = state; AcceptsTransfers = accepts; access = canAccess; resolve = currentState; }
    }

    public interface ITopDown3DCampStation
    {
        string Id { get; }
        string Label { get; }
        ITopDown3DInteractable Target { get; }
        bool CanAccess(Transform actor);
    }

    internal sealed class TopDown3DCampStationAdapter : ITopDown3DCampStation
    {
        public string Id { get; }
        public string Label { get; }
        public ITopDown3DInteractable Target { get; }
        private readonly Func<Transform, bool> access;
        internal TopDown3DCampStationAdapter(string id, string label, ITopDown3DInteractable target, Func<Transform, bool> canAccess)
        { Id = id; Label = label; Target = target; access = canAccess; }
        public bool CanAccess(Transform actor) => Target.UnityObject != null && Target.IsAvailable && access(actor);
    }

    public sealed class TopDown3DCampRegistry
    {
        private readonly List<TopDown3DCampStoreBinding> stores = new();
        private readonly List<ITopDown3DCampStation> stations = new();
        private readonly HashSet<string> ids = new(StringComparer.Ordinal);
        public IReadOnlyList<TopDown3DCampStoreBinding> Stores => stores;
        public IReadOnlyList<ITopDown3DCampStation> Stations => stations;
        public Scene Scene { get; }
        public string WorldId { get; }
        public string SiteId { get; }
        public Guid SessionToken { get; } = Guid.NewGuid();
        public uint Generation { get; private set; }
        public bool Active { get; private set; } = true;
        public bool Current
        {
            get
            {
                if (!Active) return false;
                foreach (var station in stations) if (station.Target.UnityObject == null) return false;
                foreach (var store in stores) if (!store.Current) return false;
                return true;
            }
        }
        internal TopDown3DCampRegistry(Scene scene, string world, string site)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(world) || string.IsNullOrWhiteSpace(site)) throw new ArgumentException("Camp identity is incomplete.");
            Scene = scene; WorldId = world; SiteId = site;
        }
        internal void Add(TopDown3DCampStoreBinding store)
        {
            if (!Active || stores.Count >= 16 || store == null || store.Owner == null || store.Owner.gameObject.scene != Scene || store.State == null
                || !store.State.IsInitialized || string.IsNullOrWhiteSpace(store.Id) || ids.Contains(store.Id)) throw new ArgumentException("Invalid or duplicate camp store.");
            foreach (var other in stores) if (ReferenceEquals(store.State, other.State)) throw new ArgumentException("One inventory cannot have multiple camp owners.");
            if (stores.Count > 0 && stores[0].State.ItemCatalog != store.State.ItemCatalog) throw new ArgumentException("Camp stores require one catalog authority.");
            ids.Add(store.Id); stores.Add(store); Generation++;
        }
        internal void Add(ITopDown3DCampStation station)
        {
            if (!Active || stations.Count >= 16 || station == null || station.Target.UnityObject is not Component owner || owner.gameObject.scene != Scene
                || string.IsNullOrWhiteSpace(station.Id) || !ids.Add(station.Id)) throw new ArgumentException("Invalid or duplicate camp station.");
            stations.Add(station); Generation++;
        }
        public TopDown3DCampStoreBinding FindStore(string id)
        { foreach (var store in stores) if (store.Id == id) return store; return null; }
        public ITopDown3DCampStation FindStation(ITopDown3DInteractable target)
        { foreach (var station in stations) if (ReferenceEquals(station.Target, target)) return station; return null; }
        internal void Invalidate() { Active = false; if (Generation < uint.MaxValue) Generation++; }
    }
}
