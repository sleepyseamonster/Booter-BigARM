using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    internal static class TopDown3DManifoldNative
    {
        private const int PositionPropertyCount = 3;

#if UNITY_EDITOR_OSX
        internal static bool IsSupportedPlatform => true;
#else
        internal static bool IsSupportedPlatform => false;
#endif

        internal static bool TryUnionMany(
            IReadOnlyList<TopDown3DIndexedMeshData> solids,
            out TopDown3DIndexedMeshData result,
            out double volume,
            out string error)
        {
            result = null;
            volume = 0.0;
            error = null;
            if (!IsSupportedPlatform)
            {
                error = "Manifold rock fusion is supported only as a macOS Editor authoring tool.";
                return false;
            }

            if (solids == null || solids.Count < 2)
            {
                error = "A Boolean formation union requires at least two solids.";
                return false;
            }

            var nativeSolids = new List<SafeManifoldHandle>(solids.Count);
            try
            {
                for (var i = 0; i < solids.Count; i++)
                {
                    var report = TopDown3DRockMeshTopology.Validate(solids[i]);
                    if (!report.IsValid || report.SignedVolume <= 0.0)
                    {
                        error = $"Fusion input {i} is not a positive-volume closed manifold: {report.Error}";
                        return false;
                    }

                    nativeSolids.Add(CreateManifold(solids[i]));
                }

                using (var vector = CreateVector(nativeSolids))
                using (var union = CreateBatchUnion(vector))
                {
                    var status = NativeMethods.manifold_status(union.DangerousGetHandle());
                    if (status != ManifoldError.NoError)
                    {
                        error = $"Manifold batch union failed with status {status}.";
                        return false;
                    }

                    if (NativeMethods.manifold_is_empty(union.DangerousGetHandle()) != 0)
                    {
                        error = "Manifold batch union returned an empty solid.";
                        return false;
                    }

                    using (var components = Decompose(union))
                    {
                        var componentCount = ToInt(
                            NativeMethods.manifold_manifold_vec_length(
                                components.DangerousGetHandle()));
                        if (componentCount != 1)
                        {
                            error = $"Manifold batch union produced {componentCount} disconnected components.";
                            return false;
                        }
                    }

                    volume = NativeMethods.manifold_volume(union.DangerousGetHandle());
                    if (double.IsNaN(volume) || double.IsInfinity(volume) || volume <= 0.0)
                    {
                        error = $"Manifold batch union returned invalid volume {volume}.";
                        return false;
                    }

                    using (var mesh = ExtractMesh(union))
                    {
                        if (!TryReadMesh(mesh, out result, out error))
                        {
                            return false;
                        }
                    }
                }

                var outputReport = TopDown3DRockMeshTopology.Validate(result);
                if (!outputReport.IsValid || outputReport.SignedVolume <= 0.0)
                {
                    error = $"Extracted union topology is invalid: {outputReport.Error}";
                    result = null;
                    return false;
                }

                // Recheck the actual float-representable surface, not only the native
                // double result: a collapsed narrow connection must not hide a split.
                using (var converted = CreateManifold(result))
                {
                    if (NativeMethods.manifold_status(converted.DangerousGetHandle()) != ManifoldError.NoError)
                    {
                        error = "Float-converted union is not a closed manifold.";
                        result = null;
                        return false;
                    }

                    using (var components = Decompose(converted))
                    {
                        if (ToInt(NativeMethods.manifold_manifold_vec_length(
                            components.DangerousGetHandle())) != 1)
                        {
                            error = "Float-converted union is not a single connected component.";
                            result = null;
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (DllNotFoundException exception)
            {
                error = $"Manifold native library could not be loaded: {exception.Message}";
                return false;
            }
            catch (EntryPointNotFoundException exception)
            {
                error = $"Manifold native ABI is incompatible: {exception.Message}";
                return false;
            }
            catch (BadImageFormatException exception)
            {
                error = $"Manifold native binary has the wrong architecture: {exception.Message}";
                return false;
            }
            catch (OverflowException exception)
            {
                error = $"Manifold output is too large for a Unity mesh: {exception.Message}";
                return false;
            }
            catch (Exception exception)
            {
                error = $"Unexpected Manifold interop failure: {exception.GetType().Name}: {exception.Message}";
                return false;
            }
            finally
            {
                for (var i = nativeSolids.Count - 1; i >= 0; i--)
                {
                    nativeSolids[i].Dispose();
                }
            }
        }

        private static SafeManifoldHandle CreateManifold(TopDown3DIndexedMeshData solid)
        {
            var properties = new double[solid.Vertices.Length * PositionPropertyCount];
            for (var i = 0; i < solid.Vertices.Length; i++)
            {
                var offset = i * PositionPropertyCount;
                properties[offset] = solid.Vertices[i].x;
                properties[offset + 1] = solid.Vertices[i].y;
                properties[offset + 2] = solid.Vertices[i].z;
            }

            var triangles = new long[solid.Triangles.Length];
            for (var i = 0; i < solid.Triangles.Length; i++)
            {
                triangles[i] = solid.Triangles[i];
            }

            using (var mesh = CreateMesh(properties, solid.Vertices.Length, triangles))
            {
                var memory = NativeMethods.manifold_alloc_manifold();
                var handle = NativeMethods.manifold_of_meshgl64(
                    memory,
                    mesh.DangerousGetHandle());
                return new SafeManifoldHandle(handle);
            }
        }

        private static SafeMeshGl64Handle CreateMesh(
            double[] properties,
            int vertexCount,
            long[] triangles)
        {
            var memory = NativeMethods.manifold_alloc_meshgl64();
            var handle = NativeMethods.manifold_meshgl64(
                memory,
                properties,
                ToSize(vertexCount),
                ToSize(PositionPropertyCount),
                triangles,
                ToSize(triangles.Length / 3));
            return new SafeMeshGl64Handle(handle);
        }

        private static SafeManifoldVectorHandle CreateVector(
            IReadOnlyList<SafeManifoldHandle> solids)
        {
            var memory = NativeMethods.manifold_alloc_manifold_vec();
            var handle = NativeMethods.manifold_manifold_empty_vec(memory);
            var vector = new SafeManifoldVectorHandle(handle);
            for (var i = 0; i < solids.Count; i++)
            {
                NativeMethods.manifold_manifold_vec_push_back(
                    vector.DangerousGetHandle(),
                    solids[i].DangerousGetHandle());
            }

            return vector;
        }

        private static SafeManifoldHandle CreateBatchUnion(
            SafeManifoldVectorHandle solids)
        {
            var memory = NativeMethods.manifold_alloc_manifold();
            var handle = NativeMethods.manifold_batch_boolean(
                memory,
                solids.DangerousGetHandle(),
                ManifoldOperation.Add);
            return new SafeManifoldHandle(handle);
        }

        private static SafeManifoldVectorHandle Decompose(SafeManifoldHandle manifold)
        {
            var memory = NativeMethods.manifold_alloc_manifold_vec();
            var handle = NativeMethods.manifold_decompose(
                memory,
                manifold.DangerousGetHandle());
            return new SafeManifoldVectorHandle(handle);
        }

        private static SafeMeshGl64Handle ExtractMesh(SafeManifoldHandle manifold)
        {
            var memory = NativeMethods.manifold_alloc_meshgl64();
            var handle = NativeMethods.manifold_get_meshgl64(
                memory,
                manifold.DangerousGetHandle());
            return new SafeMeshGl64Handle(handle);
        }

        private static bool TryReadMesh(
            SafeMeshGl64Handle mesh,
            out TopDown3DIndexedMeshData result,
            out string error)
        {
            result = null;
            error = null;
            var nativeMesh = mesh.DangerousGetHandle();
            var propertyCount = ToInt(NativeMethods.manifold_meshgl64_num_prop(nativeMesh));
            var vertexCount = ToInt(NativeMethods.manifold_meshgl64_num_vert(nativeMesh));
            var triangleCount = ToInt(NativeMethods.manifold_meshgl64_num_tri(nativeMesh));
            var propertyLength = ToInt(
                NativeMethods.manifold_meshgl64_vert_properties_length(nativeMesh));
            var triangleLength = ToInt(NativeMethods.manifold_meshgl64_tri_length(nativeMesh));
            if (propertyCount != PositionPropertyCount
                || propertyLength != vertexCount * PositionPropertyCount
                || triangleLength != triangleCount * 3)
            {
                error = "Manifold extraction returned an unexpected mesh property layout.";
                return false;
            }

            var propertiesPointer = IntPtr.Zero;
            var trianglesPointer = IntPtr.Zero;
            try
            {
                propertiesPointer = Marshal.AllocHGlobal(checked(propertyLength * sizeof(double)));
                trianglesPointer = Marshal.AllocHGlobal(checked(triangleLength * sizeof(long)));
                NativeMethods.manifold_meshgl64_vert_properties(propertiesPointer, nativeMesh);
                NativeMethods.manifold_meshgl64_tri_verts(trianglesPointer, nativeMesh);

                var properties = new double[propertyLength];
                var nativeTriangles = new long[triangleLength];
                Marshal.Copy(propertiesPointer, properties, 0, propertyLength);
                Marshal.Copy(trianglesPointer, nativeTriangles, 0, triangleLength);

                var vertices = new Vector3[vertexCount];
                for (var i = 0; i < vertexCount; i++)
                {
                    var offset = i * PositionPropertyCount;
                    if (!TryToFloat(properties[offset], out var x)
                        || !TryToFloat(properties[offset + 1], out var y)
                        || !TryToFloat(properties[offset + 2], out var z))
                    {
                        error = $"Manifold vertex {i} cannot be represented safely by Unity floats.";
                        return false;
                    }

                    vertices[i] = new Vector3(x, y, z);
                }

                var triangles = new int[triangleLength];
                for (var i = 0; i < triangleLength; i++)
                {
                    triangles[i] = checked((int)nativeTriangles[i]);
                }

                result = NormalizeConvertedMesh(vertices, triangles);
                return true;
            }
            finally
            {
                if (trianglesPointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(trianglesPointer);
                }

                if (propertiesPointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(propertiesPointer);
                }
            }
        }

        internal static TopDown3DIndexedMeshData NormalizeConvertedMesh(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> triangles)
        {
            var normalized = TopDown3DRockMeshTopology.Normalize(vertices, triangles);
            var retained = new List<int>(normalized.Triangles.Length);
            for (var i = 0; i < normalized.Triangles.Length; i += 3)
            {
                var a = normalized.Triangles[i];
                var b = normalized.Triangles[i + 1];
                var c = normalized.Triangles[i + 2];
                // Distinct native doubles can become the exact same Unity float position.
                // Remove only faces collapsed by that exact weld, never near-zero slivers.
                // The caller still requires closed, positive-volume manifold topology.
                if (a == b || b == c || c == a) continue;
                retained.Add(a);
                retained.Add(b);
                retained.Add(c);
            }

            return retained.Count == normalized.Triangles.Length
                ? normalized
                : TopDown3DRockMeshTopology.Normalize(normalized.Vertices, retained);
        }

        private static bool TryToFloat(double value, out float converted)
        {
            converted = (float)value;
            return !double.IsNaN(value) && !double.IsInfinity(value)
                && !float.IsNaN(converted) && !float.IsInfinity(converted);
        }

        private static UIntPtr ToSize(int value)
        {
            return new UIntPtr(checked((uint)value));
        }

        private static int ToInt(UIntPtr value)
        {
            return checked((int)value.ToUInt64());
        }

        private enum ManifoldError
        {
            NoError,
            NonFiniteVertex,
            NotManifold,
            VertexIndexOutOfBounds,
            PropertiesWrongLength,
            MissingPositionProperties,
            MergeVectorsDifferentLengths,
            MergeIndexOutOfBounds,
            TransformWrongLength,
            RunIndexWrongLength,
            FaceIdWrongLength,
            InvalidConstruction,
            ResultTooLarge,
            InvalidTangents,
            Cancelled
        }

        private enum ManifoldOperation
        {
            Add,
            Subtract,
            Intersect
        }

        private sealed class SafeManifoldHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal SafeManifoldHandle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                try
                {
                    NativeMethods.manifold_delete_manifold(handle);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private sealed class SafeManifoldVectorHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal SafeManifoldVectorHandle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                try
                {
                    NativeMethods.manifold_delete_manifold_vec(handle);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private sealed class SafeMeshGl64Handle : SafeHandleZeroOrMinusOneIsInvalid
        {
            internal SafeMeshGl64Handle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                try
                {
                    NativeMethods.manifold_delete_meshgl64(handle);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static class NativeMethods
        {
            private const string LibraryName = "manifoldc";

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr manifold_alloc_manifold();

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr manifold_alloc_manifold_vec();

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr manifold_alloc_meshgl64();

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr manifold_meshgl64(
                IntPtr memory,
                [In] double[] vertexProperties,
                UIntPtr vertexCount,
                UIntPtr propertyCount,
                [In] long[] triangleVertices,
                UIntPtr triangleCount);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr manifold_of_meshgl64(IntPtr memory, IntPtr mesh);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr manifold_manifold_empty_vec(IntPtr memory);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void manifold_manifold_vec_push_back(
                IntPtr vector,
                IntPtr manifold);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UIntPtr manifold_manifold_vec_length(IntPtr vector);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr manifold_batch_boolean(
                IntPtr memory,
                IntPtr vector,
                ManifoldOperation operation);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern ManifoldError manifold_status(IntPtr manifold);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int manifold_is_empty(IntPtr manifold);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr manifold_decompose(IntPtr memory, IntPtr manifold);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern double manifold_volume(IntPtr manifold);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr manifold_get_meshgl64(IntPtr memory, IntPtr manifold);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UIntPtr manifold_meshgl64_num_prop(IntPtr mesh);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UIntPtr manifold_meshgl64_num_vert(IntPtr mesh);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UIntPtr manifold_meshgl64_num_tri(IntPtr mesh);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UIntPtr manifold_meshgl64_vert_properties_length(IntPtr mesh);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UIntPtr manifold_meshgl64_tri_length(IntPtr mesh);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr manifold_meshgl64_vert_properties(IntPtr memory, IntPtr mesh);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr manifold_meshgl64_tri_verts(IntPtr memory, IntPtr mesh);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void manifold_delete_manifold(IntPtr manifold);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void manifold_delete_manifold_vec(IntPtr vector);

            [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void manifold_delete_meshgl64(IntPtr mesh);
        }
    }
}
