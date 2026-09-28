using System.Collections.Generic;
using System.Linq;
using BooterBigArm.Editor;
using BooterBigArm.TopDown3D;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BooterBigArm.Tests
{
    public sealed class TopDown3DAuthoredFormationTemplateTests
    {
        [Test]
        public void HandbuiltSpireReferencePreservesAuthoredComposition()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                TopDown3DAuthoredFormationTemplateCapture.HandbuiltSpireReferencePath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(Quaternion.Angle(prefab.transform.localRotation, Quaternion.identity),
                Is.LessThan(0.001f));
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one));

            var rocks = prefab.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
            Assert.That(rocks, Has.Length.EqualTo(25));
            Assert.That(rocks.Any(rock => Quaternion.Angle(
                rock.transform.localRotation, Quaternion.identity) > 1f), Is.True);
            Assert.That(rocks.Any(rock =>
                (rock.transform.localScale - Vector3.one).sqrMagnitude > 0.01f), Is.True);

            foreach (var rock in rocks)
            {
                var mesh = rock.GetComponent<MeshFilter>()?.sharedMesh;
                var material = rock.GetComponent<MeshRenderer>()?.sharedMaterial;
                Assert.That(mesh, Is.Not.Null, rock.name + " is missing its captured mesh.");
                Assert.That(EditorUtility.IsPersistent(mesh), Is.True,
                    rock.name + " uses a transient mesh.");
                Assert.That(material, Is.Not.Null, rock.name + " is missing its material.");
                Assert.That(new SerializedObject(rock).FindProperty("autoRebuild").boolValue,
                    Is.False, rock.name + " should preserve its captured mesh.");
            }

            var instance = Object.Instantiate(prefab);
            try
            {
                var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
                var bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.002f));
                Assert.That(bounds.center.x, Is.EqualTo(0f).Within(0.002f));
                Assert.That(bounds.center.z, Is.EqualTo(0f).Within(0.002f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void HandbuiltSpireBakeRetainsEveryCapturedRockTransform()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                TopDown3DAuthoredFormationTemplateCapture.HandbuiltSpireReferencePath);
            var asset = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(
                TopDown3DMixedFormationAssetBaker.GetOutputPath(prefab));
            Assert.That(prefab, Is.Not.Null);
            Assert.That(asset, Is.Not.Null);
            var rocks = prefab.GetComponentsInChildren<TopDown3DRockWorkbenchAuthoring>(true);
            Assert.That(rocks, Has.Length.EqualTo(25));
            Assert.That(asset.Members, Has.Count.EqualTo(rocks.Length));
            foreach (var rock in rocks)
            {
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    rock, out string guid, out long localId), Is.True);
                var member = asset.Members.Single(candidate =>
                    candidate.SourceId == guid + ":" + localId);
                var pose = prefab.transform.worldToLocalMatrix * rock.transform.localToWorldMatrix;
                Assert.That(MatrixDistance(member.LocalPose, pose), Is.LessThan(0.000001f),
                    rock.name + " transform changed during baking.");
                Assert.That(member.Mesh, Is.SameAs(rock.GetComponent<MeshFilter>().sharedMesh));
            }
        }

        [Test]
        public void FormationCatalogContainsBothTemplatesAndSelectsDeterministically()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationCatalog>(
                TopDown3DMixedFormationAssetBaker.CatalogPath);
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(
                TopDown3DLandscapeAuthoringSandboxEditor.MixedReferencePath);
            var spire = AssetDatabase.LoadAssetAtPath<GameObject>(
                TopDown3DAuthoredFormationTemplateCapture.HandbuiltSpireReferencePath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.IsComplete, Is.True);
            Assert.That(catalog.Templates.Select(template => template.SourceGuid),
                Does.Contain(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original))));
            Assert.That(catalog.Templates.Select(template => template.SourceGuid),
                Does.Contain(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(spire))));
            Assert.That(catalog.Select("formation:test:42"),
                Is.SameAs(catalog.Select("formation:test:42")));
            Assert.That(TopDown3DMixedFormationAssetBaker.GetOutputPath(original),
                Is.EqualTo(TopDown3DMixedFormationAssetBaker.OutputPath));

            var asset = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(
                TopDown3DMixedFormationAssetBaker.GetOutputPath(spire));
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.Members, Has.Count.EqualTo(25));
            Assert.That(asset.Members.All(member => member.Mesh != null && member.Material != null),
                Is.True);
        }

        [Test]
        public void HandbuiltSpireVariationsRemainDeterministicAndSupported()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TopDown3DAuthoredFormationAsset>(
                TopDown3DMixedFormationAssetBaker.GetOutputPath(
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        TopDown3DAuthoredFormationTemplateCapture.HandbuiltSpireReferencePath)));
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.VariationProfile,
                Is.EqualTo(TopDown3DAuthoredFormationVariationProfile.HandbuiltSpire));

            var counts = new HashSet<int>();
            var foundShorterTallStone = false;
            for (var seed = 0; seed < 64; seed++)
            {
                var first = TopDown3DAuthoredFormationVariation.GenerateLayout(asset, seed);
                var second = TopDown3DAuthoredFormationVariation.GenerateLayout(asset, seed);
                Assert.That(first.Entries, Is.Not.Empty);
                Assert.That(first.Entries.Count, Is.LessThanOrEqualTo(asset.Members.Count + 3));
                Assert.That(second.Entries.Count, Is.EqualTo(first.Entries.Count));
                Assert.That(first.Entries.Select(entry => entry.InstanceId), Is.Unique);
                for (var i = 0; i < first.Entries.Count; i++)
                {
                    Assert.That(second.Entries[i].InstanceId, Is.EqualTo(first.Entries[i].InstanceId));
                    Assert.That(MatrixDistance(second.Entries[i].Transform, first.Entries[i].Transform),
                        Is.LessThan(0.000001f));
                }
                AssertSupported(asset, first);
                counts.Add(first.Entries.Count);
                var unchangedHeight = FormationHeight(asset,
                    TopDown3DAuthoredFormationVariation.Generate(asset, seed));
                foundShorterTallStone |= FormationHeight(asset, first) < unchangedHeight * 0.96f;
            }

            Assert.That(counts.Any(count => count < asset.Members.Count), Is.True);
            Assert.That(counts.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(foundShorterTallStone, Is.True);
        }

        private static void AssertSupported(TopDown3DAuthoredFormationAsset asset,
            TopDown3DAuthoredFormationVariation.Layout layout)
        {
            var bounds = layout.Entries.Select(entry => TransformBounds(
                asset.Members[entry.SourceIndex].Mesh.bounds, entry.Transform)).ToArray();
            var floor = bounds.Min(candidate => candidate.min.y);
            var order = Enumerable.Range(0, bounds.Length)
                .OrderBy(index => bounds[index].min.y).ToArray();
            var supported = new List<int>();
            foreach (var index in order)
            {
                var candidate = bounds[index];
                var grounded = candidate.min.y <= floor + Mathf.Max(0.08f, candidate.size.y * 0.12f);
                var stacked = supported.Any(lower => Supports(bounds[lower], candidate));
                Assert.That(grounded || stacked, Is.True,
                    layout.Entries[index].InstanceId + " is visibly unsupported.");
                supported.Add(index);
            }
        }

        private static bool Supports(Bounds lower, Bounds upper)
        {
            if (lower.center.y >= upper.center.y) return false;
            var tolerance = Mathf.Max(0.06f, Mathf.Min(lower.size.y, upper.size.y) * 0.16f);
            if (lower.max.y < upper.min.y - tolerance) return false;
            var overlapX = Mathf.Min(lower.max.x, upper.max.x) - Mathf.Max(lower.min.x, upper.min.x);
            var overlapZ = Mathf.Min(lower.max.z, upper.max.z) - Mathf.Max(lower.min.z, upper.min.z);
            return overlapX > 0f && overlapZ > 0f
                && overlapX * overlapZ / Mathf.Max(0.0001f, upper.size.x * upper.size.z) >= 0.06f;
        }

        private static float FormationHeight(TopDown3DAuthoredFormationAsset asset,
            Matrix4x4[] transforms)
        {
            var bounds = TransformBounds(asset.Members[0].Mesh.bounds, transforms[0]);
            for (var i = 1; i < transforms.Length; i++)
                bounds.Encapsulate(TransformBounds(asset.Members[i].Mesh.bounds, transforms[i]));
            return bounds.size.y;
        }

        private static float FormationHeight(TopDown3DAuthoredFormationAsset asset,
            TopDown3DAuthoredFormationVariation.Layout layout)
        {
            var first = layout.Entries[0];
            var bounds = TransformBounds(asset.Members[first.SourceIndex].Mesh.bounds, first.Transform);
            for (var i = 1; i < layout.Entries.Count; i++)
            {
                var entry = layout.Entries[i];
                bounds.Encapsulate(TransformBounds(asset.Members[entry.SourceIndex].Mesh.bounds,
                    entry.Transform));
            }
            return bounds.size.y;
        }

        private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            var result = new Bounds(matrix.MultiplyPoint3x4(bounds.center), Vector3.zero);
            for (var corner = 0; corner < 8; corner++)
                result.Encapsulate(matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1,
                        (corner & 4) == 0 ? -1 : 1))));
            return result;
        }

        private static float MatrixDistance(Matrix4x4 left, Matrix4x4 right)
        {
            var distance = 0f;
            for (var i = 0; i < 16; i++) distance += Mathf.Abs(left[i] - right[i]);
            return distance;
        }

    }
}
