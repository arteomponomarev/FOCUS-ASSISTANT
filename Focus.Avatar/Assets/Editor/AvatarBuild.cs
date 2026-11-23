using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Focus.Avatar.Editor
{
    public static class AvatarBuild
    {
        [MenuItem("Focus Assistant/Build Windows x64")]
        public static void BuildWindows()
        {
            const BuildTarget target = BuildTarget.StandaloneWindows64;
            const string scene = "Assets/Scenes/Main.unity";
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
                throw new BuildFailedException("Install Windows Build Support for this Unity Editor.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) == null)
                throw new BuildFailedException("The desktop companion scene is missing.");
            if (AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/OrbitShell.png") == null)
                throw new BuildFailedException("The Orbit character texture is missing.");

            PlayerSettings.companyName = "Focus Assistant";
            PlayerSettings.productName = "Focus Assistant Avatar";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "org.focusassistant.avatar");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
            PlayerSettings.SetGraphicsAPIs(target, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.useFlipModelSwapchain = false;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 400;
            PlayerSettings.defaultScreenHeight = 478;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;

            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../artifacts/avatar-win64/FocusAvatar.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = output,
                target = target
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Windows Avatar build {report.summary.result}.");
            Debug.Log($"Avatar Windows build succeeded ({report.summary.totalSize} bytes).");
        }
    }
}
