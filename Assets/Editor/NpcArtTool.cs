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

    // 工程被编辑器占着时没法用命令行，这里用一个 flag 文件触发：
    // 放一个 Temp/apply-npc.flag，Unity 回到前台时自动执行一次。
    [InitializeOnLoadMethod]
    static void AutoRunIfRequested()
    {
        string flag = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "apply-npc.flag");
        if (!File.Exists(flag)) return;
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.Log("[NPC] 正在 Play 模式，等停下来再执行");
            return;
        }

        File.Delete(flag);
        EditorApplication.delayCall += () =>
        {
            BuildAll();
            string log = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "apply-npc.log");
            File.WriteAllText(log, System.DateTime.Now.ToString("HH:mm:ss") + " 完成\n");
        };
    }
}
