using System;
using System.Collections.Generic;
using BooterBigArm.TopDown3D;
using BooterBigArm.TopDown3D.WorldCreator;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BooterBigArm.Editor
{
    // Disposable authoring presentation, not a second atmosphere or world simulation.
    [InitializeOnLoad]
    internal static class TopDown3DMixedFormationBlowingSand
    {
        private const int Count = 64;
        private const int Samples = 33;
        private static readonly List<Preview> Previews = new List<Preview>();
        private static double nextFrame;

        private sealed class Preview
        {
            internal TopDown3DLandscapeAuthoringSandbox Owner;
            internal ParticleSystem System;
            internal Material Material;
            internal Texture2D Texture;
            internal readonly Vector3[,] Positions = new Vector3[Count, Samples];
            internal readonly float[,] Visibility = new float[Count, Samples];
            internal readonly float[] Phase = new float[Count];
            internal readonly float[] Duration = new float[Count];
            internal readonly float[] Angle = new float[Count];
            internal readonly ParticleSystem.Particle[] Particles = new ParticleSystem.Particle[Count];
            internal double Started;

            internal void Dispose()
            {
                if (System != null) Object.DestroyImmediate(System.gameObject);
                if (Material != null) Object.DestroyImmediate(Material);
                if (Texture != null) Object.DestroyImmediate(Texture);
            }
        }

        static TopDown3DMixedFormationBlowingSand()
        {
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += ClearAll;
            EditorApplication.quitting += ClearAll;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode) ClearAll();
            };
        }

        internal static void Apply(TopDown3DLandscapeAuthoringSandbox owner, Transform parent,
            MeshCollider[] terrain, TopDown3DRockWorkbenchAuthoring[] rocks)
        {
            Clear(owner);
            if (rocks.Length == 0 || Application.isBatchMode) return;
            var preview = new Preview { Owner = owner, Started = EditorApplication.timeSinceStartup };
            try
            {
                var area = rocks[0].GetComponent<MeshRenderer>().bounds;
                var obstacles = new List<Bounds>();
                foreach (var rock in rocks)
                {
                    var bounds = rock.GetComponent<MeshRenderer>().bounds;
                    area.Encapsulate(bounds);
                    bounds.Expand(0.25f);
                    obstacles.Add(bounds);
                }
                area.Expand(2f);
                var generator = new TopDown3DWorldGenerator(owner.WorldSettings);
                var seed = unchecked(owner.WorldSettings.WorldSeed ^ owner.CenterChunk.x * 73856093
                    ^ owner.CenterChunk.y * 19349663);
                var random = new System.Random(seed);
                float Unit() => (float)random.NextDouble();
                Physics.SyncTransforms();
                for (var i = 0; i < Count; i++)
                {
                    var center = new Vector3(Mathf.Lerp(area.min.x, area.max.x, Unit()), 0f,
                        Mathf.Lerp(area.min.z, area.max.z, Unit()));
                    if (!generator.Authority.Materials.TrySample(
                            new AbsoluteWorldPosition(center.x, 0d, center.z), out var material, out var error))
                        throw new InvalidOperationException(error);
                    var angle = material.PrevailingWindDirection * Mathf.PI * 2f;
                    var wind = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    var cross = new Vector3(-wind.z, 0f, wind.x);
                    var length = Mathf.Lerp(3f, 6f, Unit());
                    preview.Phase[i] = Unit();
                    preview.Duration[i] = length / Mathf.Lerp(0.45f, 0.85f, Unit());
                    preview.Angle[i] = -angle * Mathf.Rad2Deg;
                    for (var j = 0; j < Samples; j++)
                    {
                        var fraction = j / (Samples - 1f);
                        var point = center + wind * ((fraction - 0.5f) * length)
                            + cross * (Mathf.Sin(fraction * Mathf.PI * 2f + preview.Phase[i] * 6f) * 0.12f);
                        var visible = 0f;
                        foreach (var ground in terrain)
                        {
                            var box = ground.bounds;
                            if (point.x < box.min.x || point.x > box.max.x || point.z < box.min.z || point.z > box.max.z)
                                continue;
                            if (!ground.Raycast(new Ray(new Vector3(point.x, box.max.y + 1f, point.z), Vector3.down),
                                    out var hit, box.size.y + 2f)) continue;
                            point.y = hit.point.y + 0.065f;
                            visible = 1f;
                            break;
                        }
                        foreach (var rock in obstacles)
                        {
                            // Fade before solid rock; reduce flow in its near downwind shelter.
                            var dx = Mathf.Max(0f, Mathf.Abs(point.x - rock.center.x) - rock.extents.x);
                            var dz = Mathf.Max(0f, Mathf.Abs(point.z - rock.center.z) - rock.extents.z);
                            visible *= Mathf.SmoothStep(0f, 1f, Mathf.Sqrt(dx * dx + dz * dz) / 0.35f);
                            var delta = point - rock.center;
                            var along = Vector3.Dot(delta, wind);
                            var width = Mathf.Abs(wind.z) * rock.extents.x + Mathf.Abs(wind.x) * rock.extents.z;
                            if (along > 0f && along < 2f && Mathf.Abs(Vector3.Dot(delta, cross)) < width)
                                visible *= Mathf.Lerp(0.25f, 1f, along / 2f);
                        }
                        preview.Positions[i, j] = point;
                        preview.Visibility[i, j] = visible;
                    }
                }

                preview.Texture = TopDown3DDustAtmosphere.CreateSoftDustTexture();
                preview.Material = TopDown3DDustAtmosphere.CreateParticleMaterial(preview.Texture, false);
                if (preview.Material == null) throw new InvalidOperationException("Blowing sand needs the existing URP dust shader.");
                var child = new GameObject("Blowing Sand — Preview") { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
                child.transform.SetParent(parent, false);
                preview.System = child.AddComponent<ParticleSystem>();
                preview.System.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = preview.System.main;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = Count;
                main.startSize3D = true;
                var emission = preview.System.emission;
                emission.enabled = false;
                var shape = preview.System.shape;
                shape.enabled = false;
                var renderer = child.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = preview.Material;
                renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                // Explicit bounds: SetParticles is driven while simulation is paused in edit mode.
                area.Expand(14f);
                renderer.localBounds = new Bounds(child.transform.InverseTransformPoint(area.center), area.size);
                preview.System.Play();
                preview.System.Pause();
                Previews.Add(preview);
            }
            catch
            {
                preview.Dispose();
                throw;
            }
        }

        internal static void Clear(TopDown3DLandscapeAuthoringSandbox owner)
        {
            for (var i = Previews.Count - 1; i >= 0; i--)
                if (Previews[i].Owner == owner)
                {
                    Previews[i].Dispose();
                    Previews.RemoveAt(i);
                }
        }

        private static void ClearAll()
        {
            foreach (var preview in Previews) preview.Dispose();
            Previews.Clear();
        }

        private static void Tick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            var now = EditorApplication.timeSinceStartup;
            if (now < nextFrame) return;
            nextFrame = now + 1d / 24d;
            var repaint = false;
            for (var p = Previews.Count - 1; p >= 0; p--)
            {
                var preview = Previews[p];
                if (preview.Owner == null || preview.System == null)
                {
                    preview.Dispose();
                    Previews.RemoveAt(p);
                    continue;
                }
                var strength = preview.Owner.isActiveAndEnabled ? preview.Owner.BlowingSand : 0f;
                if (strength <= 0f)
                {
                    if (preview.System.particleCount > 0)
                    {
                        preview.System.Clear();
                        repaint = true;
                    }
                    continue;
                }
                if (SceneView.sceneViews.Count == 0) continue;
                var time = (float)(now - preview.Started);
                var gust = 0.65f + 0.35f * Mathf.Sin(time * 0.7f);
                for (var i = 0; i < Count; i++)
                {
                    var phase = Mathf.Repeat(time / preview.Duration[i] + preview.Phase[i], 1f);
                    var sample = phase * (Samples - 1);
                    var low = Mathf.Min(Samples - 2, Mathf.FloorToInt(sample));
                    var fraction = sample - low;
                    var visibility = Mathf.Lerp(preview.Visibility[i, low], preview.Visibility[i, low + 1], fraction);
                    var tint = TopDown3DDustAtmosphere.DefaultRustParticleDust;
                    tint.a = strength * 0.28f * gust * visibility * Mathf.Sin(phase * Mathf.PI);
                    preview.Particles[i] = new ParticleSystem.Particle
                    {
                        position = Vector3.Lerp(preview.Positions[i, low], preview.Positions[i, low + 1], fraction),
                        startColor = tint,
                        startSize3D = new Vector3(0.75f, 0.22f, 0.22f),
                        rotation = preview.Angle[i],
                        startLifetime = 1f,
                        remainingLifetime = 1f,
                        randomSeed = (uint)(i + 1)
                    };
                }
                preview.System.SetParticles(preview.Particles, Count);
                repaint = true;
            }
            if (repaint) SceneView.RepaintAll();
        }
    }
}
