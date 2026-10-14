using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    // Outlives a cancelled/disabled action owner while a released body separates.
    // Owns only the temporary ignore pairs; it never moves an actor or prop.
    [DisallowMultipleComponent]
    public sealed class AnimationPlaygroundPropCollisionGuard : MonoBehaviour
    {
        private readonly struct Pair
        {
            public readonly Collider Prop, Actor;
            public readonly bool Original;
            public Pair(Collider prop, Collider actor, bool original)
            { Prop = prop; Actor = actor; Original = original; }
        }
        private readonly List<Pair> pairs = new();
        public bool HasPairs => pairs.Count != 0;
        internal void Add(Collider prop, Collider actor, bool original)
        {
            foreach (var pair in pairs) if (pair.Prop == prop && pair.Actor == actor) return;
            pairs.Add(new Pair(prop, actor, original));
            Physics.IgnoreCollision(prop, actor, true);
        }
        internal void RetireSeparatedPairs()
        {
            for (var i = pairs.Count - 1; i >= 0; i--)
            {
                var pair = pairs[i];
                if (pair.Prop != null && pair.Actor != null && pair.Prop.enabled && pair.Actor.enabled
                    && pair.Prop.gameObject.activeInHierarchy && pair.Actor.gameObject.activeInHierarchy
                    && pair.Prop.bounds.Intersects(pair.Actor.bounds))
                { Physics.IgnoreCollision(pair.Prop, pair.Actor, true); continue; }
                Restore(pair); pairs.RemoveAt(i);
            }
        }
        internal void RestoreAll()
        {
            foreach (var pair in pairs) Restore(pair);
            pairs.Clear();
        }
        private static void Restore(Pair pair)
        {
            if (pair.Prop != null && pair.Actor != null) Physics.IgnoreCollision(pair.Prop, pair.Actor, pair.Original);
        }
        private void FixedUpdate() => RetireSeparatedPairs();
        private void OnDestroy() => RestoreAll();
    }
}
