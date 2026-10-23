using System;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Offline pose snapshots from actual evaluated bone matrices, independent of PlayerLoop/GPU skinning caches.</summary>
    public static class WardrobePoseBake
    {
        public static Mesh Bake(SkinnedMeshRenderer renderer)
        {
            var source = renderer.sharedMesh;
            if (!source || !source.isReadable) throw new ArgumentException("Readable skin required.");
            var vertices = source.vertices; var normals = source.normals; var bones = renderer.bones; var binds = source.bindposes;
            var matrices = new Matrix4x4[bones.Length];
            var normalMatrices = new Matrix4x4[bones.Length];
            for (var b = 0; b < bones.Length; b++)
            { matrices[b] = renderer.transform.worldToLocalMatrix * bones[b].localToWorldMatrix * binds[b]; normalMatrices[b] = matrices[b].inverse.transpose; }
            var weights = source.GetAllBoneWeights(); var counts = source.GetBonesPerVertex(); // Mesh-owned views.
            var result = new Vector3[vertices.Length]; var resultNormals = new Vector3[normals.Length]; var offset = 0;
            for (var v = 0; v < vertices.Length; v++)
            {
                for (var influence = 0; influence < counts[v]; influence++)
                {
                    var weight = weights[offset++]; var matrix = matrices[weight.boneIndex];
                    result[v] += matrix.MultiplyPoint3x4(vertices[v]) * weight.weight;
                    if (normals.Length == vertices.Length)
                        resultNormals[v] += normalMatrices[weight.boneIndex].MultiplyVector(normals[v]) * weight.weight;
                }
                if (normals.Length == vertices.Length) resultNormals[v].Normalize();
                if (!float.IsFinite(result[v].x) || !float.IsFinite(result[v].y) || !float.IsFinite(result[v].z))
                    throw new InvalidOperationException("Non-finite evaluated garment vertex.");
            }
            var mesh = UnityEngine.Object.Instantiate(source); mesh.name = "Offline wardrobe pose";
            mesh.vertices = result; if (normals.Length == vertices.Length) mesh.normals = resultNormals;
            mesh.RecalculateBounds(); return mesh;
        }
    }
}
