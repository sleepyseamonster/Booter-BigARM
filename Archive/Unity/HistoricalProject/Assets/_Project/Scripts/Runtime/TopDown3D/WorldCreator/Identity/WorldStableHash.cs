using System;

namespace BooterBigArm.TopDown3D.WorldCreator
{
    internal struct WorldStableHashBuilder
    {
        private const ulong OffsetA = 14695981039346656037UL;
        private const ulong OffsetB = 7809847782465536322UL;
        private const ulong PrimeA = 1099511628211UL;
        private const ulong PrimeB = 14029467366897019727UL;

        private ulong stateA;
        private ulong stateB;
        private ulong byteCount;

        public WorldStableHashBuilder(string domainTag)
        {
            stateA = OffsetA;
            stateB = OffsetB;
            byteCount = 0;
            Append(domainTag);
        }

        public void Append(byte value)
        {
            unchecked
            {
                stateA = (stateA ^ value) * PrimeA;
                stateB = (stateB ^ (value + 0x9dUL)) * PrimeB;
                byteCount++;
            }
        }

        public void Append(int value)
        {
            Append(unchecked((uint)value));
        }

        public void Append(uint value)
        {
            Append((byte)value);
            Append((byte)(value >> 8));
            Append((byte)(value >> 16));
            Append((byte)(value >> 24));
        }

        public void Append(long value)
        {
            Append(unchecked((ulong)value));
        }

        public void Append(ulong value)
        {
            Append((byte)value);
            Append((byte)(value >> 8));
            Append((byte)(value >> 16));
            Append((byte)(value >> 24));
            Append((byte)(value >> 32));
            Append((byte)(value >> 40));
            Append((byte)(value >> 48));
            Append((byte)(value >> 56));
        }

        public void Append(string value)
        {
            if (value == null)
            {
                Append(-1);
                return;
            }

            Append(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                Append((byte)character);
                Append((byte)(character >> 8));
            }
        }

        public void Finish128(out ulong high, out ulong low)
        {
            high = Avalanche(stateA ^ RotateLeft(stateB, 17) ^ byteCount);
            low = Avalanche(stateB ^ RotateLeft(stateA, 41) ^ (byteCount * PrimeA));
            if (high == 0UL && low == 0UL)
            {
                low = 1UL;
            }
        }

        public ulong Finish64()
        {
            Finish128(out var high, out var low);
            return Avalanche(high ^ RotateLeft(low, 29));
        }

        public int FinishHashCode()
        {
            var value = Finish64();
            return unchecked((int)(value ^ (value >> 32)));
        }

        private static ulong RotateLeft(ulong value, int distance)
        {
            return (value << distance) | (value >> (64 - distance));
        }

        private static ulong Avalanche(ulong value)
        {
            unchecked
            {
                value ^= value >> 30;
                value *= 0xbf58476d1ce4e5b9UL;
                value ^= value >> 27;
                value *= 0x94d049bb133111ebUL;
                value ^= value >> 31;
                return value;
            }
        }
    }

    internal static class WorldStableText
    {
        public static string Require(string value, string parameterName, int maximumLength = 512)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("A stable value is required.", parameterName);
            }

            if (value.Length > maximumLength)
            {
                throw new ArgumentException($"Stable values cannot exceed {maximumLength} characters.", parameterName);
            }

            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Stable values cannot have leading or trailing whitespace.", parameterName);
            }

            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                {
                    throw new ArgumentException("Stable values cannot contain control characters.", parameterName);
                }
            }

            return value;
        }
    }
}
