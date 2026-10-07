using System.Collections.Generic;

namespace BooterBigArm.Runtime
{
    public static class RuleGroundMasks
    {
        public static readonly int[] CanonicalMasks = BuildCanonicalMasks();

        public static bool IsCanonicalTerrainMask(int mask)
        {
            return CanonicalMasksIndexLookup.Contains(mask);
        }

        private static readonly HashSet<int> CanonicalMasksIndexLookup = new HashSet<int>(CanonicalMasks);

        private static int[] BuildCanonicalMasks()
        {
            var masks = new List<int>(47);
            for (var mask = 0; mask < 256; mask++)
            {
                if (IsValidTerrainMask(mask))
                {
                    masks.Add(mask);
                }
            }

            return masks.ToArray();
        }

        private static bool IsValidTerrainMask(int mask)
        {
            var north = (mask & (1 << 1)) != 0;
            var west = (mask & (1 << 3)) != 0;
            var east = (mask & (1 << 4)) != 0;
            var south = (mask & (1 << 6)) != 0;

            if ((mask & 1) != 0 && !(north && west))
            {
                return false;
            }

            if ((mask & (1 << 2)) != 0 && !(north && east))
            {
                return false;
            }

            if ((mask & (1 << 5)) != 0 && !(west && south))
            {
                return false;
            }

            if ((mask & (1 << 7)) != 0 && !(east && south))
            {
                return false;
            }

            return true;
        }
    }
}
