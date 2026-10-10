using System;
using System.Collections.Generic;
using UnityEngine;

namespace BooterBigArm.TopDown3D
{
    /// <summary>Bounded art heuristic shared by sand coverage, gravel and shallow relief.
    /// Inputs are immutable placed footprints; this is not erosion or airflow simulation.</summary>
    public sealed class FixedTerrainRockContactField
    {
        public readonly struct Footprint
        {
            public readonly Vector2 Center;
            public readonly Vector2 HalfSize;
            public Footprint(Vector2 center, Vector2 halfSize)
            {
                if (!Finite(center.x) || !Finite(center.y) || !Finite(halfSize.x) || !Finite(halfSize.y)
                    || halfSize.x <= 0f || halfSize.y <= 0f) throw new ArgumentException("Invalid rock footprint.");
                Center = center; HalfSize = halfSize;
            }
        }
        public readonly struct Sample
        {
            public readonly float Sand, Gravel, Relief;
            public Sample(float sand, float gravel, float relief) { Sand = sand; Gravel = gravel; Relief = relief; }
        }
        private readonly Footprint[] footprints;
        private readonly Vector2 wind;
        private readonly int seed;
        public FixedTerrainRockContactField(IEnumerable<Footprint> rocks, Vector2 windDirection, int worldSeed)
        {
            if (rocks == null) throw new ArgumentNullException(nameof(rocks));
            if (!Finite(windDirection.x) || !Finite(windDirection.y) || windDirection.sqrMagnitude < .0001f)
                throw new ArgumentException("A finite nonzero wind direction is required.");
            footprints = new List<Footprint>(rocks).ToArray();
            foreach (var rock in footprints)
                if (!Finite(rock.Center.x) || !Finite(rock.Center.y) || !Finite(rock.HalfSize.x) || !Finite(rock.HalfSize.y)
                    || rock.HalfSize.x <= 0 || rock.HalfSize.y <= 0)
                    throw new ArgumentException("Invalid or default rock footprint.", nameof(rocks));
            wind = windDirection.normalized; seed = worldSeed;
        }
        public Sample Evaluate(Vector2 position, float slopeDegrees)
        {
            if (!Finite(position.x) || !Finite(position.y) || !Finite(slopeDegrees)) return default;
            float sand = 0f, gravel = 0f, shelter = 0f;
            var across = new Vector2(-wind.y, wind.x);
            foreach (var rock in footprints)
            {
                var delta = position - rock.Center;
                // Approximate the transformed bounds with an ellipse. Exact mesh contact is a later experiment.
                float edge = (new Vector2(delta.x / rock.HalfSize.x, delta.y / rock.HalfSize.y).magnitude - 1f)
                    * Mathf.Min(rock.HalfSize.x, rock.HalfSize.y);
                sand = Mathf.Max(sand, 1f - Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, edge) / 1.1f));
                gravel = Mathf.Max(gravel, 1f - Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, edge) / 2.5f));
                float radius = Mathf.Max(rock.HalfSize.x, rock.HalfSize.y);
                float downwind = Vector2.Dot(delta, wind);
                float lateral = Mathf.Abs(Vector2.Dot(delta, across));
                float wake = downwind <= 0f ? 0f : (1f - Mathf.SmoothStep(0f, 1f, downwind / (radius + 3f)))
                    * (1f - Mathf.SmoothStep(0f, 1f, lateral / (radius + .6f)));
                shelter = Mathf.Max(shelter, wake);
            }
            // Stable spatial grain; max composition is invariant to footprint enumeration order.
            float grain = .65f + .35f * Mathf.PerlinNoise(position.x * .7f + (seed & 1023), position.y * .7f + ((seed >> 10) & 1023));
            float slope = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((slopeDegrees - 15f) / 25f));
            sand = Mathf.Clamp01(Mathf.Max(sand * .85f, shelter) * grain * slope);
            gravel = Mathf.Clamp01(gravel * grain * (1f - sand * .6f));
            return new Sample(sand, gravel, .08f * shelter * grain * slope);
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
