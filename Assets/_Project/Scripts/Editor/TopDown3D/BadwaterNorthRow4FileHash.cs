using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;

namespace BooterBigArm.Editor
{
    /// <summary>Fresh streamed SHA-256 for row four; no metadata or timestamp hash cache.</summary>
    public static class BadwaterNorthRow4FileHash
    {
        public static string Compute(string path)
        {
#if UNITY_EDITOR_WIN
            return WindowsHash(path);
#else
            return BadwaterTerrainExpansionSource.Hash(path);
#endif
        }

#if UNITY_EDITOR_WIN
        private static readonly object Gate = new object();
        private static IntPtr algorithm;
        private static int objectLength;
        [ThreadStatic] private static byte[] buffer;

        static BadwaterNorthRow4FileHash()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CloseProvider;
            EditorApplication.quitting += CloseProvider;
        }

        private static void Check(int status)
        {
            if (status < 0) throw new System.Security.Cryptography.CryptographicException("Windows SHA-256 failed: " + status.ToString("X8"));
        }

        private static void Initialize()
        {
            if (algorithm != IntPtr.Zero) return;
            Check(BCryptOpenAlgorithmProvider(out algorithm, "SHA256", null, 0));
            try
            {
                byte[] size = new byte[4];
                Check(BCryptGetProperty(algorithm, "ObjectLength", size, size.Length, out int actual, 0));
                if (actual != 4 || (objectLength = BitConverter.ToInt32(size, 0)) <= 0)
                    throw new InvalidDataException("Invalid SHA-256 object length.");
            }
            catch { BCryptCloseAlgorithmProvider(algorithm, 0); algorithm = IntPtr.Zero; throw; }
        }

        private static string WindowsHash(string path)
        {
            lock (Gate)
            {
                Initialize();
                IntPtr state = Marshal.AllocHGlobal(objectLength), hash = IntPtr.Zero;
                try
                {
                    Check(BCryptCreateHash(algorithm, out hash, state, objectLength, null, 0, 0));
                    if (buffer == null) buffer = new byte[128 * 1024];
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, FileOptions.SequentialScan))
                    {
                        int count;
                        while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                            Check(BCryptHashData(hash, buffer, count, 0));
                    }
                    byte[] digest = new byte[32];
                    Check(BCryptFinishHash(hash, digest, digest.Length, 0));
                    return BitConverter.ToString(digest).Replace("-", "").ToLowerInvariant();
                }
                finally
                {
                    if (hash != IntPtr.Zero) BCryptDestroyHash(hash);
                    Marshal.FreeHGlobal(state);
                }
            }
        }

        private static void CloseProvider()
        {
            lock (Gate)
            {
                if (algorithm != IntPtr.Zero) { BCryptCloseAlgorithmProvider(algorithm, 0); algorithm = IntPtr.Zero; }
            }
        }

        [DllImport("bcrypt.dll", ExactSpelling = true, CharSet = CharSet.Unicode)] private static extern int BCryptOpenAlgorithmProvider(out IntPtr handle, string name, string implementation, int flags);
        [DllImport("bcrypt.dll", ExactSpelling = true, CharSet = CharSet.Unicode)] private static extern int BCryptGetProperty(IntPtr handle, string name, byte[] output, int length, out int actual, int flags);
        [DllImport("bcrypt.dll", ExactSpelling = true)] private static extern int BCryptCreateHash(IntPtr algorithm, out IntPtr hash, IntPtr state, int stateLength, byte[] secret, int secretLength, int flags);
        [DllImport("bcrypt.dll", ExactSpelling = true)] private static extern int BCryptHashData(IntPtr hash, byte[] data, int length, int flags);
        [DllImport("bcrypt.dll", ExactSpelling = true)] private static extern int BCryptFinishHash(IntPtr hash, byte[] output, int length, int flags);
        [DllImport("bcrypt.dll", ExactSpelling = true)] private static extern int BCryptDestroyHash(IntPtr hash);
        [DllImport("bcrypt.dll", ExactSpelling = true)] private static extern int BCryptCloseAlgorithmProvider(IntPtr algorithm, int flags);
#endif
    }
}
