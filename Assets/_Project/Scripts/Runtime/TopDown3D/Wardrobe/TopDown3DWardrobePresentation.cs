using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>
    /// Explicitly configured presentation; no scene scans, Animator, inventory ownership, or pose writes.
    /// TryReplace is an all-or-nothing visual transaction. Gameplay equipment transactions remain separately owned.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class TopDown3DWardrobePresentation : MonoBehaviour
    {
        private SkinnedMeshRenderer body;
        private Transform modelRoot;
        private TopDown3DWardrobeRig rig;
        private Material[] originalMaterials;
        private GameObject assembly;
        private Mesh coveredBody;
        private string[] equipped = Array.Empty<string>();
        public string[] EquippedVisualIds => (string[])equipped.Clone();
        public int Revision { get; private set; }

        public void Configure(Transform root, SkinnedMeshRenderer target, TopDown3DWardrobeRig contract)
        {
            if (body != null) throw new InvalidOperationException("Wardrobe is already configured.");
            if (!root || !target || !contract || contract.schemaVersion != 1 || string.IsNullOrWhiteSpace(contract.bodyId)
                || target.sharedMesh != contract.bodyMesh || !contract.bodyMesh || !contract.bodyMesh.isReadable
                || contract.bonePaths == null || contract.triangleRegions == null)
                throw new ArgumentException("A matching readable body and schema-1 rig are required.");
            var bones = target.bones;
            if (bones.Length != contract.bonePaths.Length || bones.Length != contract.bodyMesh.bindposes.Length)
                throw new ArgumentException("Body bone contract differs.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < bones.Length; i++)
                if (string.IsNullOrEmpty(contract.bonePaths[i]) || !seen.Add(contract.bonePaths[i])
                    || root.Find(contract.bonePaths[i]) != bones[i]) throw new ArgumentException("Body hierarchy differs.");
            var count = 0;
            for (var s = 0; s < contract.bodyMesh.subMeshCount; s++) count += contract.bodyMesh.GetTriangles(s).Length / 3;
            if (contract.triangleRegions.Length != count) throw new ArgumentException("Coverage triangle contract differs.");
            foreach (var region in contract.triangleRegions)
                if ((region & ~63) != 0) throw new ArgumentException("Unknown body coverage region.");
            body = target; modelRoot = root; rig = contract; originalMaterials = target.sharedMaterials;
        }

        public bool CanReplace(TopDown3DWardrobeDefinition[] definitions, out string reason)
        {
            reason = null;
            try { Validate(definitions); return true; }
            catch (ArgumentException e) { reason = e.Message; return false; }
            catch (InvalidOperationException e) { reason = e.Message; return false; }
        }

        public bool TryReplace(TopDown3DWardrobeDefinition[] definitions, out string reason)
        {
            if (!CanReplace(definitions, out reason)) return false;
            GameObject prepared = null; Mesh preparedBody = null;
            var materials = originalMaterials;
            var ids = new string[definitions.Length];
            try
            {
                prepared = new GameObject("Wardrobe presentation"); prepared.SetActive(false);
                var coverage = WardrobeRegion.None;
                for (var d = 0; d < definitions.Length; d++)
                {
                    var definition = definitions[d]; ids[d] = definition.visualId; coverage |= definition.hides;
                    if (definition.bodyMaterials.Length != 0) materials = definition.bodyMaterials;
                    foreach (var part in definition.parts)
                    {
                        var node = new GameObject(definition.visualId); node.transform.SetParent(prepared.transform, false);
                        var skin = node.AddComponent<SkinnedMeshRenderer>(); skin.sharedMesh = part.mesh;
                        var bones = new Transform[part.bonePaths.Length];
                        for (var i = 0; i < bones.Length; i++) bones[i] = modelRoot.Find(part.bonePaths[i]);
                        skin.bones = bones; skin.rootBone = modelRoot.Find(part.rootBonePath);
                        skin.sharedMaterials = part.materials; skin.localBounds = part.bounds;
                        skin.quality = body.quality; skin.shadowCastingMode = body.shadowCastingMode;
                        skin.receiveShadows = body.receiveShadows;
                    }
                }
                if (coverage != WardrobeRegion.None) preparedBody = CreateCoveredBody(coverage);
            }
            catch (Exception e)
            {
                Release(prepared); Release(preparedBody); reason = e.Message; return false;
            }
            // Publish only after all allocations, mesh work, and renderer wiring have succeeded.
            var previous = assembly; var previousBody = coveredBody;
            prepared.transform.SetParent(body.transform, false);
            if (previous) previous.SetActive(false);
            body.sharedMesh = preparedBody ? preparedBody : rig.bodyMesh;
            body.sharedMaterials = materials;
            coveredBody = preparedBody; assembly = prepared; equipped = ids; Revision++;
            prepared.SetActive(true); Release(previous); Release(previousBody);
            return true;
        }

        private void Validate(TopDown3DWardrobeDefinition[] definitions)
        {
            if (!body || !modelRoot || !rig) throw new InvalidOperationException("Configure the wardrobe first.");
            if (body.sharedMesh != (coveredBody ? coveredBody : rig.bodyMesh))
                throw new InvalidOperationException("Another owner changed the body mesh.");
            if (definitions == null) throw new ArgumentException("Use an empty array to unequip.");
            var ids = new HashSet<string>(StringComparer.Ordinal); var occupied = new int[3]; var materialOwner = false;
            var binds = rig.bodyMesh.bindposes;
            foreach (var d in definitions)
            {
                if (!d || d.schemaVersion != 1 || d.rig != rig || string.IsNullOrWhiteSpace(d.visualId) || !ids.Add(d.visualId))
                    throw new ArgumentException("Missing, duplicate, or incompatible wearable.");
                var layer = (int)d.layer;
                if (layer < 0 || layer >= occupied.Length || (((int)d.occupies | (int)d.hides) & ~63) != 0
                    || (occupied[layer] & (int)d.occupies) != 0) throw new ArgumentException("Wearable region conflict.");
                occupied[layer] |= (int)d.occupies;
                if (d.bodyMaterials == null || d.parts == null) throw new ArgumentException("Missing wearable arrays.");
                if (d.bodyMaterials.Length != 0)
                {
                    if (materialOwner) throw new ArgumentException("Multiple body material owners.");
                    materialOwner = true; CheckMaterials(d.bodyMaterials, rig.bodyMesh.subMeshCount);
                }
                foreach (var part in d.parts)
                {
                    if (part == null || !part.mesh || !part.mesh.isReadable || part.bonePaths == null
                        || part.bonePaths.Length == 0 || part.mesh.bindposes.Length != part.bonePaths.Length
                        || string.IsNullOrEmpty(part.rootBonePath) || !modelRoot.Find(part.rootBonePath)
                        || Array.IndexOf(rig.bonePaths, part.rootBonePath) < 0)
                        throw new ArgumentException("Invalid garment mesh or root bone.");
                    CheckMaterials(part.materials, part.mesh.subMeshCount);
                    var size = part.bounds.size;
                    if (!Finite(part.bounds.center) || !Finite(size) || size.x <= 0 || size.y <= 0 || size.z <= 0)
                        throw new ArgumentException("Invalid animated bounds.");
                    var paths = new HashSet<string>(StringComparer.Ordinal);
                    var partBinds = part.mesh.bindposes;
                    for (var i = 0; i < part.bonePaths.Length; i++)
                    {
                        var path = part.bonePaths[i]; var index = Array.IndexOf(rig.bonePaths, path);
                        if (index < 0 || !paths.Add(path) || !modelRoot.Find(path)) throw new ArgumentException("Missing garment bone.");
                        for (var k = 0; k < 16; k++)
                            if (!float.IsFinite(partBinds[i][k]) || Mathf.Abs(partBinds[i][k] - binds[index][k]) > .0001f)
                                throw new ArgumentException("Incompatible garment bind pose.");
                    }
                    var weights = part.mesh.GetAllBoneWeights(); var counts = part.mesh.GetBonesPerVertex();
                    // These are mesh-owned Allocator.None views.
                    if (counts.Length != part.mesh.vertexCount) throw new ArgumentException("Missing garment weights.");
                    var offset = 0;
                    for (var v = 0; v < counts.Length; v++)
                    {
                        var sum = 0f;
                        if (counts[v] == 0) throw new ArgumentException("Unweighted garment vertex.");
                        for (var j = 0; j < counts[v]; j++)
                        {
                            var w = weights[offset++];
                            if (w.boneIndex < 0 || w.boneIndex >= part.bonePaths.Length || !float.IsFinite(w.weight) || w.weight < 0)
                                throw new ArgumentException("Invalid garment influence.");
                            sum += w.weight;
                        }
                        if (Mathf.Abs(sum - 1) > .001f) throw new ArgumentException("Garment weights must sum to one.");
                    }
                }
            }
        }

        private Mesh CreateCoveredBody(WardrobeRegion coverage)
        {
            var mesh = Instantiate(rig.bodyMesh); mesh.name = "Wardrobe covered body";
            try
            {
                var offset = 0;
                for (var s = 0; s < mesh.subMeshCount; s++)
                {
                    var original = rig.bodyMesh.GetTriangles(s); var visible = new List<int>(original.Length);
                    for (var t = 0; t < original.Length; t += 3)
                    {
                        if ((rig.triangleRegions[offset++] & (int)coverage) != 0) continue;
                        visible.Add(original[t]); visible.Add(original[t + 1]); visible.Add(original[t + 2]);
                    }
                    mesh.SetTriangles(visible, s, false);
                }
                return mesh;
            }
            catch { Release(mesh); throw; }
        }
        private static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        private static void CheckMaterials(Material[] materials, int count)
        {
            if (materials == null || materials.Length != count) throw new ArgumentException("Material/submesh mismatch.");
            foreach (var m in materials) if (!m) throw new ArgumentException("Missing wearable material.");
        }
        private static void Release(UnityEngine.Object value)
        { if (value) { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); } }
        private void OnDestroy()
        {
            if (body && rig && body.sharedMesh == (coveredBody ? coveredBody : rig.bodyMesh))
            { body.sharedMesh = rig.bodyMesh; body.sharedMaterials = originalMaterials; }
            Release(assembly); Release(coveredBody);
        }
    }
}
