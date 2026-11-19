using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 第0层的框架生成器：只搭「两层 + 楼梯 + 两扇门」这条能走通的最小结构，
/// 物件一律用 Unity 自带的白方块 + 颜色，不产出任何美术资源。
/// 关卡内容（房间多大、家具、碎片、台词）留给人来做。
/// </summary>
public static class Level0Builder
{
    public const string SceneDir = "Assets/Scenes";
    public const string ShelterScenePath = SceneDir + "/Shelter_L0.unity";
    public const string Event01ScenePath = SceneDir + "/Event_01.unity";
    public const string Event02ScenePath = SceneDir + "/Event_02.unity";
    public const string BuildDir = "D:/Unity2D/Build/25Hours";
    public const string ShotDir = BuildDir + "/preview";
    public const string BuildExe = BuildDir + "/25Hours.exe";
    public const string StoryDir = "Assets/Resources/Story";
    public const string StoryPath = StoryDir + "/story.csv";

    const int GroundLayer = 8;
    const int InteractableLayer = 9;

    // 1F 地面 x 从 -6 到 16；B1 地面 x 从 -22 到 -11；楼梯连接 (-6,0) 与 (-11,-6)
    const float OneFFloorTop = 0f;
    const float BOneFloorTop = -6f;
    const float StairTopX = -6f;
    const float StairBottomX = -11f;

    static Color colorFloor = new Color(0.72f, 0.74f, 0.77f);
    static Color colorWall = new Color(0.38f, 0.40f, 0.44f);
    static Color colorStair = new Color(0.30f, 0.32f, 0.36f);
    static Color colorPlayer = new Color(0.86f, 0.30f, 0.26f);
    static Color colorDoor = new Color(0.62f, 0.56f, 0.42f);

    [MenuItem("工具/25小时/重建第0层（框架）", false, 1)]
    public static void BuildAll()
    {
        EnsureFolder(SceneDir);
        WhiteBoxArt.Prepare();
        EnsureStoryCsv();
        WhiteBoxUI.Load();
        SetupLayers();
        SetupProjectSettings();

        BuildShelterScene();
        BuildEventScene(Event01ScenePath, "event_01");
        BuildEventScene(Event02ScenePath, "event_02");
        SetupBuildSettings();

        AssetDatabase.SaveAssets();
        Debug.Log("[25Hours] 第0层框架搭建完成：" + ShelterScenePath);
    }

