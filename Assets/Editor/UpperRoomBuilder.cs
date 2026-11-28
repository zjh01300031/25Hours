using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>生成「避难所二层」，并在第 0 层右下角放一个通往二层的通道。</summary>
public static class UpperRoomBuilder
{
    const string SceneDir = "Assets/Scenes";
    const string GroundFloorScene = SceneDir + "/Shelter_L0.unity";
    const string UpperScene = SceneDir + "/Shelter_2F.unity";

    const float FloorTop = 0f;
    const float RoomLeft = -10f;
    const float RoomRight = 10f;
    const float CeilingHeight = 6f;

    static readonly Color WallColor = new Color(0.30f, 0.32f, 0.37f, 1f);
    static readonly Color FloorColor = new Color(0.72f, 0.74f, 0.78f, 1f);
    static readonly Color SlabColor = new Color(0.45f, 0.47f, 0.52f, 1f);
    static readonly Color PortalColor = new Color(1f, 0.92f, 0.6f, 0.9f);
    static readonly Color NpcColor = new Color(0.86f, 0.88f, 0.95f, 1f);

    [MenuItem("工具/25小时/生成避难所二层（两个房间可来回走）", false, 18)]
    public static void BuildAll()
    {
        BuildUpper();
        AddPortalToGroundFloor();
        Decorate();
        SetupBuildSettings();
        Debug.Log("[二层] 已生成 Shelter_2F，并在第 0 层右下角放了通道");
    }

