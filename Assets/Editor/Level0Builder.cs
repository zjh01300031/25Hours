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

    public const string PrefabDir = "Assets/Prefabs";

    static Color colorPlayer = new Color(0.86f, 0.30f, 0.26f);
    static Color colorDoor = new Color(0.52f, 0.68f, 0.92f);
    static Color colorPickup = new Color(0.96f, 0.82f, 0.42f);

    [MenuItem("工具/25小时/重建场景骨架（只有功能）", false, 1)]
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

    [MenuItem("工具/25小时/导出预制体（玩家 / 事件门 / 拾取物）", false, 2)]
    public static void ExportPrefabs()
    {
        EnsureFolder(PrefabDir);
        WhiteBoxArt.Prepare();
        WhiteBoxUI.Load();
        SetupLayers();

        // 记住当前场景，导出完再打开回来，免得把正在编辑的场景弄丢
        string previousScene = EditorSceneManager.GetActiveScene().path;
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 玩家要一个相机跟随目标，导出后相机本身丢掉
        var camGo = new GameObject("tmp_camera", typeof(Camera), typeof(CameraRig2D));
        var rig = camGo.GetComponent<CameraRig2D>();

        var player = BuildPlayer(null, rig);
        PrefabUtility.SaveAsPrefabAsset(player.gameObject, PrefabDir + "/玩家.prefab");
        Object.DestroyImmediate(player.gameObject);

        var door = BuildDoor(null, "事件门", 0f, 0f, 1.2f, 2.6f, "", null, null, null, null);
        PrefabUtility.SaveAsPrefabAsset(door.gameObject, PrefabDir + "/事件门.prefab");
        Object.DestroyImmediate(door.gameObject);

        var item = BuildPickup(null, "拾取物", 0f, 0f, "item_01", null);
        PrefabUtility.SaveAsPrefabAsset(item.gameObject, PrefabDir + "/拾取物.prefab");
        Object.DestroyImmediate(item.gameObject);

        var text = BuildText(null, "文字物件", 0f, 0f);
        PrefabUtility.SaveAsPrefabAsset(text.gameObject, PrefabDir + "/文字物件.prefab");
        Object.DestroyImmediate(text.gameObject);

        Object.DestroyImmediate(camGo);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (!string.IsNullOrEmpty(previousScene))
        {
            EditorSceneManager.OpenScene(previousScene);
        }

        Debug.Log("[25Hours] 预制体已导出：" + PrefabDir);
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

        var characters = Group(world, "01_玩家");
        var player = BuildPlayer(characters, rig);

        // 关卡内容（地面、墙、楼梯、门、物品）不再生成，全部留给项目里的人手搭。
        // 需要现成形式就用菜单「导出预制体」，拖进自己的场景。
        var systems = Group(world, "02_系统");
        var catalogGo = new GameObject("事件登记表", typeof(EventCatalog));
        catalogGo.transform.SetParent(systems, false);
        var catalog = catalogGo.GetComponent<EventCatalog>();
        catalog.events.Add(new EventInfo { id = "event_01", displayName = "事件一", sceneName = SceneNames.Event01 });
        catalog.events.Add(new EventInfo { id = "event_02", displayName = "事件二", sceneName = SceneNames.Event02 });

        BuildUI(player);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ShelterScenePath);
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
        return door;
    }

    /// <summary>「按 E 出字」的通用物件：白方块 + 触发圈 + SimpleTextTrigger。</summary>
    static SimpleTextTrigger BuildText(Transform parent, string name, float cx, float cy)
    {
        var go = new GameObject(name, typeof(SimpleTextTrigger));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(cx, cy, 0f);
        go.layer = InteractableLayer;

        Visual(go.transform, new Color(0.72f, 0.72f, 0.76f, 1f), 0.9f, 0.9f, 4);

        var body = go.AddComponent<CircleCollider2D>();
        body.isTrigger = true;
        body.radius = 0.75f;

        var text = go.GetComponent<SimpleTextTrigger>();
        text.displayName = "文字物件";
        text.verb = "查看";
        text.speaker = "旁白";
        text.lines = new string[0];
        return text;
    }

    static PickupItem BuildPickup(Transform parent, string name, float cx, float cy, string itemId, string storyId)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(cx, cy, 0f);
        root.layer = InteractableLayer;

        Visual(root.transform, colorPickup, 0.5f, 0.5f, 6);

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

    static SpriteRenderer Visual(Transform parent, Color color, float w, float h, int order)
    {
        // 图直接挂在物体自己身上，不再多一层「图」子物体
        var go = parent.gameObject;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteBoxArt.Square;
        sr.color = color;
        sr.sortingOrder = order;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(w, h);
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

    [MenuItem("工具/25小时/清空存档（物品会重新出现）", false, 41)]
    public static void ClearProgress()
    {
        var path = GameProgress.SavePath;
        if (File.Exists(path)) File.Delete(path);
        Debug.Log("[25Hours] 已删除存档：" + path + "（下次运行物品会重新出现、门回到上锁状态）");
    }

    [MenuItem("工具/25小时/修复丢失的精灵引用", false, 45)]
    public static void RepairSprites()
    {
        var scene = EditorSceneManager.OpenScene(ShelterScenePath);
        var sprite = WhiteBoxArt.Square;
        int count = 0;

        foreach (var sr in Object.FindObjectsOfType<SpriteRenderer>(true))
        {
            if (sr.sprite == null) { sr.sprite = sprite; count++; }
        }
        foreach (var img in Object.FindObjectsOfType<UnityEngine.UI.Image>(true))
        {
            if (img.sprite == null) { img.sprite = sprite; count++; }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ShelterScenePath);
        Debug.Log("[25Hours] 精灵引用修复：" + count + " 个");
    }

    [MenuItem("工具/25小时/打开第0层并运行", false, 10)]
    public static void OpenAndPlay()
    {
        EditorSceneManager.OpenScene(ShelterScenePath);
        EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
    }

    [MenuItem("工具/25小时/重建并截图", false, 11)]
    public static void RebuildAndPreview()
    {
        BuildAll();
        CapturePreviews();
    }

    [MenuItem("工具/25小时/一键：重建 + 截图 + 打包", false, 12)]
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

        // 骨架里没有关卡内容，只拍一张出生视角，关卡搭起来以后可以自己加机位
        Shot(cam, new Vector3(0f, 2f, -10f), ShotDir + "/00-skeleton.png");

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
