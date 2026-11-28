using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 搭「事件一 = 探索区 + 里面的三个棋盘」：
/// - Event_01：俯视探索区（白盒），地上摆三个「棋盘入口」，投人偶进去分别去 Board_01/02/03
/// - Board_01/02/03：三块大小不同的棋盘，打完自动回 Event_01
/// </summary>
public static class ExploreBuilder
{
    const string SceneDir = "Assets/Scenes";
    const string FontPath = "Assets/Fonts/SimHei.ttf";
    const string PlayerArt = "Assets/Art/Characters/player.png";

    [MenuItem("工具/25小时/生成事件一（探索区 + 三个棋盘）", false, 16)]
    public static void BuildAll()
    {
        var boards = new[] { BuildBoard("Board_01", 5, 5, "board_01", "第一个棋盘"), 
                             BuildBoard("Board_02", 6, 6, "board_02", "第二个棋盘"), 
                             BuildBoard("Board_03", 7, 5, "board_03", "第三个棋盘") };
        BuildExplore(boards);
        SetupBuildSettings();
        Debug.Log("[探索区] Event_01 + Board_01/02/03 已生成");
    }

    // ---------------- 棋盘 ----------------

    static string BuildBoard(string sceneName, int w, int h, string eventId, string label)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera", typeof(Camera));
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.05f, 0.055f, 0.07f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.tag = "MainCamera";

        var go = new GameObject("棋盘");
        var board = go.AddComponent<PuzzleController>();
        board.width = w;
        board.height = h;
        board.blockedCells = new string[0];
        board.SetEventId(eventId);
        board.returnScene = "Event_01";
        board.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        board.avatarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerArt);

        var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));

        BoardPreview.Build(board);

        var path = SceneDir + "/" + sceneName + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        return sceneName;
    }

    // ---------------- 探索区 ----------------

    static void BuildExplore(string[] boardScenes)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(CameraRig2D));
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = new Color(0.05f, 0.055f, 0.07f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.tag = "MainCamera";

        var head = new GameObject("GameProgress", typeof(GameProgress));
        var boot = new GameObject("AppBoot", typeof(AppBoot));

        // 地面（纯装饰，不加碰撞，只让画面不是全黑）
        var floor = new GameObject("地面");
        var floorSr = floor.AddComponent<SpriteRenderer>();
        floorSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        floorSr.drawMode = SpriteDrawMode.Sliced;
        floorSr.size = new Vector2(30f, 16f);
        floorSr.color = new Color(0.17f, 0.18f, 0.21f, 1f);
        floorSr.sortingOrder = -10;

        // 四面墙（碰撞）
        Wall("墙_左", new Vector2(-11f, 0f), new Vector2(0.6f, 16f));
        Wall("墙_右", new Vector2(11f, 0f), new Vector2(0.6f, 16f));
        Wall("墙_上", new Vector2(0f, 6f), new Vector2(22f, 0.6f));
        Wall("墙_下", new Vector2(0f, -6f), new Vector2(22f, 0.6f));

        // 玩家
        var player = new GameObject("玩家_俯视", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(TopDownPlayer));
        player.transform.position = new Vector3(0f, -3f, 0f);
        var rb = player.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        var col = player.GetComponent<CircleCollider2D>();
        col.radius = 0.32f;

        var sr = player.GetComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerArt);
        sr.sortingOrder = 5;
        if (sr.sprite == null) sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var topdown = player.GetComponent<TopDownPlayer>();
        topdown.spriteRenderer = sr;

        var rig = camGo.GetComponent<CameraRig2D>();
        rig.target = player.transform;
        rig.useBounds = false;

        // 三个棋盘入口
        var positions = new[] { new Vector3(-6f, 1f, 0f), new Vector3(0f, 3f, 0f), new Vector3(6f, 1f, 0f) };
        var titles = new[] { "第一个棋盘", "第二个棋盘", "第三个棋盘" };

        for (int i = 0; i < boardScenes.Length && i < positions.Length; i++)
        {
            var gateGo = new GameObject("棋盘入口_" + (i + 1));
            gateGo.transform.position = positions[i];
            gateGo.layer = LayerMask.NameToLayer("Interactable");

            var gsr = gateGo.AddComponent<SpriteRenderer>();
            gsr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            gsr.drawMode = SpriteDrawMode.Sliced;
            gsr.size = new Vector2(1.4f, 1.8f);
            gsr.color = new Color(0.55f, 0.78f, 0.95f, 1f);
            gsr.sortingOrder = 3;

            var gcol = gateGo.AddComponent<CircleCollider2D>();
            gcol.isTrigger = true;
            gcol.radius = 0.9f;

            var gate = gateGo.AddComponent<BoardGate>();
            gate.gateTitle = titles[i];
            gate.introText = "一座矮台，台面上有个凹槽，正好能嵌进一个人偶。";
            gate.requiredItemId = "item_doll";
            gate.requiredItemName = "人偶";
            gate.boardScene = boardScenes[i];
            gate.triggerRadius = 1.8f;
        }

        // 收尾：三块棋盘都打完就回第 0 层
        var flowGo = new GameObject("收尾", typeof(LevelFlow));
        var flow = flowGo.GetComponent<LevelFlow>();
        flow.returnScene = "Shelter_L0";
        flow.levelId = "event_01";
        flow.requiredEvents = new[] { "board_01", "board_02", "board_03" };
        flow.returnWhenAllDoneOnStart = true;

        var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));

        EditorSceneManager.SaveScene(scene, SceneDir + "/Event_01.unity");
    }

    static void Wall(string name, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name, typeof(BoxCollider2D));
        go.transform.position = center;
        go.GetComponent<BoxCollider2D>().size = size;
        go.layer = LayerMask.NameToLayer("Ground");
    }

    static void SetupBuildSettings()
    {
        var order = new[] { "MainMenu", "Shelter_L0", "Event_01", "Board_01", "Board_02", "Board_03" };
        var list = new List<EditorBuildSettingsScene>();
        foreach (var name in order)
        {
            var path = SceneDir + "/" + name + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                list.Add(new EditorBuildSettingsScene(path, true));
        }

        // 其余已经存在、但不在表里的场景补在后面，别弄丢别人的场景
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!list.Exists(x => x.path == s.path) && !s.path.Contains("Board_0") && !s.path.Contains("Event_0"))
                list.Add(s);
        }

        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[探索区] Build Settings：" + string.Join(" / ", list.ConvertAll(x => System.IO.Path.GetFileNameWithoutExtension(x.path))));
    }
}