    // ------------------------------------------------------------------
    // 避难所
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
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f);
        camGo.transform.position = new Vector3(0f, 2f, -10f);

        var rig = camGo.AddComponent<CameraRig2D>();
        rig.min = new Vector2(-22f, -8.5f);
        rig.max = new Vector2(16f, 4f);
        rig.smoothTime = 0.08f;

        var world = new GameObject("World").transform;

        var structure = Group(world, "01_结构");
        BuildStructure(structure);

        var characters = Group(world, "02_角色");
        var player = BuildPlayer(characters, rig);

        var items = Group(world, "03_物品");
        BuildPickup(items, "拾取_磁带", 7f, 1.2f, "item_tape", "pickup.tape");
        BuildPickup(items, "拾取_钥匙", -15f, -5.2f, "item_key", "pickup.key");

        var doors = Group(world, "04_门");
        BuildDoor(doors, "门·事件一", 15.0f, 1.3f, 1.2f, 2.6f,
            "event_01", null, "door01.enter", null, "door01.cleared");
        BuildDoor(doors, "门·事件二", -21.0f, -4.7f, 1.2f, 2.6f,
            "event_02", "event_01", "door02.enter", "door02.locked", null);

        // 事件登记表：id → 场景名。加事件就在 Inspector 里加一条。
        var systems = Group(world, "05_系统");
        var catalogGo = new GameObject("事件登记表", typeof(EventCatalog));
        catalogGo.transform.SetParent(systems, false);
        var catalog = catalogGo.GetComponent<EventCatalog>();
        catalog.events.Add(new EventInfo { id = "event_01", displayName = "事件一", sceneName = SceneNames.Event01 });
        catalog.events.Add(new EventInfo { id = "event_02", displayName = "事件二", sceneName = SceneNames.Event02 });

        var tester = new GameObject("自测", typeof(ShelterSelfTest));
        tester.transform.SetParent(systems, false);

        BuildUI(player);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ShelterScenePath);
    }

    static void BuildStructure(Transform parent)
    {
        // 1F：地板 + 右墙（左边是楼梯口，不砌墙，走过去就是斜坡）
        Box(parent, "1F_地板", 5f, OneFFloorTop - 0.4f, 22f, 0.8f, colorFloor);
        Box(parent, "1F_右墙", 16.3f, 4.1f, 0.6f, 9f, colorWall);

        // B1：地板 + 左墙
        Box(parent, "B1_地板", -16.5f, BOneFloorTop - 0.4f, 11f, 0.8f, colorFloor);
        Box(parent, "B1_左墙", -22.3f, -2.1f, 0.6f, 8.6f, colorWall);

        // 楼梯：13 级视觉台阶，真正给玩家走的是下面那条看不见的斜坡
        var stairs = Group(parent, "楼梯");
        const int steps = 13;
        float stepH = (OneFFloorTop - BOneFloorTop) / steps;
        float stepW = (StairBottomX - StairTopX) / steps;
        for (int i = 0; i < steps; i++)
        {
            float top = OneFFloorTop - stepH * (i + 1);
            float left = StairTopX + stepW * i;
            Box(stairs, "台阶_" + (i + 1).ToString("00"),
                left + stepW * 0.5f, top - stepH * 0.5f,
                Mathf.Abs(stepW) * 1.7f, stepH * 1.9f, colorStair, false);
        }

        var ramp = new GameObject("楼梯_斜坡碰撞");
        ramp.transform.SetParent(stairs, false);
        var start = new Vector2(StairTopX, OneFFloorTop);
        var end = new Vector2(StairBottomX, BOneFloorTop);
        var delta = end - start;
        var up = new Vector2(-delta.y, delta.x).normalized;
        if (up.y < 0f) up = -up;
        ramp.transform.position = (start + end) * 0.5f - up * 0.15f;
        ramp.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        ramp.layer = GroundLayer;
        var rampCollider = ramp.AddComponent<BoxCollider2D>();
        rampCollider.size = new Vector2(delta.magnitude, 0.3f);
    }

    static ShelterPlayer BuildPlayer(Transform parent, CameraRig2D rig)
    {
        var go = new GameObject("玩家_主控", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(ShelterPlayer), typeof(ShelterInteractor));
        go.tag = "Player";
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(0f, 1f, 0f);

        Visual(go.transform, colorPlayer, 0.8f, 1.45f, 10);

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
        player.spawnPoint = new Vector3(0f, 1f, 0f);
        player.groundCheck = check.transform;
        player.groundMask = 1 << GroundLayer;

        var interactor = go.GetComponent<ShelterInteractor>();
        interactor.interactableMask = 1 << InteractableLayer;
        interactor.radius = 1.6f;

        rig.target = go.transform;
        return player;
    }

    static EventDoor BuildDoor(Transform parent, string name, float cx, float cy, float w, float h,
        string eventId, string requiresEventId, string enterStoryId, string lockedStoryId, string clearedStoryId)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(cx, cy, 0f);
        root.layer = InteractableLayer;

        var sr = Visual(root.transform, colorDoor, w, h, 4);

        var glowSr = Visual(root.transform, new Color(1f, 1f, 1f, 0.06f), w * 1.8f, h * 1.25f, 3);

        var trigger = root.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 1.0f;

        var door = root.AddComponent<EventDoor>();
        door.displayName = name;
        door.verb = "进入";
        door.eventId = eventId;
        door.requiresEventId = requiresEventId;
        door.enterStoryId = enterStoryId;
        door.lockedStoryId = lockedStoryId;
        door.clearedStoryId = clearedStoryId;
        door.doorRenderer = sr;
        door.statusGlow = glowSr;
        return door;
    }

    static PickupItem BuildPickup(Transform parent, string name, float cx, float cy, string itemId, string storyId)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(cx, cy, 0f);
        root.layer = InteractableLayer;

        Visual(root.transform, new Color(0.95f, 0.82f, 0.45f), 0.5f, 0.5f, 6);

        var trigger = root.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.9f;

        var item = root.AddComponent<PickupItem>();
        item.displayName = name;
        item.itemId = itemId;
        item.storyId = storyId;
        return item;
    }

    // ------------------------------------------------------------------
    // 事件场景（空房间，留给事件负责人）
    // ------------------------------------------------------------------

    static void BuildEventScene(string path, string eventId)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        new GameObject("GameProgress", typeof(GameProgress));

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.035f, 0.045f, 1f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        var roomGo = new GameObject("事件房间", typeof(EventRoomTemplate));
        var room = roomGo.GetComponent<EventRoomTemplate>();
        room.SetEventId(eventId);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);
    }

    // ------------------------------------------------------------------
    // UI
    // ------------------------------------------------------------------

    static void BuildUI(ShelterPlayer player)
    {
        var canvas = WhiteBoxUI.MakeCanvas("UI");
        var hud = canvas.gameObject.AddComponent<ShelterHUD>();

        hud.titleText = WhiteBoxUI.Label("标题", canvas.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -30f), new Vector2(900f, 44f), "第0层 · 框架",
            32, TextAnchor.UpperLeft, new Color(0.78f, 0.80f, 0.86f, 0.9f));

        hud.helpText = WhiteBoxUI.Label("操作提示", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(40f, 28f), new Vector2(900f, 40f), "A / D 移动　空格 跳跃　E 交互",
            28, TextAnchor.LowerLeft, new Color(0.62f, 0.65f, 0.70f, 0.85f));

        var promptPanel = WhiteBoxUI.Panel("提示条", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 120f), new Vector2(560f, 60f), new Color(0.08f, 0.09f, 0.11f, 0.95f));
        hud.promptRoot = promptPanel.gameObject;
        hud.promptText = WhiteBoxUI.Label("提示文字", promptPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(520f, 48f), "", 30, TextAnchor.MiddleCenter, new Color(0.97f, 0.92f, 0.78f));
        hud.promptRoot.SetActive(false);

        hud.toastText = WhiteBoxUI.Label("toast", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -34f), new Vector2(1200f, 44f), "", 30, TextAnchor.UpperCenter, new Color(0.93f, 0.85f, 0.6f));

        // 对话框
        var dialog = WhiteBoxUI.Panel("对话框", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(120f, 60f), new Vector2(1680f, 260f), new Color(0.05f, 0.06f, 0.08f, 0.96f));
        var dialogue = canvas.gameObject.AddComponent<ShelterDialogue>();
        dialogue.player = player;
        dialogue.panel = dialog.gameObject;
        dialogue.speakerText = WhiteBoxUI.Label("说话人", dialog.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -22f), new Vector2(700f, 40f), "", 32, TextAnchor.UpperLeft, new Color(0.93f, 0.85f, 0.6f));
        dialogue.bodyText = WhiteBoxUI.Label("正文", dialog.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -70f), new Vector2(1600f, 130f), "", 32, TextAnchor.UpperLeft, new Color(0.95f, 0.95f, 0.95f));
        dialogue.hintText = WhiteBoxUI.Label("继续提示", dialog.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-40f, 18f), new Vector2(700f, 36f), "E / 空格 继续　·　Esc 关闭", 26, TextAnchor.LowerRight, new Color(0.6f, 0.62f, 0.66f));
        dialogue.panel.SetActive(false);

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(canvas.transform, false);
    }

    // ------------------------------------------------------------------
    // 工具
    // ------------------------------------------------------------------

    static GameObject Box(Transform parent, string name, float cx, float cy, float w, float h, Color color, bool collider = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(cx, cy, 0f);

        // 图挂在子物体上：碰撞体的尺寸用世界尺寸写死，不受视觉缩放影响
        Visual(go.transform, color, w, h, 0);

        if (collider)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(w, h);
        }
        go.layer = GroundLayer;
        return go;
    }

    static SpriteRenderer Visual(Transform parent, Color color, float w, float h, int order)
    {
        var go = new GameObject("图");
        go.transform.SetParent(parent, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteBoxArt.Square;
        sr.color = color;
        sr.sortingOrder = order;

        float unit = sr.sprite != null ? sr.sprite.bounds.size.x : 1f;
        if (unit <= 0.0001f) unit = 1f;
        go.transform.localScale = new Vector3(w / unit, h / unit, 1f);
        return sr;
    }

    static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static void EnsureStoryCsv()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(StoryDir);

        if (File.Exists(StoryPath)) return;

        const string content =
            "id,speaker,text\n" +
            "door01.enter,门·事件一,（示例文案）东边这扇门通向事件一。\n" +
            "door01.cleared,门·事件一,（示例文案）事件一已经走过一次了。\n" +
            "door02.locked,门·事件二,（示例文案）门锁着，先在事件一里把那摊事做完。\n" +
            "door02.enter,门·事件二,（示例文案）锁开了，门后面是事件二。\n" +
            "pickup.tape,磁带,（示例文案）一盒没有标签的磁带，标签被人撕掉了。\n" +
            "pickup.key,钥匙,（示例文案）一把锈掉的钥匙，不知道开哪扇门。\n";

        File.WriteAllText(StoryPath, content, new System.Text.UTF8Encoding(false));
        AssetDatabase.ImportAsset(StoryPath, ImportAssetOptions.ForceUpdate);
        Debug.Log("[25Hours] 已生成文案表：" + StoryPath);
    }

    [MenuItem("工具/25小时/检查文案", false, 40)]
    public static void CheckStory()
    {
        Story.Reload();

        if (!File.Exists(ShelterScenePath))
        {
            Debug.LogWarning("[25Hours] 还没有 Shelter_L0 场景，先重建一次");
            return;
        }

        EditorSceneManager.OpenScene(ShelterScenePath);

        int missing = 0;
        foreach (var door in Object.FindObjectsOfType<EventDoor>())
        {
            missing += Report(door.displayName + ".enterStoryId", door.enterStoryId);
            missing += Report(door.displayName + ".lockedStoryId", door.lockedStoryId);
            missing += Report(door.displayName + ".clearedStoryId", door.clearedStoryId);
        }
        foreach (var item in Object.FindObjectsOfType<PickupItem>())
        {
            missing += Report(item.displayName + ".storyId", item.storyId);
        }
        foreach (var prop in Object.FindObjectsOfType<StoryInteractable>())
        {
            missing += Report(prop.displayName + ".storyId", prop.storyId);
        }

        if (missing == 0) Debug.Log("[25Hours] 文案齐全，一共 " + Story.Count + " 段");
        else Debug.LogWarning("[25Hours] 有 " + missing + " 个文案 id 在 story.csv 里找不到，运行时会走占位提示");
    }

    static int Report(string owner, string storyId)
    {
        if (string.IsNullOrEmpty(storyId)) return 0;
        if (Story.Has(storyId)) return 0;

        Debug.LogWarning("[25Hours] 缺文案：" + owner + " = " + storyId);
        return 1;
    }

    static void SetupLayers()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;

        var tagManager = new SerializedObject(assets[0]);
        var layers = tagManager.FindProperty("layers");
        layers.GetArrayElementAtIndex(GroundLayer).stringValue = "Ground";
        layers.GetArrayElementAtIndex(InteractableLayer).stringValue = "Interactable";
        tagManager.ApplyModifiedProperties();
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
    // 验收
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

        Shot(cam, new Vector3(6f, 2f, -10f), ShotDir + "/01-floor-1f.png");
        Shot(cam, new Vector3(-8f, -2f, -10f), ShotDir + "/02-stairs.png");
        Shot(cam, new Vector3(-17f, -4f, -10f), ShotDir + "/03-floor-b1.png");
        Shot(cam, new Vector3(0f, 2f, -10f), ShotDir + "/04-spawn.png");

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
