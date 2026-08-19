using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CityBuilder.EditorTools
{
    /// <summary>
    /// Builds the Android APK from the command line, the same way SetupProject regenerates the
    /// project from the command line -- so a build is a command with an output anyone can read,
    /// not a sequence of clicks in a dialog that nobody can review afterwards.
    ///
    /// Two flavours, and the difference matters for why this exists at all:
    ///
    /// - **Plain** is what the game actually performs like. Use it to judge frame rate, heat and
    ///   battery, because a development build is measurably slower than the thing players run.
    /// - **`-development`** carries the profiler and will connect back to the editor over the
    ///   network. This is the only way to get real render numbers -- draw calls, SetPass calls,
    ///   GPU time -- since a -nographics batchmode run measures none of it, and a desktop GPU is
    ///   the wrong shape to extrapolate from anyway (mobile GPUs are tile-based).
    ///
    /// Deliberately NOT wired into SetupProject: regenerating the project is something every test
    /// run does, and a 10-20 minute IL2CPP compile has no business happening on that path.
    /// </summary>
    public static class BuildAndroid
    {
        private const string OutputFolder = "Builds/Android";

        /// <summary>Entry point for -executeMethod. Pass -development on the same command line for the profiler-carrying build.</summary>
        public static void Run()
        {
            Build(HasFlag("-development"));
        }

        /// <summary>The development build, for anyone who would rather pass a method name than a flag.</summary>
        public static void RunDevelopment()
        {
            Build(true);
        }

        private static void Build(bool development)
        {
            var scenes = ScenePaths();
            if (scenes.Length == 0)
            {
                Fail("No scenes are enabled in EditorBuildSettings -- run SetupProject first.");
                return;
            }

            Directory.CreateDirectory(OutputFolder);
            var apkPath = Path.Combine(OutputFolder, development ? "CityBuilding-dev.apk" : "CityBuilding.apk");

            var options = BuildOptions.None;
            if (development)
            {
                // ConnectWithProfiler on its own is not enough: without Development the player has
                // no profiler to connect. Deep profiling is deliberately NOT enabled -- it distorts
                // per-frame timings far more than it explains them.
                options |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = options,
            };

            Debug.Log($"[BuildAndroid] Building {(development ? "DEVELOPMENT" : "release")} APK to {apkPath}");
            Debug.Log($"[BuildAndroid] identifier={PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)} " +
                      $"backend={PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)} " +
                      $"architectures={PlayerSettings.Android.targetArchitectures} minSdk={PlayerSettings.Android.minSdkVersion}");

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                Fail($"Build {summary.result} with {summary.totalErrors} error(s) after {summary.totalTime}.");
                return;
            }

            // The APK on disk, not summary.totalSize -- on Android that reports the UNCOMPRESSED
            // payload and reads about twenty times larger than the file anyone actually installs
            // (692 MB against 32,5 MB on the first build).
            var apkFile = new FileInfo(apkPath);
            var megabytes = apkFile.Exists ? apkFile.Length / 1024f / 1024f : 0f;
            Debug.Log($"[BuildAndroid] OK: {apkPath}, {megabytes:F1} MB, took {summary.totalTime}.");
            EditorApplication.Exit(0);
        }

        private static string[] ScenePaths()
        {
            var enabled = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene != null && scene.enabled) enabled.Add(scene.path);
            }
            return enabled.ToArray();
        }

        private static bool HasFlag(string flag)
        {
            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (string.Equals(argument, flag, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>
        /// Exits non-zero so a shell script can tell a failed build from a successful one. Unity's
        /// own exit code is 0 for a failed BuildPlayer, which is exactly the kind of green that
        /// means nothing.
        /// </summary>
        private static void Fail(string message)
        {
            Debug.LogError("[BuildAndroid] " + message);
            EditorApplication.Exit(1);
        }
    }
}
