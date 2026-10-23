using System;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Versioned contract. Bone paths are relative to the imported model root.</summary>
    public sealed class TopDown3DWardrobeRig : ScriptableObject
    {
        public int schemaVersion = 1;
        public string bodyId;
        public Mesh bodyMesh;
        public string[] bonePaths = Array.Empty<string>();
        // One region per triangle, flattened in submesh order. Boundary triangles remain visible.
        public int[] triangleRegions = Array.Empty<int>();
    }
}
