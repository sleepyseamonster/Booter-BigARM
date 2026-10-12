using System;
using System.Linq;
using BooterBigArm.TopDown3D;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BooterBigArm.Editor
{
    public static class TopDown3DWaypointCampValidator
    {
        // Read-only against the already loaded production scene. Does not install UI, save, change scene, or enter Play Mode.
        public static string ValidateLoadedProduction()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != TopDown3DWaypointCampSettings.ProductionScene) throw new InvalidOperationException("Load Greater Wasteland before validation");
            var definition = TopDown3DWaypointCampSettings.Load();
            if (definition == null || !definition.TryValidate(out var reason)) throw new InvalidOperationException("Waypoint definition missing or invalid");
            var camp = TopDown3DCampSettings.Load();
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).ToArray();
            var player = components.OfType<TopDown3DPlayerMotor>().Single();
            var companion = components.OfType<TopDown3DBigArmFollower>().Single();
            if (components.OfType<TopDown3DProceduralWorld>().Any() || components.OfType<TopDown3DGameStateSaveService>().Any())
                throw new InvalidOperationException("Generated-world services are forbidden in production");
            if (!TopDown3DCampPlacement.Validate(definition.position, Quaternion.identity, camp, player.transform, null, out var supported, out reason))
                throw new InvalidOperationException("Starting camp support failed: " + reason);
            var capsule = player.GetComponent<CapsuleCollider>(); var box = companion.GetComponent<BoxCollider>();
            var clearance = Mathf.Max(capsule.radius, Mathf.Max(box.size.x, box.size.z) * .5f);
            var samples = 0;
            for (var segment = 1; segment < definition.returnRoute.Length; segment++)
            {
                var a = definition.returnRoute[segment - 1]; var b = definition.returnRoute[segment];
                var count = Mathf.CeilToInt(Vector3.Distance(a, b) / .5f);
                if (count > 200) throw new InvalidOperationException("Return segment exceeds bounded validation budget");
                for (var i = 0; i <= count; i++)
                {
                    var point = Vector3.Lerp(a, b, (float)i / Mathf.Max(1, count));
                    if (!TopDown3DCampPlacement.TryGround(point, out var ground, out var normal) || Vector3.Angle(normal, Vector3.up) > 30)
                        throw new InvalidOperationException("Shared route lacks ground support");
                    foreach (var side in new[] { -1f, 1f })
                        if (!TopDown3DCampPlacement.TryGround(point + Vector3.right * clearance * side, out var edge, out var slope)
                            || Mathf.Abs(edge.y - ground.y) > .5f || Vector3.Angle(slope, Vector3.up) > 30)
                            throw new InvalidOperationException("Shared route is too narrow or steep");
                    var overlaps = Physics.OverlapCapsule(ground + Vector3.up * (clearance + .1f), ground + Vector3.up * Mathf.Max(clearance + .1f, capsule.height - clearance), clearance, ~0, QueryTriggerInteraction.Ignore);
                    if (overlaps.Any(c => !(c is TerrainCollider) && c.GetComponentInParent<TopDown3DGroundSurface>() == null
                        && !c.transform.IsChildOf(player.transform) && !c.transform.IsChildOf(companion.transform)))
                        throw new InvalidOperationException("Shared return or arrival space is occupied");
                    samples++;
                }
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopDown3DWaypointCampAuthoring.MenuPath);
            if (prefab.GetComponentsInChildren<Button>(true).Count(button => button.name == "Camps") != 1)
                throw new InvalidOperationException("Camps navigation missing or duplicated");
            if (definition.siteId != "camp.starting" || definition.worldId != "greater-wasteland.authored.v1") throw new InvalidOperationException("Starting site identity changed");
            return $"WAYPOINT PRODUCTION VALIDATION PASSED: authored starting camp, {samples} shared route samples, clear arrival, one Camps entry, fixed terrain and no generated save service. Session-only prototype; no scene modified.";
        }
    }
}