    // ---------------------------------------------------------------- 二层
    static void BuildUpper()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(CameraRig2D));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.2f;
        cam.backgroundColor = new Color(0.07f, 0.075f, 0.09f, 1f);
        cam.transform.position = new Vector3(0f, 2.3f, -10f);

        Floor("地板", new Vector2(0f, -0.4f), new Vector2(22f, 0.8f));
        Floor("天花板", new Vector2(0f, CeilingHeight + 0.3f), new Vector2(22f, 0.6f));
        Wall("墙_左", new Vector2(RoomLeft - 0.3f, 3f), new Vector2(0.6f, 7f));
        Wall("墙_右", new Vector2(RoomRight + 0.3f, 3f), new Vector2(0.6f, 7f));

        // 二层比一层「更空、更冷」：地板上一道中缝 + 两盏灯的位置留白
        Box("中缝", new Vector2(0f, 0.02f), new Vector2(22f, 0.06f), new Color(0.16f, 0.17f, 0.2f, 1f), -9);

        // 玩家
        var player = new GameObject("玩家_主控", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(ShelterPlayer));
        player.transform.position = new Vector3(-7f, 1.0f, 0f);

        var art = new GameObject("图", typeof(SpriteRenderer));
        art.transform.SetParent(player.transform, false);
        var sr = art.GetComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(0.8f, 1.45f);
        sr.color = new Color(0.86f, 0.30f, 0.26f, 1f);
        sr.sortingOrder = 10;

        var rb = player.GetComponent<Rigidbody2D>();
        rb.gravityScale = 3.5f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var box = player.GetComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, 1.45f);

        var sp = player.GetComponent<ShelterPlayer>();
        sp.spawnPoint = new Vector3(-7f, 1.0f, 0f);
        sp.stepHeight = 0.55f;
        sp.spriteRenderer = sr;
        sp.spriteRoot = art.transform;

        var rig = camGo.GetComponent<CameraRig2D>();
        rig.target = player.transform;
        rig.min = new Vector2(-12f, -1f);
        rig.max = new Vector2(12f, 5.4f);
        rig.smoothTime = 0.08f;

        // 回一层的通道（左侧）
        var back = new GameObject("入口_一层", typeof(RoomPortal));
        back.transform.position = new Vector3(-8.6f, 1.2f, 0f);
        var backPortal = back.GetComponent<RoomPortal>();
        backPortal.targetScene = "Shelter_L0";
        backPortal.spawnObjectName = "到达点_一层";
        backPortal.fadeOut = 0.7f;
        backPortal.blackHold = 2.0f;
        backPortal.fadeIn = 1.0f;

        // 到达点（从一层过来落在这里）
        Marker2("到达点_二层", new Vector3(-6.2f, 1.0f, 0f));

        new GameObject("GameProgress", typeof(GameProgress));
        new GameObject("AppBoot", typeof(AppBoot));

        EditorSceneManager.SaveScene(scene, UpperScene);
    }

    // ------------------------------------------------------- 第 0 层加通道
    static void AddPortalToGroundFloor()
    {
        var scene = EditorSceneManager.OpenScene(GroundFloorScene, OpenSceneMode.Single);

        // 已有的先删掉，避免越加越多
        foreach (var old in Object.FindObjectsOfType<RoomPortal>(true))
            Object.DestroyImmediate(old.gameObject);

        var go = new GameObject("入口_二层", typeof(RoomPortal));
        go.transform.position = new Vector3(11.4f, 1.2f, 0f);
        var portal = go.GetComponent<RoomPortal>();
        portal.targetScene = "Shelter_2F";
        portal.spawnObjectName = "到达点_二层";

        Marker2("到达点_一层", new Vector3(9.9f, 1.0f, 0f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // -------------------------------------------- 白毛 NPC / 通道箭头（第 0 层）
    static void Decorate()
    {
        var scene = EditorSceneManager.OpenScene(GroundFloorScene, OpenSceneMode.Single);

        foreach (var tr in Object.FindObjectsOfType<Transform>(true))
        {
            if (tr.name != "npc" && tr.name != "对话NPC" && !tr.name.StartsWith("NPC")) continue;

            var gate = tr.GetComponent<NpcAfterEvent>();
            if (gate == null) gate = tr.gameObject.AddComponent<NpcAfterEvent>();
            gate.eventId = "event_02";

            var talk = tr.GetComponentInChildren<ChoiceNpc>(true);
            if (talk != null) talk.afterTalkHint = "去探索另一个空间吧";
            Debug.Log("[二层] NPC 已挂钩事件二：" + tr.name);
            break;
        }

        var arrowGo = GameObject.Find("通道指示");
        if (arrowGo == null) arrowGo = new GameObject("通道指示");
        arrowGo.transform.position = new Vector3(10.2f, 2.2f, 0f);
        var arrow = arrowGo.GetComponent<ArrowHint>();
        if (arrow == null) arrow = arrowGo.AddComponent<ArrowHint>();
        arrow.eventId = "event_02";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ------------------------------------------------------------- 工具方法
    static void Floor(string name, Vector2 center, Vector2 size)
    {
        Box(name, center, size, FloorColor, 0);
        var go = GameObject.Find(name);
        go.GetComponent<BoxCollider2D>().enabled = true;
    }

    static void Wall(string name, Vector2 center, Vector2 size)
    {
        Box(name, center, size, WallColor, 1);
        var go = GameObject.Find(name);
        go.GetComponent<BoxCollider2D>().enabled = true;
    }

    static void Box(string name, Vector2 center, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
        go.transform.position = new Vector3(center.x, center.y, 0f);

        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = color;
        sr.sortingOrder = order;

        var box = go.GetComponent<BoxCollider2D>();
        box.size = size;
        box.enabled = false;

        go.layer = LayerMask.NameToLayer("Ground");
    }

    static void Marker2(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
    }

    static void SetupBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string path = UpperScene;
        bool found = false;
        foreach (var s in list) if (s.path == path) found = true;

        if (!found) list.Add(new EditorBuildSettingsScene(path, true));

        // 方便调试：MainMenu 在最前
        list.Sort((a, b) =>
        {
            int Rank(string p)
            {
                if (p.EndsWith("MainMenu.unity")) return 0;
                if (p.EndsWith("Shelter_L0.unity")) return 1;
                if (p.EndsWith("Shelter_2F.unity")) return 2;
                if (p.EndsWith("Event_01.unity")) return 3;
                if (p.EndsWith("Event_02.unity")) return 4;
                return 10;
            }
            return Rank(a.path).CompareTo(Rank(b.path));
        });

        EditorBuildSettings.scenes = list.ToArray();
    }
}
