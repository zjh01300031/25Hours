using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 把 Assets/Art/Characters/npc.png 做成「能按 E 说话的 NPC」预制体。
/// 菜单：工具 / 25小时 / 生成对话 NPC（npc.png）
/// </summary>
public static class NpcArtTool
{
    const string ArtPath = "Assets/Art/Characters/npc.png";
    const string PrefabDir = "Assets/Prefabs";
    const string PrefabPath = PrefabDir + "/对话NPC.prefab";

    const float PixelsPerUnit = 353f;   // 512 高 ÷ 1.45 世界单位

    [MenuItem("工具/25小时/生成对话 NPC（npc.png）", false, 12)]
    public static void BuildAll()
    {
        ConfigureImporter();
        BuildPrefab();
    }

    public static void ConfigureImporter()
    {
        var importer = AssetImporter.GetAtPath(ArtPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[NPC] 没有找到贴图：" + ArtPath);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.Tight;
        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
        Debug.Log("[NPC] 贴图设置已改：PPU " + PixelsPerUnit + "，底部中心对齐");
    }

    public static void BuildPrefab()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        if (sprite == null)
        {
            Debug.LogError("[NPC] 贴图不是 Sprite：" + ArtPath);
            return;
        }

        var go = new GameObject("对话NPC");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = 5;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.6f;
        col.offset = new Vector2(0f, 0.75f);

        var talk = go.AddComponent<SimpleTextTrigger>();
        talk.displayName = "白发少女";
        talk.verb = "对话";
        talk.speaker = "白发少女";
        talk.lines = new[] { "……你来了。" };

        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0) go.layer = layer;
        else Debug.LogWarning("[NPC] 没有 Interactable 层，NPC 现在不会响应 E。");

        Directory.CreateDirectory(PrefabDir);
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();

        Debug.Log("[NPC] 预制体已生成：" + PrefabPath + "（拖进场景，按 E 就会说话）");
    }

    // ===== 把场景里「只有一张图」的 npc 补成能交互的 NPC =====

    [MenuItem("工具/25小时/修好场景里的 NPC（补碰撞体与对话）", false, 13)]
    public static void RepairSceneNpc()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            Debug.LogError("[NPC] 当前场景还没保存，先 Ctrl+S 再点这个菜单。");
            return;
        }

        var arr = Object.FindObjectsOfType<Transform>(true);
        int fixedCount = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            var t = arr[i];
            // 用 npc.png 画的物体，但还没挂交互脚本
            if (t.GetComponent<InteractableBase>() != null) continue;
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;
            if (AssetDatabase.GetAssetPath(sr.sprite.texture) != ArtPath) continue;

            MakeInteractive(t.gameObject);
            fixedCount++;
            Debug.Log("[NPC] 已补好：" + t.name + "（" + t.position + "）");
        }

        if (fixedCount == 0)
        {
            Debug.LogWarning("[NPC] 场景里没找到需要修的 npc 图片物体。直接把 对话NPC.prefab 拖进来就行。");
            return;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[NPC] 修好 " + fixedCount + " 个，场景已保存。");
    }

    static void MakeInteractive(GameObject go)
    {
        if (go.GetComponent<CircleCollider2D>() == null)
        {
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;
            col.offset = new Vector2(0f, 0.75f);
        }

        var talk = go.AddComponent<SimpleTextTrigger>();
        talk.displayName = "白发少女";
        talk.verb = "对话";
        talk.speaker = "白发少女";
        talk.lines = new[] { "……你来了。" };

        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0) go.layer = layer;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 5;
    }

    // 工程被编辑器占着时没法用命令行，这里用 flag 文件触发：
    // Temp/apply-npc.flag → 重新生成预制体；Temp/add-npc.flag → 修当前场景里的 npc。
    [InitializeOnLoadMethod]
    static void AutoRunIfRequested()
    {
        string root = Directory.GetCurrentDirectory();
        string flagAll = Path.Combine(root, "Temp", "apply-npc.flag");
        string flagFix = Path.Combine(root, "Temp", "add-npc.flag");
        bool doAll = File.Exists(flagAll);
        bool doFix = File.Exists(flagFix);
        if (!doAll && !doFix) return;

        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.Log("[NPC] 正在 Play 模式，等停下来再执行");
            return;
        }

        if (doAll) File.Delete(flagAll);
        if (doFix) File.Delete(flagFix);

        EditorApplication.delayCall += () =>
        {
            if (doAll) BuildAll();
            if (doFix) RepairSceneNpc();
            string log = Path.Combine(root, "Temp", "apply-npc.log");
            File.WriteAllText(log, System.DateTime.Now.ToString("HH:mm:ss") + " 完成\n");
        };
    }
}
