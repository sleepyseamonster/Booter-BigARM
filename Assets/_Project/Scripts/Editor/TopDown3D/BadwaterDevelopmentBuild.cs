using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BooterBigArm.Editor
{
    /// <summary>Builds only the bounded Badwater study for Player profiling.</summary>
    public static class BadwaterDevelopmentBuild
    {
        private const string ScenePath = "Assets/_Project/Scenes/TopDown3D/BadwaterFourSlices.unity";
        private const string OutputArgument = "-buildOutput";

        public static void BuildFromCli()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
                throw new InvalidOperationException("Badwater profiling build requires the active StandaloneOSX target.");

            string output = ReadArgument(Environment.GetCommandLineArgs(), OutputArgument);
            if (string.IsNullOrWhiteSpace(output))
                throw new ArgumentException("Pass -buildOutput with a new .app path outside Assets.");
            output = Path.GetFullPath(output);
            if (!output.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Badwater StandaloneOSX output must end in .app.");

            string assetsPath = Path.GetFullPath(Application.dataPath);
            if (output.StartsWith(assetsPath + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || Directory.Exists(output) || File.Exists(output))
                throw new IOException("Output must be a new path outside Assets; refusing to overwrite an existing build.");
            string projectRoot = Path.GetDirectoryName(assetsPath) ?? throw new IOException("Project root is unavailable.");
            if (!File.Exists(Path.Combine(projectRoot, ScenePath)))
                throw new FileNotFoundException("Badwater terrain scene is missing.", ScenePath);

            BadwaterPlayableSceneBuilder.ValidateFromCli();
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? throw new IOException("Output has no parent directory."));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            };
            Debug.Log($"Building Badwater Development Player: {ScenePath} -> {output}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Badwater build failed: {report.summary.result} ({report.summary.totalErrors} errors).");

            Debug.Log($"Badwater Development Player built: {output}; size={report.summary.totalSize} bytes; buildTime={report.summary.totalTime}.");
        }

        private static string ReadArgument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return string.Empty;
        }
    }
}
