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
        private const string ScenePath = "Assets/_Project/Scenes/Production/GreaterWasteland.unity";
        private const string OutputArgument = "-buildOutput";

        public static void BuildFromCli()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string output = ReadArgument(Environment.GetCommandLineArgs(), OutputArgument);
            string assetsPath = Path.GetFullPath(Application.dataPath);
            output = ValidateOutputPath(target, output, assetsPath);
            string projectRoot = Path.GetDirectoryName(assetsPath) ?? throw new IOException("Project root is unavailable.");
            if (!File.Exists(Path.Combine(projectRoot, ScenePath)))
                throw new FileNotFoundException("Badwater terrain scene is missing.", ScenePath);

            BadwaterPlayableSceneBuilder.ValidateFromCli();
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? throw new IOException("Output has no parent directory."));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = target,
                options = BuildOptions.Development
            };
            Debug.Log($"Building Badwater Development Player: {ScenePath} -> {output}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Badwater build failed: {report.summary.result} ({report.summary.totalErrors} errors).");

            Debug.Log($"Badwater Development Player built: {output}; size={report.summary.totalSize} bytes; buildTime={report.summary.totalTime}.");
        }

        internal static string ValidateOutputPath(BuildTarget target, string output, string assetsPath)
        {
            string extension = target switch
            {
                BuildTarget.StandaloneOSX => ".app",
                BuildTarget.StandaloneWindows64 => ".exe",
                _ => throw new InvalidOperationException("Badwater profiling supports active StandaloneOSX or StandaloneWindows64 targets.")
            };
            if (string.IsNullOrWhiteSpace(output))
                throw new ArgumentException($"Pass -buildOutput with a new {extension} path outside Assets.");
            output = Path.GetFullPath(output);
            assetsPath = Path.GetFullPath(assetsPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!output.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Badwater {target} output must end in {extension}.");
            if (output.StartsWith(assetsPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || Directory.Exists(output) || File.Exists(output))
                throw new IOException("Output must be a new path outside Assets; refusing to overwrite an existing build.");
            // Windows emits an executable, data folder and support files. Require
            // a new parent directory so none of those siblings can be overwritten.
            string parent = Path.GetDirectoryName(output) ?? throw new IOException("Output has no parent directory.");
            if (target == BuildTarget.StandaloneWindows64 && (Directory.Exists(parent) || File.Exists(parent)))
                throw new IOException("Windows profiling output requires a new parent directory for all build files.");
            return output;
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
