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
    /// - **`-store`** is the Google Play upload (Design/RELEASE_PLAN.md): an App Bundle (.aab), CB_RELEASE, the game's
    ///   data split into an install-time asset pack (Play Asset Delivery -- the user chose it, 2026-11-24: the build is
    ///   ~227 MB and Play caps the base download at 200 MB), signed with the UPLOAD KEY from environment variables
    ///   CB_KEYSTORE (path), CB_KEYSTORE_PASS, CB_KEY_ALIAS, CB_KEY_PASS -- never from the repo, which is public --
    ///   and versionCode from `-versionCode N` (the wrapper passes the git commit count: always growing).
    ///
    /// Deliberately NOT wired into SetupProject: regenerating the project is something every test
    /// run does, and a 10-20 minute IL2CPP compile has no business happening on that path.
    /// </summary>
    public static class BuildAndroid
    {
        private const string OutputFolder = "Builds/Android";
        private const string AppName = "CityCraft";

        /// <summary>Entry point for -executeMethod. Pass -development on the same command line for the profiler-carrying build.</summary>
        public static void Run()
        {
            Build(HasFlag("-development") && !HasFlag("-store"));
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
            var store = HasFlag("-store");
            var apkPath = Path.Combine(OutputFolder, store ? "CityCraft.aab" : development ? "CityBuilding-dev.apk" : "CityBuilding.apk");
            // the store upload: a bundle with the data in an install-time asset pack, signed with the upload key
            EditorUserBuildSettings.buildAppBundle = store;
            PlayerSettings.Android.splitApplicationBinary = store;
            var keystoreBefore = PlayerSettings.Android.useCustomKeystore;
            if (store && !UseUploadKey()) return;
            if (store)
            {
                var code = IntArg("-versionCode");
                if (code <= 0)
                {
                    Fail("-store needs -versionCode N (a number larger than every upload before it).");
                    return;
                }
                PlayerSettings.Android.bundleVersionCode = code;
            }

            // Telegram takes no file over 50 MB and the APK had grown past it (52,2 MB on 2026-10-16).
            // Unity stores its data in the APK uncompressed (so it can be read in place), which makes
            // how the data is packed decide most of the size: LZ4HC packs it much tighter than the
            // default LZ4 for the same load speed. IL2CPP generating for size shrinks the native
            // library at a cost in speed too small to see in this game.
            var options = BuildOptions.CompressWithLz4HC;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, Il2CppCodeGeneration.OptimizeSize);
            // vertex channels no shader in the build reads (tangents: there are no normal maps) stay out
            PlayerSettings.stripUnusedMeshComponents = true;
            // code nothing calls stays out of the native library; the game reaches nothing by
            // reflection (checked 2026-10-22), which is what Medium could break
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
            if (development)
            {
                // ConnectWithProfiler on its own is not enough: without Development the player has
                // no profiler to connect. Deep profiling is deliberately NOT enabled -- it distorts
                // per-frame timings far more than it explains them.
                options |= BuildOptions.Development | BuildOptions.ConnectWithProfiler;
            }
            // the CPU/GPU frame times Cheats/PerfProbe logs (FrameTimingManager) -- the development build only
            PlayerSettings.enableFrameTimingStats = development;
            // no Vulkan pre-transform: on the Samsung A71 (Adreno 618) the frame came out squeezed into the left part of
            // the landscape screen, the rest left black (the user, 2026-11-18)
            PlayerSettings.vulkanEnablePreTransform = false;

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = options,
                // a release for strangers (-release) leaves the cheat window out: CheatsPanel
                // compiles to an empty component under CB_RELEASE (the user, 2026-10-31)
                extraScriptingDefines = HasFlag("-release") || store ? new[] { "CB_RELEASE" } : null,
            };

            Debug.Log($"[BuildAndroid] Building {(development ? "DEVELOPMENT" : "release")} APK to {apkPath}");
            Debug.Log($"[BuildAndroid] identifier={PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)} " +
                      $"backend={PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)} " +
                      $"architectures={PlayerSettings.Android.targetArchitectures} minSdk={PlayerSettings.Android.minSdkVersion}");

            // The name under the icon on the phone is the product name (the user, 2026-11-18: «название игры
            // должно быть CityCraft у апк»). Only for the APK: the Editor keeps «CityBuilding», because the
            // Editor's PlayerPrefs and persistentDataPath are keyed by it (saves, account switcher).
            var editorName = PlayerSettings.productName;
            PlayerSettings.productName = AppName;
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            }
            finally
            {
                PlayerSettings.productName = editorName;
                // the Editor and the everyday APKs keep the debug key and a plain APK
                PlayerSettings.Android.useCustomKeystore = keystoreBefore;
                PlayerSettings.Android.keystorePass = PlayerSettings.Android.keyaliasPass = "";
                EditorUserBuildSettings.buildAppBundle = false;
                PlayerSettings.Android.splitApplicationBinary = false;
            }
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

        /// <summary>The upload key from the environment (never stored in the project: the repo is public).</summary>
        private static bool UseUploadKey()
        {
            var path = Environment.GetEnvironmentVariable("CB_KEYSTORE");
            var storePass = Environment.GetEnvironmentVariable("CB_KEYSTORE_PASS");
            var alias = Environment.GetEnvironmentVariable("CB_KEY_ALIAS");
            var keyPass = Environment.GetEnvironmentVariable("CB_KEY_PASS");
            if (string.IsNullOrEmpty(path) || !File.Exists(path) || string.IsNullOrEmpty(storePass) || string.IsNullOrEmpty(alias))
            {
                Fail("-store needs the upload key: set CB_KEYSTORE (an existing .keystore file), CB_KEYSTORE_PASS, CB_KEY_ALIAS and CB_KEY_PASS.");
                return false;
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = path;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasName = alias;
            PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(keyPass) ? storePass : keyPass;
            return true;
        }

        private static int IntArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && int.TryParse(args[i + 1], out var v)) return v;
            return 0;
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
