using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gatefall.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Gatefall/Scenes/Gatefall.unity";

        static ProjectSetup() { EditorApplication.delayCall += InitializeIfNeeded; }

        private static void InitializeIfNeeded()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath)) Prepare();
        }

        [MenuItem("Gatefall/Prepare playable project")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Gatefall/Scenes");
            PlayerSettings.companyName = "Independent Studio";
            PlayerSettings.productName = "Gatefall";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 540; PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.prototype.gatefall");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.prototype.gatefall");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.stripEngineCode = false;
            AddRuntimeShaders();
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                // Runtime bootstrap creates the world. No scene/prefab wiring is required from the player.
                var marker = new GameObject("Gatefall — press Play");
                marker.AddComponent<GameRoot>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            if (!Application.isBatchMode && !EditorApplication.isPlaying)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("Gatefall ready. Open the Game tab and press Play.");
        }

        private static void AddRuntimeShaders()
        {
            var graphics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphics.Length == 0) return;
            var settings = new SerializedObject(graphics[0]);
            var shaders = settings.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null || !shaders.isArray) return;
            foreach (string name in new[] { "Standard", "GUI/Text Shader", "UI/Default" })
            {
                Shader shader = Shader.Find(name);
                if (shader == null) continue;
                bool exists = false;
                for (int i = 0; i < shaders.arraySize; i++)
                    if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) exists = true;
                if (exists) continue;
                int index = shaders.arraySize; shaders.InsertArrayElementAtIndex(index);
                shaders.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Gatefall/Build/Android test APK")]
        public static void BuildAndroidTest()
        {
            bool previous = EditorUserBuildSettings.buildAppBundle;
            try { EditorUserBuildSettings.buildAppBundle = false; Build(BuildTarget.Android, "Builds/Android/Gatefall.apk", false); }
            finally { EditorUserBuildSettings.buildAppBundle = previous; }
        }

        [MenuItem("Gatefall/Build/Android App Bundle")]
        public static void BuildAndroid()
        {
            bool previous = EditorUserBuildSettings.buildAppBundle;
            try { EditorUserBuildSettings.buildAppBundle = true; Build(BuildTarget.Android, "Builds/Android/Gatefall.aab", false); }
            finally { EditorUserBuildSettings.buildAppBundle = previous; }
        }

        [MenuItem("Gatefall/Build/iOS Xcode project")]
        public static void BuildIOS() { Build(BuildTarget.iOS, "Builds/iOS", false); }

        [MenuItem("Gatefall/Build/Mac prototype")]
        public static void BuildMac() { Build(BuildTarget.StandaloneOSX, "Builds/Mac/Gatefall.app", true); }

        private static void Build(BuildTarget target, string path, bool development)
        {
            Prepare();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroupFor(target), target))
                throw new InvalidOperationException("Install the " + target + " build support module first.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = path, target = target,
                options = development ? BuildOptions.Development : BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed: " + report.summary.result);
        }

        private static BuildTargetGroup BuildTargetGroupFor(BuildTarget target)
        {
            return target == BuildTarget.Android ? BuildTargetGroup.Android : target == BuildTarget.iOS ? BuildTargetGroup.iOS : BuildTargetGroup.Standalone;
        }

        public static void RunSmoke()
        {
            Prepare();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }
    }
}
