using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 《第25小时》棋盘原型的一键构建脚本。
/// 菜单：工具 → 生成棋盘场景并打包
/// 命令行：Unity.exe -batchmode -quit -projectPath &lt;工程&gt; -executeMethod PrototypeBuilder.BuildWindows -logFile &lt;日志&gt;
/// </summary>
public static class PrototypeBuilder
{
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = SceneDir + "/MapBoard.unity";
    const string BuildPath = "D:/Unity2D/Build/25Hours/25Hours.exe";

    [MenuItem("工具/生成棋盘场景")]
    public static void BuildScene()
    {
        Directory.CreateDirectory(SceneDir);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
        camGo.tag = "MainCamera";

        var go = new GameObject("Board");
        go.AddComponent<TwentyFiveHours>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.productName = "25 Hours - Map";
        PlayerSettings.companyName = "Team Nine";
        PlayerSettings.defaultScreenWidth = 1000;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;

        Debug.Log("[Proto] 场景已生成：" + ScenePath);
    }

    [MenuItem("工具/生成棋盘场景并打包")]
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
        Debug.Log("[Proto] 构建结果：" + report.summary.result + "，大小 " + report.summary.totalSize + " 字节");
    }

    [MenuItem("工具/打开棋盘场景并运行")]
    public static void OpenAndPlay()
    {
        if (!File.Exists(ScenePath)) BuildScene();
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.isPlaying = true;
    }
}
