using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// L1-1 关卡原型的一键构建脚本。
/// 命令行：Unity.exe -batchmode -quit -projectPath &lt;工程&gt; -executeMethod LevelBuilder.BuildWindows -logFile &lt;日志&gt;
/// </summary>
public static class LevelBuilder
{
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = SceneDir + "/LevelL1_1.unity";
    const string BuildPath = "D:/Unity2D/Build/25Hours/L1-1.exe";

    [MenuItem("Tools/25Hours/Build L1-1 Scene")]
    public static void BuildScene()
    {
        Directory.CreateDirectory(SceneDir);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var go = new GameObject("Level");
        go.AddComponent<LevelL1_1>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.productName = "25 Hours - L1-1";
        PlayerSettings.companyName = "Team Nine";
        PlayerSettings.defaultScreenWidth = 1100;
        PlayerSettings.defaultScreenHeight = 700;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;

        Debug.Log("[Level] scene ready: " + ScenePath);
    }

    [MenuItem("Tools/25Hours/Build L1-1 exe")]
    public static void BuildWindows()
    {
        BuildScene();
        Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = BuildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[Level] build result: " + report.summary.result + ", size " + report.summary.totalSize + " bytes");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
        EditorApplication.Exit(0);
    }

    [MenuItem("Tools/25Hours/Open L1-1 and Play")]
    public static void OpenAndPlay()
    {
        if (!File.Exists(ScenePath)) BuildScene();
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.isPlaying = true;
    }
}
