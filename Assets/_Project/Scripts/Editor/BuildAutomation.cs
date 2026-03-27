using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BooterBigArm.Editor
{
    public static class BuildAutomation
    {
        private const string DefaultBuildName = "BooterBigArm";

        [MenuItem("Booter & BigARM/Build/Build Active Target")]
        public static void BuildActiveTargetMenu()
        {
            BuildFromCli();
        }

        [MenuItem("Booter & BigARM/Build/Log Active Target")]
        public static void LogActiveTargetMenu()
        {
            Debug.Log($"Active build target: {EditorUserBuildSettings.activeBuildTarget}");
        }

        public static void BuildFromCli()
        {
            BuildPlayer();
        }

        public static void BuildPlayer()
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes found in Build Settings.");
            }

            var args = Environment.GetCommandLineArgs();
            var outputPath = GetArgumentValue(args, "-buildOutput");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = ResolveDefaultOutputPath(buildTarget);
            }

            var development = HasFlag(args, "-development");

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? outputPath);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = buildTarget,
                options = development ? BuildOptions.Development : BuildOptions.None
            };

            Debug.Log($"Starting build for {buildTarget} -> {outputPath}");

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Build failed for {buildTarget}: {report.summary.result} ({report.summary.totalErrors} errors)");
            }

            Debug.Log($"Build succeeded: {outputPath}");
        }

        private static string ResolveDefaultOutputPath(BuildTarget buildTarget)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var buildRoot = Path.Combine(projectRoot, "Builds");
            var targetRoot = Path.Combine(buildRoot, buildTarget.ToString());
            Directory.CreateDirectory(targetRoot);

            return buildTarget switch
            {
                BuildTarget.StandaloneOSX => Path.Combine(targetRoot, $"{DefaultBuildName}.app"),
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 =>
                    Path.Combine(targetRoot, $"{DefaultBuildName}.exe"),
                BuildTarget.StandaloneLinux64 => Path.Combine(targetRoot, DefaultBuildName),
                _ => Path.Combine(targetRoot, DefaultBuildName)
            };
        }

        private static string GetArgumentValue(string[] args, string key)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return string.Empty;
        }

        private static bool HasFlag(string[] args, string key)
        {
            return args.Any(arg => string.Equals(arg, key, StringComparison.OrdinalIgnoreCase));
        }
    }
}
