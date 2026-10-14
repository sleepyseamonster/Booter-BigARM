using System;
using System.Collections.Generic;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BooterBigArm.Editor
{
    public static class AnimationPlaygroundSceneValidator
    {
        [Serializable] public sealed class StationReport { public string id, prefab; public int targets, props, bodies, colliders; public bool returnClear, returnSupported; }
        [Serializable] public sealed class Report
        {
            public bool valid;
            public int terrains, stations, rigidbodies, renderers, skinnedActors, legacyResources, caches;
            public StationReport[] stationReports;
            public string[] errors;
        }
        // Inspection only: never opens/saves/installs a scene or changes a runtime participant.
        public static Report Validate(Scene scene)
        {
            var errors = new List<string>(); var roots = scene.GetRootGameObjects(); var session = TopDown3DAnimationPlaygroundSession.Find(scene);
            var result = new Report { terrains = roots.SelectMany(root => root.GetComponentsInChildren<Terrain>(true)).Count() };
            if (scene.path != TopDown3DAnimationPlaygroundSession.ScenePath || session == null) errors.Add("The exact authored lab/session is required.");
            if (session == null || session.Player == null) { result.errors = errors.ToArray(); return result; }
            if (result.terrains != 9) errors.Add("Preserve the nine native terrain pairs.");
            if (session.Bindings == null) errors.Add("Private player bindings are missing.");
            else if (!session.Bindings.ValidatePlayer(out var reason)) errors.Add(reason);
            result.legacyResources = roots.SelectMany(root => root.GetComponentsInChildren<TopDown3DAuthoredIronstoneNode>(true)).Count();
            result.caches = roots.SelectMany(root => root.GetComponentsInChildren<TopDown3DResourceCache>(true)).Count();
            if (result.legacyResources != 12 || result.caches != 1) errors.Add("Preserve the twelve original Ironstone nodes and single cache.");
            var stations = session.Stations; result.stations = stations.Length;
            if (stations.Length != 12) errors.Add("Twelve station shells are required.");
            var ids = new HashSet<string>(StringComparer.Ordinal); var reports = new List<StationReport>();
            var terrainHashes = roots.SelectMany(root => root.GetComponentsInChildren<Terrain>(true)).ToDictionary(value => value, value => AnimationPlaygroundLayoutAuthoring.Hash(AssetDatabase.GetAssetPath(value.terrainData)));
            var capsule = session.Player.GetComponent<CapsuleCollider>();
            foreach (var station in stations)
            {
                if (station == null || !station.transform.IsChildOf(session.transform) || !ids.Add(station.StationId)) { errors.Add("Station identities are missing, duplicated or outside ownership."); continue; }
                var colliders = station.GetComponentsInChildren<Collider>(true); var bodies = station.GetComponentsInChildren<Rigidbody>(true);
                if (station.transform.lossyScale != Vector3.one || bodies.Any(value => !TopDown3DAnimationPlaygroundSession.HasUnitPhysicsHierarchy(value.transform)))
                    errors.Add(station.StationId + " scales a physics root.");
                foreach (var prop in station.Props) if (prop == null || !ids.Add("Prop." + prop.StableId)) errors.Add(station.StationId + " has a missing/duplicate prop.");
                foreach (var target in station.Targets)
                {
                    if (target == null || !ids.Add("Target." + target.StableId)) { errors.Add(station.StationId + " has a missing/duplicate target."); continue; }
                    if (target.ActionKind != AnimationPlaygroundActionKind.Preview && target.ActionKind != AnimationPlaygroundActionKind.Reset && target.IsAvailable)
                        errors.Add(station.StationId + " offers an unqualified physical target.");
                }
                var point = station.SafeReturn.position; var bottom = point + Vector3.down * (capsule.height * .5f - capsule.radius - .005f);
                var top = point + Vector3.up * (capsule.height * .5f - capsule.radius);
                var overlaps = Physics.OverlapCapsule(bottom, top, capsule.radius * .98f, ~0, QueryTriggerInteraction.Ignore);
                var clear = overlaps.All(value => value.transform.IsChildOf(session.Player.transform));
                var supported = Physics.Raycast(point, Vector3.down, out var hit, capsule.height * .5f + .1f, ~0, QueryTriggerInteraction.Ignore)
                    && !hit.transform.IsChildOf(session.Player.transform) && Vector3.Angle(hit.normal, Vector3.up) < 14;
                if (!clear || !supported) errors.Add(station.StationId + " safe return is blocked or unsupported.");
                var record = new StationReport { id = station.StationId, prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(station.gameObject),
                    targets = station.Targets.Length, props = station.Props.Length, bodies = bodies.Length, colliders = colliders.Length, returnClear = clear, returnSupported = supported };
                reports.Add(record); result.rigidbodies += bodies.Length; result.renderers += station.GetComponentsInChildren<Renderer>(true).Length;
                result.skinnedActors += station.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            }
            if (result.skinnedActors != 0 || result.rigidbodies > 16) errors.Add("Keep station skins absent and bounded physical props.");
            for (var i = 0; i < stations.Length; i++) for (var j = i + 1; j < stations.Length; j++)
            {
                if (stations[i] == null || stations[j] == null) continue;
                var delta = stations[i].transform.position - stations[j].transform.position;
                if (Mathf.Abs(delta.x) < (stations[i].Footprint.x + stations[j].Footprint.x) * .5f + 3
                    && Mathf.Abs(delta.z) < (stations[i].Footprint.y + stations[j].Footprint.y) * .5f + 3)
                    errors.Add(stations[i].StationId + " and " + stations[j].StationId + " leave no return corridor.");
            }
            foreach (var pair in terrainHashes)
                if (pair.Value != AnimationPlaygroundLayoutAuthoring.Hash(AssetDatabase.GetAssetPath(pair.Key.terrainData))) errors.Add("A terrain changed during validation.");
            result.stationReports = reports.ToArray(); result.errors = errors.ToArray(); result.valid = errors.Count == 0; return result;
        }
    }
}
