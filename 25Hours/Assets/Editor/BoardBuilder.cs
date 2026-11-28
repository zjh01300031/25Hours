using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 生成「内层棋盘」白盒场景（Event_01）。
/// 场景里只有：相机 + EventSystem + 一个挂 PuzzleController 的空物体，
/// 棋盘界面由 PuzzleController 在运行时用 RuntimeUI 画出来。
/// </summary>
public static class BoardBuilder
{
    public const string ScenePath = "Assets/Scenes/Event_01.unity";
    public const string FontPath = "Assets/Fonts/SimHei.ttf";

    [MenuItem("工具/25小时/生成内层棋盘（白盒）", false, 5)]
    public static void BuildEvent01()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.055f, 0.07f, 1f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var boardGo = new GameObject("棋盘");
        var board = boardGo.AddComponent<PuzzleController>();
        board.width = 5;
        board.height = 5;
        board.blockedCells = new string[0];   // 设计稿出来后再填，或改用 layoutJson
        board.skills = new[]
        {
            new PuzzleSkill("微光", PuzzleShape.Single, 6),
            new PuzzleSkill("回响", PuzzleShape.Cross, 4),
            new PuzzleSkill("共振", PuzzleShape.Square3, 2),
        };
        board.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        board.avatarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/player.png");

        Directory.CreateDirectory("Assets/Scenes");
        BoardPreview.Build(board);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Board] 内层棋盘场景已生成：" + ScenePath);
    }

    /// <summary>一次性：生成场景 + 跑逻辑自测（给命令行用）。</summary>
    public static void BuildAndTest()
    {
        BuildEvent01();
        BoardLogicTest.RunTests();
    }
}
