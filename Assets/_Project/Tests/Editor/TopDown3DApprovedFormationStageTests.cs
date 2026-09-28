using System.Linq;
using System.Collections.Generic;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DApprovedFormationStageTests
    {
        [TestCase("MixedPileScatter")]
        [TestCase("HandbuiltSpire")]
        public void AuthoredWorldFormationKeepsEveryWorkbenchMemberInOneLodGroup(string assetName)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset");
            var asset = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(
                "Assets/_Project/Art/Environment/Rocks/Generated/" + assetName + ".asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Materials/TopDown3D/Greybox_Terrain.mat");
            var entries = asset.ApprovedStageEntries;
            var members = new TopDown3DRockFormationMember[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var position = entry.LocalPose.GetColumn(3);
                var bounds = new Bounds(position, Vector3.one);
                members[i] = new TopDown3DRockFormationMember(entry.InstanceId,
                    entry.Family.StableId, TopDown3DRockSizeTier.Medium, entry.Family.Shape, 0,
                    position, entry.LocalPose.rotation, entry.LocalPose.lossyScale,
                    i, -1, 1f, bounds, entry.Family, asset.Members[entry.SourceIndex].Material, 0f);
            }
            var formation = new TopDown3DRockFormationPlan(
                new TopDown3DRockRootKey(TopDown3DRockSizeTier.Medium, 0, 0, 1),
                "complete-workbench-stage", 1, TopDown3DNaturalObjectLayer.Obstacle,
                TopDown3DRockSurface.Regular, members, Vector2.zero, 5f, 3f, asset);
            var plan = new TopDown3DNaturalObjectChunkPlan(
                new List<TopDown3DNaturalObjectPlacement>(),
                new List<TopDown3DRockFormationPlan> { formation },
                new List<TopDown3DResourceNodePlacement>());
            var chunkObject = new GameObject("Authored formation LOD proof");
            try
            {
                var chunk = chunkObject.AddComponent<TopDown3DGeneratedChunk>();
                TopDown3DNaturalObjectDecorator.Decorate(chunk, settings, material, plan);
                var root = chunk.DecorationRoot.GetChild(0);
                var groups = root.GetComponentsInChildren<LODGroup>(true);
                Assert.That(entries.Count, Is.GreaterThanOrEqualTo(15));
                Assert.That(groups.Length, Is.EqualTo(1));
                Assert.That(groups[0].transform, Is.EqualTo(root));
                var lods = groups[0].GetLODs();
                Assert.That(lods.Length, Is.EqualTo(3));
                Assert.That(lods[0].screenRelativeTransitionHeight, Is.LessThanOrEqualTo(0.08f));
                Assert.That(lods[2].screenRelativeTransitionHeight, Is.LessThanOrEqualTo(0.002f));
                for (var lod = 0; lod < lods.Length; lod++)
                    Assert.That(lods[lod].renderers.Length, Is.GreaterThanOrEqualTo(entries.Count));
            }
            finally { UnityEngine.Object.DestroyImmediate(chunkObject); }
        }

        [Test]
        public void EachFormationFamilyCanSelectEveryBakedGeneration()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset");
            var seen = new Dictionary<TopDown3DAuthoredFormationAsset, HashSet<int>>();
            for (var i = 0; i < 256; i++)
            {
                var id = "generation-selection-proof:" + i;
                var template = settings.SelectAuthoredFormation(id);
                if (!seen.TryGetValue(template, out var choices))
                    seen.Add(template, choices = new HashSet<int>());
                var choice = TopDown3DAuthoredFormationPlacement.SelectGenerationIndex(template, id);
                Assert.That(choice, Is.EqualTo(
                    TopDown3DAuthoredFormationPlacement.SelectGenerationIndex(template, id)));
                choices.Add(choice);
            }
            Assert.That(seen.Count, Is.EqualTo(2));
            foreach (var choices in seen.Values)
                Assert.That(choices, Is.EquivalentTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void FormationGroundShaderMaskContinuesAcrossChunkBoundary()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset");
            var template = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(
                "Assets/_Project/Art/Environment/Rocks/Generated/MixedPileScatter.asset");
            var groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Materials/TopDown3D/Greybox_Terrain.mat");
            var staged = template.ApprovedStageEntries[0];
            var bounds = new Bounds(new Vector3(18f, 0.2f, 9f), new Vector3(2f, 1f, 2f));
            var member = new TopDown3DRockFormationMember("seam-member", staged.Family.StableId,
                TopDown3DRockSizeTier.Medium, staged.Family.Shape, 0,
                bounds.center, Quaternion.identity, Vector3.one, 0, -1, 1f, bounds,
                staged.Family, template.Members[staged.SourceIndex].Material, 0f);
            var formation = new TopDown3DRockFormationPlan(
                new TopDown3DRockRootKey(TopDown3DRockSizeTier.Medium, 0, 0, 1),
                "seam", 1, TopDown3DNaturalObjectLayer.Obstacle, TopDown3DRockSurface.Regular,
                new[] { member }, new Vector2(18f, 9f), 2f, 1f, template);
            var influence = new[] { new TopDown3DFormationTerrainShader.Influence(formation, Vector2.zero) };
            var leftObject = new GameObject("Left seam terrain");
            var rightObject = new GameObject("Right seam terrain");
            var leftMesh = new Mesh();
            var rightMesh = new Mesh();
            try
            {
                leftMesh.vertices = new[] { new Vector3(0f, 0f, 9f), new Vector3(18f, 0f, 9f) };
                rightMesh.vertices = new[] { new Vector3(0f, 0f, 9f), new Vector3(18f, 0f, 9f) };
                leftMesh.uv2 = new[] { new Vector2(0f, 0.6f), new Vector2(0f, 0.6f) };
                rightMesh.uv2 = new[] { new Vector2(0f, 0.6f), new Vector2(0f, 0.6f) };
                rightObject.transform.position = new Vector3(18f, 0f, 0f);
                var left = leftObject.AddComponent<TopDown3DGeneratedChunk>();
                var right = rightObject.AddComponent<TopDown3DGeneratedChunk>();
                left.Initialize(new Vector2Int(0, 0), leftMesh);
                right.Initialize(new Vector2Int(1, 0), rightMesh);
                leftObject.AddComponent<MeshFilter>().sharedMesh = leftMesh;
                rightObject.AddComponent<MeshFilter>().sharedMesh = rightMesh;
                var leftRenderer = leftObject.AddComponent<MeshRenderer>();
                var rightRenderer = rightObject.AddComponent<MeshRenderer>();
                leftRenderer.sharedMaterial = groundMaterial;
                rightRenderer.sharedMaterial = groundMaterial;
                TopDown3DFormationTerrainShader.Apply(left, settings, influence);
                TopDown3DFormationTerrainShader.Apply(right, settings, influence);
                var leftProperties = new MaterialPropertyBlock();
                var rightProperties = new MaterialPropertyBlock();
                leftRenderer.GetPropertyBlock(leftProperties);
                rightRenderer.GetPropertyBlock(rightProperties);
                var leftMask = leftProperties.GetTexture("_FormationGroundMask") as Texture2D;
                var rightMask = rightProperties.GetTexture("_FormationGroundMask") as Texture2D;
                Assert.That(leftMask, Is.Not.Null);
                Assert.That(rightMask, Is.Not.Null);
                var leftEdge = leftMask.GetPixel(63, 32);
                var rightEdge = rightMask.GetPixel(0, 32);
                Assert.That(leftEdge.r, Is.GreaterThan(0f));
                Assert.That(leftEdge.g, Is.GreaterThan(0f));
                Assert.That(Mathf.Abs(leftEdge.r - rightEdge.r), Is.LessThan(0.15f));
                Assert.That(Mathf.Abs(leftEdge.g - rightEdge.g), Is.LessThan(0.15f));
                Assert.That(leftMesh.uv2[1].y, Is.EqualTo(0.6f),
                    "Formation treatment must preserve the original terrain sand channel.");
                rightObject.transform.position = Vector3.zero;
                TopDown3DFormationTerrainShader.RepositionMask(right, settings);
                rightRenderer.GetPropertyBlock(rightProperties);
                Assert.That(rightProperties.GetVector("_FormationGroundMaskOriginScale").x,
                    Is.EqualTo(0f).Within(0.00001f));
                Assert.That(rightProperties.GetTexture("_FormationGroundMask"), Is.SameAs(rightMask));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(leftObject);
                UnityEngine.Object.DestroyImmediate(rightObject);
                UnityEngine.Object.DestroyImmediate(leftMesh);
                UnityEngine.Object.DestroyImmediate(rightMesh);
            }
        }

        [Test]
        public void FormationGroundStones_UseTheApprovedFamilyAndRebuildDeterministically()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TopDown3DWorldSettings>(
                "Assets/_Project/Settings/World/TopDown3DWorldSettings.asset");
            var template = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(
                "Assets/_Project/Art/Environment/Rocks/Generated/MixedPileScatter.asset");
            var groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Materials/TopDown3D/Greybox_Terrain.mat");
            var staged = template.ApprovedStageEntries[0];
            var bounds = new Bounds(new Vector3(9f, 0.2f, 9f), new Vector3(2f, 1f, 2f));
            var member = new TopDown3DRockFormationMember("stone-source", staged.Family.StableId,
                TopDown3DRockSizeTier.Medium, staged.Family.Shape, 0,
                bounds.center, Quaternion.identity, Vector3.one, 0, -1, 1f, bounds,
                staged.Family, template.Members[staged.SourceIndex].Material, 0f);
            var formation = new TopDown3DRockFormationPlan(
                new TopDown3DRockRootKey(TopDown3DRockSizeTier.Medium, 0, 0, 1),
                "surface-stone-proof", 1, TopDown3DNaturalObjectLayer.Obstacle,
                TopDown3DRockSurface.Regular, new[] { member }, new Vector2(9f, 9f), 2f, 1f,
                template);
            var influence = new[] { new TopDown3DFormationTerrainShader.Influence(formation, Vector2.zero) };
            var root = new GameObject("Formation surface stone proof");
            var terrain = new Mesh();
            try
            {
                terrain.vertices = new[]
                {
                    new Vector3(0f, 0f, 0f), new Vector3(18f, 0f, 0f),
                    new Vector3(0f, 0f, 18f), new Vector3(18f, 0f, 18f)
                };
                terrain.triangles = new[] { 0, 2, 1, 1, 2, 3 };
                terrain.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                root.AddComponent<MeshFilter>().sharedMesh = terrain;
                root.AddComponent<MeshRenderer>().sharedMaterial = groundMaterial;
                var chunk = root.AddComponent<TopDown3DGeneratedChunk>();
                TopDown3DFormationTerrainShader.Apply(chunk, settings, influence);
                var first = chunk.DecorationRoot.Find("Formation Surface Stones");
                Assert.That(first, Is.Not.Null);
                var firstMaskProperties = new MaterialPropertyBlock();
                root.GetComponent<MeshRenderer>().GetPropertyBlock(firstMaskProperties);
                var firstMask = firstMaskProperties.GetTexture("_FormationGroundMask");
                var firstVertices = first.GetComponent<MeshFilter>().sharedMesh.vertices;
                Assert.That(firstVertices.Length, Is.GreaterThan(0));
                Assert.That(first.GetComponent<MeshRenderer>().sharedMaterial,
                    Is.SameAs(member.AuthoredMaterial));
                TopDown3DFormationTerrainShader.Apply(chunk, settings, influence);
                var second = chunk.DecorationRoot.Find("Formation Surface Stones");
                var secondMaskProperties = new MaterialPropertyBlock();
                root.GetComponent<MeshRenderer>().GetPropertyBlock(secondMaskProperties);
                Assert.That(second, Is.SameAs(first));
                Assert.That(secondMaskProperties.GetTexture("_FormationGroundMask"), Is.SameAs(firstMask));
                Assert.That(second.GetComponent<MeshFilter>().sharedMesh.vertices,
                    Is.EqualTo(firstVertices));
                chunk.ClearDecoration();
                TopDown3DFormationTerrainShader.Apply(chunk, settings, influence);
                var rebuilt = chunk.DecorationRoot.Find("Formation Surface Stones");
                Assert.That(rebuilt, Is.Not.Null);
                Assert.That(rebuilt.GetComponent<MeshFilter>().sharedMesh.vertices,
                    Is.EqualTo(firstVertices));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(terrain);
            }
        }

        [TestCase("MixedPileScatterReference", false, 12)]
        [TestCase("HandbuiltSpireReference", true, 4)]
        public void WorldBakeUsesTheSavedWorkbenchGeneration(string sourceName,
            bool variationEnabled, int stageSeed)
        {
            const string root = "Assets/_Project/Art/Environment/Rocks/";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(
                root + "Source/" + sourceName + ".prefab");
            var assetName = sourceName.Substring(0, sourceName.Length - "Reference".Length);
            var asset = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(
                root + "Generated/" + assetName + ".asset");
            Assert.That(source, Is.Not.Null);
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.HasApprovedStage, Is.True);
            Assert.That(asset.ApprovedStageVariationEnabled, Is.EqualTo(variationEnabled));
            Assert.That(asset.ApprovedStageSeed, Is.EqualTo(stageSeed));
            Assert.That(asset.ProceduralGenerations.Count, Is.EqualTo(3));
            Assert.That(asset.ProceduralGenerations.Select(g => g.Seed).Distinct().Count(), Is.EqualTo(3));

            var layout = variationEnabled
                ? TopDown3DAuthoredFormationVariation.GenerateLayout(asset, stageSeed) : null;
            foreach (var entry in asset.ApprovedStageEntries)
            {
                Assert.That(entry.Family.IsComplete, Is.True);
                Assert.That(entry.SourceIndex, Is.InRange(0, asset.Members.Count - 1));
                if (!variationEnabled)
                {
                    Assert.That(entry.Family.Lod0, Is.SameAs(asset.Members[entry.SourceIndex].Mesh));
                    Assert.That(entry.LocalPose, Is.EqualTo(asset.Members[entry.SourceIndex].LocalPose));
                }
                else
                {
                    var matching = layout.Entries.Single(candidate => candidate.InstanceId == entry.InstanceId);
                    Assert.That(entry.SourceIndex, Is.EqualTo(matching.SourceIndex));
                    Assert.That(entry.LocalPose, Is.EqualTo(matching.Transform));
                }
            }

            foreach (var generation in asset.ProceduralGenerations)
            {
                var generated = TopDown3DAuthoredFormationVariation.GenerateLayout(asset, generation.Seed);
                foreach (var entry in generation.Entries)
                {
                    var matching = generated.Entries.Single(candidate => candidate.InstanceId == entry.InstanceId);
                    Assert.That(entry.SourceIndex, Is.EqualTo(matching.SourceIndex));
                    Assert.That(entry.LocalPose, Is.EqualTo(matching.Transform));
                    Assert.That(entry.Family.IsComplete, Is.True);
                }
            }

            if (!variationEnabled) return;
            var first = asset.ApprovedStageEntries[0];
            var ordinal = -1;
            for (var i = 0; i < layout.Entries.Count; i++)
                if (layout.Entries[i].InstanceId == first.InstanceId) { ordinal = i; break; }
            Assert.That(ordinal, Is.GreaterThanOrEqualTo(0));
            var sourceId = asset.Members[first.SourceIndex].SourceId;
            var rock = source.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true)
                .Single(candidate =>
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out string guid, out long localId);
                    return guid + ":" + localId == sourceId;
                });
            var shapeSeed = unchecked(rock.GenerationSeed ^ stageSeed * 486187739
                ^ ordinal * 16777619);
            var regenerated = TopDown3DMixedFormationShapeVariation.BuildMesh(rock, shapeSeed,
                asset.Members[first.SourceIndex].Mesh.bounds);
            try
            {
                Assert.That(first.Family.Lod0.vertexCount, Is.EqualTo(regenerated.vertexCount));
                Assert.That(first.Family.Lod0.triangles.Length, Is.EqualTo(regenerated.triangles.Length));
                Assert.That(first.Family.Lod0.bounds.center, Is.EqualTo(regenerated.bounds.center));
                Assert.That(first.Family.Lod0.bounds.size, Is.EqualTo(regenerated.bounds.size));
            }
            finally { UnityEngine.Object.DestroyImmediate(regenerated); }
        }
    }
}
