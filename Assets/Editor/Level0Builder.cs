using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 一键生成《第25小时》第0层（避难所 1 区）白盒：
/// 建筑 / 楼梯 / 家具 / 可交互物 / 玩家 / 交互 UI / 两个事件场景（棋盘留给棋盘负责人）。
///
/// 菜单：工具 → 25小时 → 重建第0层（白盒）
/// 命令行：Unity.exe -batchmode -quit -projectPath &lt;工程&gt; -executeMethod Level0Builder.BuildAll -logFile &lt;日志&gt;
///
/// 生成出来的都是普通 GameObject，可以直接在 Scene 里拖、改尺寸、改文字；
/// 但注意：再点一次「重建」会把手工改动覆盖掉。
/// </summary>
public static class Level0Builder
{
    const string SceneDir = "Assets/Scenes";
    public const string ShelterScenePath = SceneDir + "/Shelter_L0.unity";
    public const string Event01ScenePath = SceneDir + "/Event_01.unity";
    public const string Event02ScenePath = SceneDir + "/Event_02.unity";
    const string BuildDir = "D:/Unity2D/Build/25Hours";
    const string BuildExe = BuildDir + "/25Hours.exe";
    const string ShotDir = BuildDir + "/预览";

    static int groundLayer = 8;
    static int interactableLayer = 9;

    // ------------------------------------------------------------------
    // 入口
    // ------------------------------------------------------------------

    [MenuItem("工具/25小时/重建第0层（白盒）", false, 1)]
    public static void BuildAll()
    {
        if (!Application.isBatchMode && File.Exists(ShelterScenePath))
        {
            bool ok = EditorUtility.DisplayDialog(
                "重建第0层白盒",
                "会用生成器重新铺 Shelter_L0 和两个事件场景。\n场景里手工拖过的东西会被覆盖。继续吗？",
                "重建", "取消");
            if (!ok) return;
        }

        EnsureFolder("Assets", "Scenes");

        WhiteBoxArt.GenerateAll();
        WhiteBoxUI.Load();
        SetupLayers();
        SetupProjectSettings();

        BuildShelterScene();
        BuildEventScenes();
        SetupBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(ShelterScenePath);

        Debug.Log("[25Hours] 第0层白盒搭建完成：" + ShelterScenePath);
    }

