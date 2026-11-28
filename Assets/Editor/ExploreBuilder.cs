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

        // 背景（一整块暗色，铺满视野）
        var bg = new GameObject("背景");
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bgSr.drawMode = SpriteDrawMode.Sliced;
        bgSr.size = new Vector2(40f, 22f);
        bgSr.color = new Color(0.11f, 0.12f, 0.14f, 1f);
        bgSr.sortingOrder = -20;

        // 地板：一格一格的瓷砖，从 x=-11 铺到 11，站在上面走
        Box2D floorCol = new Box2D(new Vector2(0f, -3.5f), new Vector2(22f, 1f));
        Wall("地板碰撞", floorCol.center, floorCol.size);

        const float Tile = 1f;
        int count = 22;
        for (int i = 0; i < count; i++)
        {
            var tile = new GameObject("地砖_" + i);
            tile.transform.position = new Vector3(-11f + Tile * 0.5f + i * Tile, -3.5f, 0f);
            var tsr = tile.AddComponent<SpriteRenderer>();
            tsr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            tsr.drawMode = SpriteDrawMode.Sliced;
            tsr.size = new Vector2(Tile * 0.97f, 0.92f);
            tsr.color = (i % 2 == 0) ? new Color(0.28f, 0.30f, 0.34f, 1f) : new Color(0.24f, 0.26f, 0.30f, 1f);
            tsr.sortingOrder = -10;
        }

        // 地板下的裙板，让画面有厚度
        var skirt = new GameObject("地板裙板");
        var skirtSr = skirt.AddComponent<SpriteRenderer>();
        skirtSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        skirtSr.drawMode = SpriteDrawMode.Sliced;
        skirtSr.size = new Vector2(22f, 0.5f);
        skirtSr.color = new Color(0.16f, 0.17f, 0.20f, 1f);
        skirtSr.sortingOrder = -11;
        skirt.transform.position = new Vector3(0f, -4.25f, 0f);

        // 四面墙（碰撞）：左右到地板，上面封顶，下面兜底
        Wall("墙_左", new Vector2(-11.2f, 1.5f), new Vector2(0.5f, 11f));
        Wall("墙_右", new Vector2(11.2f, 1.5f), new Vector2(0.5f, 11f));
        Wall("墙_上", new Vector2(0f, 7f), new Vector2(23f, 0.5f));
        Wall("墙_下", new Vector2(0f, -5f), new Vector2(23f, 0.5f));

        // 玩家：站在地板上（地板顶面 y = -3）
        var player = new GameObject("玩家_俯视", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(TopDownPlayer));
        player.transform.position = new Vector3(0f, -2.68f, 0f);
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
        var positions = new[] { new Vector3(-6f, -2.1f, 0f), new Vector3(0f, -2.1f, 0f), new Vector3(6f, -2.1f, 0f) };
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

        // 背包：Q 打开，里面用「对应的收集物」直接开棋盘
        var bagGo = new GameObject("背包", typeof(InventoryUI));
        var bag = bagGo.GetComponent<InventoryUI>();
        bag.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        bag.boards = new[]
        {
            new InventoryUI.BoardLink { itemId = "item_01", sceneName = "Board_01", label = "带着它走进第一块棋盘…" },
            new InventoryUI.BoardLink { itemId = "item_02", sceneName = "Board_02", label = "带着它走进第二块棋盘…" },
            new InventoryUI.BoardLink { itemId = "item_03", sceneName = "Board_03", label = "带着它走进第三块棋盘…" },
        };

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

    struct Box2D
    {
        public Vector2 center;
        public Vector2 size;
        public Box2D(Vector2 c, Vector2 s) { center = c; size = s; }
    }

    static void Wall(string name, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name, typeof(BoxCollider2D));
        go.transform.position = center;
        go.GetComponent<BoxCollider2D>().size = size;
        go.layer = LayerMask.NameToLayer("Ground");
    }

    [InitializeOnLoadMethod]
    static void AutoRunIfRequested()
    {
        const string flag = "Temp/explore-build.flag";
        if (!System.IO.File.Exists(flag)) return;
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
        System.IO.File.Delete(flag);
        EditorApplication.delayCall += () =>
        {
            BuildAll();
            Debug.Log("[探索区] flag 触发的重建完成");
        };
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