    // ------------------------------------------------------------------
    // 工程设置
    // ------------------------------------------------------------------

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    static void SetupLayers()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogError("[25Hours] 读不到 TagManager.asset，Layer 没设置");
            return;
        }

        var so = new SerializedObject(assets[0]);
        var layers = so.FindProperty("layers");
        SetLayer(layers, so, 8, "Ground");
        SetLayer(layers, so, 9, "Interactable");

        groundLayer = LayerMask.NameToLayer("Ground");
        interactableLayer = LayerMask.NameToLayer("Interactable");
        if (groundLayer < 0) groundLayer = 8;
        if (interactableLayer < 0) interactableLayer = 9;

        Debug.Log("[25Hours] Layer：Ground=" + groundLayer + "，Interactable=" + interactableLayer);
    }

    static void SetLayer(SerializedProperty layers, SerializedObject so, int index, string name)
    {
        if (layers == null || index >= layers.arraySize) return;
        var element = layers.GetArrayElementAtIndex(index);
        if (string.IsNullOrEmpty(element.stringValue))
        {
            element.stringValue = name;
            so.ApplyModifiedProperties();
        }
        else if (element.stringValue != name)
        {
            Debug.LogWarning("[25Hours] Layer " + index + " 已经是「" + element.stringValue + "」，没有改成 " + name);
        }
    }

    static void SetupProjectSettings()
    {
        PlayerSettings.companyName = "Zhao";
        PlayerSettings.productName = "第25小时";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
    }

    // ------------------------------------------------------------------
    // 第0层主场景
    // ------------------------------------------------------------------

    static void BuildShelterScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        new GameObject("GameProgress", typeof(GameProgress));
        new GameObject("AppBoot", typeof(AppBoot));

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.055f, 0.07f, 1f);
        camGo.transform.position = new Vector3(2.8f, 1.9f, -10f);

        var rig = camGo.AddComponent<CameraRig2D>();
        rig.min = new Vector2(-8f, -6.4f);
        rig.max = new Vector2(10.2f, 2.6f);
        rig.smoothTime = 0.08f;

        var world = new GameObject("World").transform;

        var stateGo = new GameObject("场景状态", typeof(ShelterState));
        stateGo.transform.SetParent(world, false);
        var state = stateGo.GetComponent<ShelterState>();
        state.warmWallSprite = LoadSprite("back_wall_1f_warm");

        var architecture = Group(world, "01_建筑");
        var wall1F = BuildArchitecture(architecture);
        state.backWall = wall1F;

        var furniture = Group(world, "02_家具");
        BuildFurniture(furniture);

        var interactables = Group(world, "03_可交互");
        BuildInteractables(interactables, state);

        var characters = Group(world, "04_角色");
        var player = BuildPlayer(characters, rig);

        var lighting = Group(world, "05_灯光");
        BuildLighting(lighting, state);

        var detail = Group(world, "07_细节");
        BuildDetail(detail);

        var foreground = Group(world, "08_前景");
        BuildForeground(foreground);

        var systems = Group(world, "06_系统");
        var tester = new GameObject("自测", typeof(ShelterSelfTest));
        tester.transform.SetParent(systems, false);

        // 事件登记表：id → 场景名。别人加了事件场景就在 Inspector 里加一条。
        var catalogGo = new GameObject("事件登记表", typeof(EventCatalog));
        catalogGo.transform.SetParent(systems, false);
        var catalog = catalogGo.GetComponent<EventCatalog>();
        catalog.events.Add(new EventInfo
        {
            id = "event_01",
            displayName = "事件一 · 回声室",
            sceneName = SceneNames.Event01
        });
        catalog.events.Add(new EventInfo
        {
            id = "event_02",
            displayName = "事件二 · 深处",
            sceneName = SceneNames.Event02
        });

        BuildUI(player);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ShelterScenePath);
    }

    /// <summary>建筑：1F 房间在右、B1 房间在左，中间是楼梯井。</summary>
    static SpriteRenderer BuildArchitecture(Transform parent)
    {
        // 背景
        var wall1F = Backdrop(parent, "背景_1F", 9.5f, 3.75f, 15f, 7.5f, "back_wall_1f").GetComponent<SpriteRenderer>();
        Backdrop(parent, "背景_B1", -10f, -4.4f, 14f, 7.2f, "back_wall_b1");
        Backdrop(parent, "背景_楼梯井", -0.5f, -0.25f, 5f, 15.5f, "back_wall_stair");
        Backdrop(parent, "背景_结构", -10f, 3.75f, 14f, 7.5f, "back_wall_earth");
        Backdrop(parent, "背景_1F下方实体", 9.5f, -4.4f, 15f, 7.2f, "block_dark");

        // 楼板
        Box(parent, "1F_地板", 9.5f, -0.4f, 15f, 0.8f, "block_mid", 0);
        Box(parent, "1F_地板_亮边", 9.5f, -0.06f, 15f, 0.12f, "block_light", 1);
        Box(parent, "B1_顶板", -10f, -0.4f, 14f, 0.8f, "block_mid", 0);
        Box(parent, "B1_顶板_亮边", -10f, -0.06f, 14f, 0.12f, "block_light", 1);
        Box(parent, "B1_地板", -7.5f, -8.4f, 19f, 0.8f, "block_dark", 0);
        Box(parent, "B1_地板_亮边", -7.5f, -8.06f, 19f, 0.1f, "block_mid", 1);
        Box(parent, "1F_天花板", 0f, 7.9f, 34f, 0.8f, "block_dark", 0);
        Box(parent, "1F_天花板_边", 0f, 7.44f, 34f, 0.12f, "block_mid", 1);
        Box(parent, "左墙", -17.3f, -0.35f, 0.6f, 17.3f, "block_dark", 0);
        Box(parent, "右墙", 17.3f, -0.35f, 0.6f, 17.3f, "block_dark", 0);

        // 楼梯：20 级台阶从 (2,0) 下到 (-3,-8)。
        // 台阶本身只做「看起来」，真正给玩家走的是下面那条看不见的斜坡碰撞——
        // 因为台阶只有 0.25 宽，角色却有 0.8 宽，用实体台阶会被卡住。
        var stairs = Group(parent, "楼梯");
        for (int i = 0; i < 20; i++)
        {
            float top = -0.4f * (i + 1);
            float left = 2f - 0.25f * (i + 1);
            float centerX = left + 0.125f;
            float height = top + 8f;
            float centerY = (-8f + top) * 0.5f;
            Box(stairs, "台阶_" + (i + 1).ToString("00"), centerX, centerY, 0.25f, height, "block_stair", 2, false);
            Box(stairs, "台阶踏面_" + (i + 1).ToString("00"), centerX, top - 0.03f, 0.25f, 0.06f, "block_light", 3);
        }

        var ramp = new GameObject("楼梯_斜坡碰撞");
        ramp.transform.SetParent(stairs, false);
        var start = new Vector2(2f, 0f);
        var end = new Vector2(-3f, -8f);
        var delta = end - start;
        var up = new Vector2(-delta.y, delta.x).normalized;
        if (up.y < 0f) up = -up;
        ramp.transform.position = (start + end) * 0.5f - up * 0.15f;
        ramp.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        ramp.layer = groundLayer;
        var rampCollider = ramp.AddComponent<BoxCollider2D>();
        rampCollider.size = new Vector2(delta.magnitude, 0.3f);

        return wall1F;
    }

    static void BuildFurniture(Transform parent)
    {
        // 1F
        Box(parent, "展柜_台座", 4.4f, 0.15f, 2.0f, 0.3f, "block_mid", 2);
        Box(parent, "桌子_桌面", 10.2f, 0.86f, 2.2f, 0.12f, "block_mid", 3);
        Box(parent, "桌子_左腿", 9.25f, 0.38f, 0.12f, 0.76f, "block_dark", 3);
        Box(parent, "桌子_右腿", 11.15f, 0.38f, 0.12f, 0.76f, "block_dark", 3);
        Box(parent, "吊架_1F", 6.4f, 4.9f, 3.0f, 0.25f, "block_dark", 2);
        Box(parent, "吊架_1F_2", 13.6f, 5.4f, 2.4f, 0.25f, "block_dark", 2);

        // B1
        Box(parent, "工作台", -15.0f, -7.55f, 2.2f, 0.9f, "block_mid", 2);
        Box(parent, "工作台_台面", -15.0f, -7.05f, 2.4f, 0.12f, "block_light", 3);
        Box(parent, "床垫", -5.9f, -7.8f, 1.6f, 0.4f, "block_dark", 2);
        Box(parent, "管道_横", -9.5f, -1.1f, 13.0f, 0.12f, "block_dark", 1);
        Box(parent, "管道_竖", -14.6f, -3.6f, 0.12f, 4.6f, "block_dark", 1);
        Box(parent, "挂画", -9.2f, -2.6f, 1.4f, 1.0f, "block_dark", 1);

        // 2 区门口的黑角
        Backdrop(parent, "2区_暗角", -4.0f, -4.4f, 2.2f, 7.2f, "block_black", -15);

    }

    static void BuildInteractables(Transform parent, ShelterState state)
    {
        // ---- 1F ----
        var display = MakeInteractable<DialogueProp>(parent, "展柜", 4.4f, 1.5f, 1.3f, 2.4f, "display_case", 5, "查看");

        var crate = MakeInteractable<FragmentPickup>(parent, "纸箱", 5.6f, 0.5f, 1.0f, 1.0f, "crate", 4, "翻找");
        crate.fragmentId = "frag_01";

        var cabinet = MakeInteractable<FragmentPickup>(parent, "柜子", 7.6f, 0.9f, 1.1f, 1.8f, "cabinet", 3, "打开");
        cabinet.fragmentId = "frag_02";

        var echoBox = MakeInteractable<DialogueProp>(parent, "回声匣", 10.2f, 1.2f, 0.55f, 0.55f, "item_glow", 6, "查看");

        // 事件一的门：第 0 层东墙，一开始就能进
        var door1 = MakeInteractable<EventDoor>(parent, "门·事件一", 16.2f, 1.35f, 1.4f, 2.7f, "door_event", 4, "进入");
        door1.eventId = "event_01";
        door1.doorRenderer = door1.GetComponentInChildren<SpriteRenderer>();
        door1.unlockedSprite = LoadSprite("door_event");

        var doll1 = MakeInteractable<DialogueProp>(parent, "人偶", 12.8f, 0.62f, 0.7f, 1.2f, "npc_doll", 5, "对话");

        var shelf = MakeInteractable<FragmentPickup>(parent, "书架", 14.6f, 0.6f, 1.4f, 1.2f, "cabinet", 3, "翻找");
        shelf.fragmentId = "frag_03";

        var jar = MakeInteractable<FragmentPickup>(parent, "玻璃罐", 14.6f, 1.4f, 0.4f, 0.4f, "crate", 6, "查看");
        jar.fragmentId = "frag_04";

        // ---- B1 ----
        var warn = new GameObject("2区_警示光");
        warn.transform.SetParent(parent, false);
        warn.transform.position = new Vector3(-4.0f, -5.4f, 0f);
        warn.transform.localScale = new Vector3(2.6f, 2.6f, 1f);
        var warnSr = warn.AddComponent<SpriteRenderer>();
        warnSr.sprite = LoadSprite("glow");
        warnSr.color = new Color(1f, 0.3f, 0.28f, 0.18f);
        warnSr.sortingOrder = -14;

        var door2 = MakeInteractable<EventDoor>(parent, "门·事件二", -4.0f, -6.6f, 1.2f, 2.8f, "door_locked", 4, "进入");
        door2.eventId = "event_02";
        door2.requiresEventId = "event_01";
        door2.doorRenderer = door2.GetComponentInChildren<SpriteRenderer>();
        door2.lockedSprite = LoadSprite("door_locked");
        door2.unlockedSprite = LoadSprite("door_event");
        door2.statusGlow = warnSr;

        var drawer = MakeInteractable<FragmentPickup>(parent, "抽屉柜", -7.6f, -7.1f, 1.3f, 1.8f, "cabinet", 3, "打开");
        drawer.fragmentId = "frag_06";

        var doll2 = MakeInteractable<DialogueProp>(parent, "人偶", -9.8f, -7.4f, 0.7f, 1.2f, "npc_doll", 5, "对话");

        var radio = MakeInteractable<FragmentPickup>(parent, "旧收音机", -12.2f, -7.55f, 0.9f, 0.7f, "crate", 4, "查看");
        radio.fragmentId = "frag_05";

        // 展柜里本来就有的那个人偶（玻璃是半透明的，人偶放在柜子里、order 4 会被玻璃盖住一点）
        var caseDoll = new GameObject("人偶_展柜内");
        caseDoll.transform.SetParent(parent, false);
        caseDoll.transform.position = new Vector3(4.22f, 0.66f, 0f);
        var caseDollSr = caseDoll.AddComponent<SpriteRenderer>();
        caseDollSr.sprite = LoadSprite("npc_doll");
        caseDollSr.sortingOrder = 4;
        caseDoll.transform.localScale = Fit(caseDollSr.sprite, 0.32f, 0.68f);

        // 事件完成后，展柜里多出来的那个人偶
        var newDoll = new GameObject("人偶_事件后出现");
        newDoll.transform.SetParent(parent, false);
        newDoll.transform.position = new Vector3(4.66f, 0.66f, 0f);
        var newDollSr = newDoll.AddComponent<SpriteRenderer>();
        newDollSr.sprite = LoadSprite("npc_doll");
        newDollSr.sortingOrder = 4;
        newDoll.transform.localScale = Fit(newDollSr.sprite, 0.32f, 0.68f);
        newDoll.SetActive(false);
        state.newDoll = newDoll;
    }

    static ShelterPlayer BuildPlayer(Transform parent, CameraRig2D rig)
    {
        var go = new GameObject("玩家_主控", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(ShelterPlayer), typeof(ShelterInteractor));
        go.tag = "Player";
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(2.6f, 0.76f, 0f);

        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("player_red");
        sr.sortingOrder = 10;

        var rb = go.GetComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var box = go.GetComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 1.45f);

        var check = new GameObject("GroundCheck");
        check.transform.SetParent(go.transform, false);
        check.transform.localPosition = new Vector3(0f, -0.78f, 0f);

        var player = go.GetComponent<ShelterPlayer>();
        player.spawnPoint = new Vector3(2.6f, 0.76f, 0f);
        player.groundCheck = check.transform;
        player.groundMask = 1 << groundLayer;
        player.spriteRenderer = sr;

        var interactor = go.GetComponent<ShelterInteractor>();
        interactor.interactableMask = 1 << interactableLayer;
        interactor.radius = 1.9f;

        rig.target = go.transform;
        return player;
    }

    static void BuildLighting(Transform parent, ShelterState state)
    {
        var lampRoot = new GameObject("1F_顶灯");
        lampRoot.transform.SetParent(parent, false);
        lampRoot.transform.position = new Vector3(10.2f, 7.15f, 0f);

        var lamp = new GameObject("灯");
        lamp.transform.SetParent(lampRoot.transform, false);
        var lampSr = lamp.AddComponent<SpriteRenderer>();
        lampSr.sprite = LoadSprite("lamp_off");
        lampSr.sortingOrder = 6;
        lamp.transform.localScale = Fit(lampSr.sprite, 0.9f, 0.45f);

        var glow = new GameObject("灯光光晕");
        glow.transform.SetParent(lampRoot.transform, false);
        var glowSr = glow.AddComponent<SpriteRenderer>();
        glowSr.sprite = LoadSprite("glow");
        glowSr.color = new Color(1f, 0.84f, 0.5f, 0.22f);
        glowSr.sortingOrder = -6;
        glow.transform.localScale = new Vector3(8f, 8f, 1f);
        glow.SetActive(false);
        state.lampGlow = glow;

        var b1 = new GameObject("B1_吊灯");
        b1.transform.SetParent(parent, false);
        b1.transform.position = new Vector3(-10f, -1.1f, 0f);
        var b1Lamp = new GameObject("灯");
        b1Lamp.transform.SetParent(b1.transform, false);
        var b1Sr = b1Lamp.AddComponent<SpriteRenderer>();
        b1Sr.sprite = LoadSprite("lamp_off");
        b1Sr.sortingOrder = 3;
        b1Lamp.transform.localScale = Fit(b1Sr.sprite, 0.8f, 0.4f);
    }

    /// <summary>细节层：墙裙、墙筋、挂钟、光池、B1 杂物。全部不碰撞，只是让空间「有层次」。</summary>
    static void BuildDetail(Transform parent)
    {
        // ---- 1F 墙面 ----
        Box(parent, "墙裙_1F", 9.5f, 2.3f, 15f, 0.12f, "block_dark", 1, false);

        float[] ribs = { 6.9f, 11.6f, 13.9f };
        for (int i = 0; i < ribs.Length; i++)
        {
            Box(parent, "墙筋_1F_" + (i + 1), ribs[i], 4.9f, 0.3f, 5.1f, "block_dark", 1, false);
            Box(parent, "墙筋_1F_受光_" + (i + 1), ribs[i] - 0.13f, 4.9f, 0.06f, 5.1f, "block_mid", 1, false);
        }

        Box(parent, "横梁_1F", 9.5f, 7.1f, 15f, 0.22f, "block_dark", 1, false);

        // 挂钟：指针停在「第 25 小时」，也是这一层的路标
        Box(parent, "挂钟", 11.6f, 6.05f, 1.5f, 1.5f, "clock_face", 2, false);
        Glow(parent, "挂钟_微光", 11.6f, 6.05f, 2.2f, 2.2f, new Color(1f, 0.9f, 0.7f, 0.06f), 1);

        // ---- B1 墙面 ----
        Box(parent, "墙裙_B1", -10f, -5.0f, 14f, 0.1f, "block_dark", 1, false);
        Box(parent, "墙筋_B1_1", -12.4f, -4.55f, 0.28f, 6.9f, "block_dark", 1, false);
        Box(parent, "墙筋_B1_受光_1", -12.53f, -4.55f, 0.06f, 6.9f, "block_mid", 1, false);
        Box(parent, "墙筋_B1_2", -7.2f, -4.55f, 0.28f, 6.9f, "block_dark", 1, false);
        Box(parent, "墙筋_B1_受光_2", -7.33f, -4.55f, 0.06f, 6.9f, "block_mid", 1, false);

        // ---- 灯光引导 ----
        Box(parent, "壁灯_门", 16.2f, 3.4f, 0.55f, 0.16f, "block_mid", 3, false);
        Glow(parent, "壁灯_门_光", 16.2f, 3.25f, 2.6f, 2.4f, new Color(1f, 0.84f, 0.55f, 0.16f), 2);

        Glow(parent, "灯池_1F_1", 6.4f, 0.2f, 4.6f, 1.5f, new Color(1f, 0.82f, 0.55f, 0.10f), 2);
        Glow(parent, "灯池_1F_2", 13.6f, 0.2f, 4.2f, 1.4f, new Color(1f, 0.82f, 0.55f, 0.10f), 2);
        Glow(parent, "灯池_B1", -10f, -7.95f, 4.2f, 1.2f, new Color(1f, 0.78f, 0.5f, 0.08f), 2);

        // ---- 门框（让两扇门读起来像「洞口」而不是一块贴图）----
        Box(parent, "门框_事件一_左", 15.42f, 1.45f, 0.2f, 2.9f, "block_dark", 2, false);
        Box(parent, "门框_事件一_右", 16.98f, 1.45f, 0.2f, 2.9f, "block_dark", 2, false);
        Box(parent, "门框_事件一_上", 16.2f, 2.92f, 1.76f, 0.22f, "block_dark", 2, false);

        Box(parent, "门框_事件二_左", -4.72f, -6.55f, 0.22f, 2.9f, "block_dark", 2, false);
        Box(parent, "门框_事件二_右", -3.28f, -6.55f, 0.22f, 2.9f, "block_dark", 2, false);
        Box(parent, "门框_事件二_上", -4.0f, -5.05f, 1.66f, 0.22f, "block_dark", 2, false);

        // ---- B1 杂物 ----
        Box(parent, "线缆盘", -13.05f, -7.5f, 0.9f, 1.0f, "block_dark", 2, false);
        Box(parent, "线缆盘_芯", -13.05f, -7.5f, 0.32f, 0.32f, "block_black", 3, false);
        Box(parent, "挂架_B1", -11.0f, -2.7f, 1.8f, 0.12f, "block_dark", 1, false);
        Box(parent, "挂架_B1_物", -11.35f, -3.2f, 0.5f, 0.9f, "block_black", 1, false);
        Box(parent, "木箱_B1", -6.15f, -7.55f, 0.9f, 0.9f, "crate", 2, false);
        Box(parent, "木箱_B1_上", -6.15f, -6.65f, 0.62f, 0.62f, "crate", 2, false);
    }

    /// <summary>前景层：压在镜头和角色之间的一层剪影，制造纵深（sortingOrder 30）。</summary>
    static void BuildForeground(Transform parent)
    {
        Box(parent, "前景_顶梁", 9.5f, 7.42f, 15.6f, 0.55f, "block_black", 30, false);
        Box(parent, "前景_右框", 17.05f, 3.4f, 0.5f, 8.4f, "block_black", 30, false);
        Box(parent, "前景_悬线", 4.2f, 6.6f, 0.08f, 1.6f, "block_black", 30, false);
        Box(parent, "前景_悬线_2", 14.9f, 6.7f, 0.08f, 1.4f, "block_black", 30, false);
    }

    static void Glow(Transform parent, string name, float cx, float cy, float w, float h, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(cx, cy, 0f);
        go.transform.localScale = new Vector3(w, h, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("glow");
        sr.color = color;
        sr.sortingOrder = order;
    }

    static void BuildUI(ShelterPlayer player)
    {
        var canvas = WhiteBoxUI.MakeCanvas("UI");
        var hud = canvas.gameObject.AddComponent<ShelterHUD>();

        var title = WhiteBoxUI.Label("标题", canvas.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -30f), new Vector2(900f, 44f), "第 25 小时　·　避难所 1 区",
            32, TextAnchor.UpperLeft, new Color(0.78f, 0.8f, 0.86f, 0.9f));

        var fragments = WhiteBoxUI.Label("碎片", canvas.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-40f, -30f), new Vector2(500f, 44f), "碎片 0 / 6",
            32, TextAnchor.UpperRight, new Color(0.9f, 0.78f, 0.5f, 0.95f));

        var help = WhiteBoxUI.Label("操作提示", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(40f, 26f), new Vector2(1200f, 36f), "A / D 移动　　空格 跳跃　　E 交互",
            24, TextAnchor.LowerLeft, new Color(0.6f, 0.63f, 0.7f, 0.85f));

        var promptPanel = WhiteBoxUI.Panel("提示条", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 170f), new Vector2(560f, 62f), new Color(0.05f, 0.06f, 0.08f, 0.95f));
        var promptText = WhiteBoxUI.Label("提示文字", promptPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(520f, 50f), "", 30, TextAnchor.MiddleCenter, new Color(0.96f, 0.9f, 0.72f, 1f));

        var dialogueRoot = new GameObject("对话系统", typeof(ShelterDialogue));
        dialogueRoot.transform.SetParent(canvas.transform, false);

        var dialoguePanel = WhiteBoxUI.Panel("对话面板", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 36f), new Vector2(1560f, 250f), new Color(0.04f, 0.05f, 0.07f, 0.92f));
        var speaker = WhiteBoxUI.Label("名字", dialoguePanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(36f, -22f), new Vector2(700f, 40f), "", 30, TextAnchor.UpperLeft, new Color(0.98f, 0.83f, 0.5f, 1f));
        var body = WhiteBoxUI.Label("正文", dialoguePanel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(36f, -74f), new Vector2(1480f, 120f), "", 30, TextAnchor.UpperLeft, new Color(0.93f, 0.94f, 0.96f, 1f));
        var hint = WhiteBoxUI.Label("继续提示", dialoguePanel.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-28f, 16f), new Vector2(560f, 30f), "", 20, TextAnchor.LowerRight, new Color(0.62f, 0.65f, 0.72f, 0.9f));

        var toast = WhiteBoxUI.Label("气泡", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -92f), new Vector2(900f, 44f), "", 28, TextAnchor.UpperCenter, new Color(1f, 0.86f, 0.55f, 1f));

        hud.promptRoot = promptPanel.gameObject;
        hud.promptText = promptText;
        hud.fragmentText = fragments;
        hud.titleText = title;
        hud.helpText = help;
        hud.toastText = toast;

        var dialogue = dialogueRoot.GetComponent<ShelterDialogue>();
        dialogue.panel = dialoguePanel.gameObject;
        dialogue.speakerText = speaker;
        dialogue.bodyText = body;
        dialogue.hintText = hint;
        dialogue.player = player;

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    // ------------------------------------------------------------------
    // 事件场景：棋盘是棋盘负责人的部分，这里只留接入约定
    // ------------------------------------------------------------------

    static void BuildEventScenes()
    {
        BuildEventScene(Event01ScenePath, "event_01");
        BuildEventScene(Event02ScenePath, "event_02");
        Debug.Log("[25Hours] 事件场景：Event_01 / Event_02");
    }

    // 事件场景是给负责人接玩法的空房间：只有相机 + 进度 + 事件脚本，不写任何文字。
    static void BuildEventScene(string path, string eventId)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.035f, 0.045f, 1f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        new GameObject("GameProgress", typeof(GameProgress));

        var roomGo = new GameObject("事件房间", typeof(EventRoomTemplate));
        var room = roomGo.GetComponent<EventRoomTemplate>();
        room.SetEventId(eventId);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);
    }
    static void SetupBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ShelterScenePath, true),
            new EditorBuildSettingsScene(Event01ScenePath, true),
            new EditorBuildSettingsScene(Event02ScenePath, true),
        };
        Debug.Log("[25Hours] Build Settings：Shelter_L0 / Event_01 / Event_02");
    }

    // ------------------------------------------------------------------
    // 小工具
    // ------------------------------------------------------------------

    static Sprite LoadSprite(string name)
    {
        var sprite = WhiteBoxArt.LoadSprite(name);
        if (sprite == null) Debug.LogError("[25Hours] 找不到图：" + name);
        return sprite;
    }

    static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Vector3 Fit(Sprite sprite, float width, float height)
    {
        var size = sprite != null ? sprite.bounds.size : Vector3.one;
        return new Vector3(width / Mathf.Max(0.001f, size.x), height / Mathf.Max(0.001f, size.y), 1f);
    }

    static GameObject Backdrop(Transform parent, string name, float cx, float cy, float w, float h, string sprite, int order = -20)
    {
        return Box(parent, name, cx, cy, w, h, sprite, order, false, 0);
    }

    static GameObject Box(Transform parent, string name, float cx, float cy, float w, float h, string spriteName, int order, bool collider = true, int layer = -1)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(cx, cy, 0f);

        var sprite = LoadSprite(spriteName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        go.transform.localScale = Fit(sprite, w, h);

        if (collider) go.AddComponent<BoxCollider2D>();
        go.layer = layer >= 0 ? layer : groundLayer;
        return go;
    }

    static T MakeInteractable<T>(Transform parent, string name, float cx, float cy, float w, float h, string spriteName, int order, string verb) where T : InteractableBase
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(cx, cy, 0f);
        root.layer = interactableLayer;

        var visual = new GameObject("图");
        visual.transform.SetParent(root.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(spriteName);
        sr.sortingOrder = order;
        visual.transform.localScale = Fit(sr.sprite, w, h);

        var trigger = root.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = Mathf.Clamp(Mathf.Max(w, h) * 0.5f, 0.35f, 1.1f);

        var highlight = new GameObject("高亮");
        highlight.transform.SetParent(root.transform, false);
        var highlightSr = highlight.AddComponent<SpriteRenderer>();
        highlightSr.sprite = LoadSprite("glow");
        highlightSr.color = new Color(1f, 0.88f, 0.55f, 0.32f);
        highlightSr.sortingOrder = order - 1;
        float glowSize = Mathf.Clamp(Mathf.Max(w, h) * 1.5f, 0.9f, 2.6f);
        highlight.transform.localScale = new Vector3(glowSize, glowSize, 1f);
        highlight.SetActive(false);

        var component = root.AddComponent<T>();
        component.displayName = name;
        component.verb = verb;
        component.highlight = highlight;
        return component;
    }

    // ------------------------------------------------------------------
    // 验收辅助
    // ------------------------------------------------------------------

    [MenuItem("工具/25小时/打开第0层并运行", false, 2)]
    public static void OpenAndPlay()
    {
        EditorSceneManager.OpenScene(ShelterScenePath);
        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }

    [MenuItem("工具/25小时/重建并截图", false, 3)]
    public static void RebuildAndPreview()
    {
        BuildAll();
        CapturePreviews();
    }

    [MenuItem("工具/25小时/一键：重建 + 截图 + 打包", false, 4)]
    public static void BuildEverything()
    {
        BuildAll();
        CapturePreviews();
        BuildWindows();
    }

    [MenuItem("工具/25小时/截图预览（世界）", false, 20)]
    public static void CapturePreviews()
    {
        EditorSceneManager.OpenScene(ShelterScenePath);
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[25Hours] 场景里没有 MainCamera");
            return;
        }

        Directory.CreateDirectory(ShotDir);
        var original = cam.transform.position;

        Shot(cam, new Vector3(9.5f, 3.0f, -10f), ShotDir + "/01_避难所1F.png");
        Shot(cam, new Vector3(0.5f, -0.6f, -10f), ShotDir + "/02_楼梯与2区.png");
        Shot(cam, new Vector3(-10.0f, -4.4f, -10f), ShotDir + "/03_避难所B1.png");
        // 出生机位：用来对照游戏里实际看到的第一屏
        Shot(cam, new Vector3(3.4f, 2.6f, -10f), ShotDir + "/04_出生机位.png");

        cam.transform.position = original;
        Debug.Log("[25Hours] 预览图：" + ShotDir);
    }

    static void Shot(Camera cam, Vector3 position, string path)
    {
        const int width = 1600;
        const int height = 900;

        cam.transform.position = position;
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());

        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(texture);
    }

    [MenuItem("工具/25小时/构建 Windows 试玩版", false, 30)]
    public static void BuildWindows()
    {
        Directory.CreateDirectory(BuildDir);
        SetupProjectSettings();

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ShelterScenePath, Event01ScenePath, Event02ScenePath },
            locationPathName = BuildExe,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log("[25Hours] 构建结果：" + report.summary.result + "，输出：" + BuildExe);

        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new System.Exception("构建失败：" + report.summary.result);
        }
    }
}
