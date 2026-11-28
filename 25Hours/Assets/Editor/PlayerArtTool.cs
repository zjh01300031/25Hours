using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 把主控的占位方块换成 Assets/Art/Characters/player.png：
/// 1. 改贴图导入设置（Sprite / PPU 353 / BottomCenter / Tight / 不压缩）
/// 2. 写进 玩家.prefab
/// 3. 写进 Shelter_L0 / level01 / level02 里的玩家
/// 菜单：工具 / 25小时 / 把主控换成 player.png
/// </summary>
public static class PlayerArtTool   // touched 204258
{
    const string ArtPath = "Assets/Art/Characters/player.png";
    const string PrefabPath = "Assets/Prefabs/玩家.prefab";
    const float PixelsPerUnit = 353f;     // 512px 高 / 1.45 世界单位
    const float FeetOffsetY = -0.725f;    // 碰撞盒 1.45 高，底面在 -0.725

    static readonly string[] Scenes =
    {
        "Assets/Scenes/Shelter_L0.unity",
        "Assets/Scenes/level01.unity",
        "Assets/Scenes/level02.unity",
    };

    [MenuItem("工具/25小时/把主控换成 player.png _F9", false, 70)]
    public static void Apply()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Art] 正在 Play 模式，先按一下 ▶ 停止，再执行（图会自动换上）。");
            return;
        }

        ConfigureImporter();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath);
        if (sprite == null)
        {
            Debug.LogError("[Art] 读不到 " + ArtPath + "（确认 Texture Type 是 Sprite）");
            return;
        }

        if (File.Exists(PrefabPath))
        {
            var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            ApplyTo(contents, sprite);
            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
            Debug.Log("[Art] 已更新 " + PrefabPath);
        }
        else
        {
            Debug.LogWarning("[Art] 没找到 " + PrefabPath + "，跳过预制体");
        }

        for (int i = 0; i < Scenes.Length; i++)
        {
            if (!File.Exists(Scenes[i]))
            {
                Debug.LogWarning("[Art] 场景不存在：" + Scenes[i]);
                continue;
            }

            var scene = EditorSceneManager.OpenScene(Scenes[i], OpenSceneMode.Single);
            var players = Object.FindObjectsOfType<ShelterPlayer>();
            for (int p = 0; p < players.Length; p++)
            {
                ApplyTo(players[p].gameObject, sprite);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Art] " + Path.GetFileName(Scenes[i]) + "：改了 " + players.Length + " 个玩家");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Art] 主控立绘已应用（贴图 + 预制体 + 3 个场景）");
    }

    static void ApplyTo(GameObject root, Sprite sprite)
    {
        SpriteRenderer target = null;

        var named = root.transform.Find("图");
        if (named != null)
        {
            target = named.GetComponent<SpriteRenderer>();
            if (target == null)
            {
                target = named.gameObject.AddComponent<SpriteRenderer>();
            }
        }

        if (target == null)
        {
            var all = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].gameObject != root)
                {
                    target = all[i];
                    break;
                }
            }
        }

        if (target == null)
        {
            // 根上只有渲染器：新建一个「图」子物体来放立绘，根上的方块关掉
            var go = new GameObject("图");
            go.transform.SetParent(root.transform, false);
            target = go.AddComponent<SpriteRenderer>();
        }

        target.sprite = sprite;
        target.color = Color.white;
        target.sortingOrder = 10;
        target.drawMode = SpriteDrawMode.Simple;
        target.transform.localScale = Vector3.one;

        var pos = target.transform.localPosition;
        pos.x = 0f;
        pos.y = FeetOffsetY;
        target.transform.localPosition = pos;

        var rootSr = root.GetComponent<SpriteRenderer>();
        if (rootSr != null && rootSr != target)
        {
            rootSr.enabled = false;
        }
    }

    static void ConfigureImporter()
    {
        var importer = AssetImporter.GetAtPath(ArtPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[Art] 贴图导入器读不到：" + ArtPath);
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
        Debug.Log("[Art] 导入设置已改：Sprite / PPU " + PixelsPerUnit + " / BottomCenter / Tight / 不压缩");
    }

    // 批处理用不了（工程被编辑器占用）时，靠这个自动跑一次：
    // 在 <工程>/Temp/apply-art.flag 放个空文件，编辑器下次重编译就会自动执行。
    [InitializeOnLoadMethod]
    static void AutoRunIfRequested()
    {
        string flag = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Temp", "apply-art.flag");
        if (!File.Exists(flag))
        {
            return;
        }

        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;   // 退出 Play 后（会重载域）再跑
        }

        File.Delete(flag);
        EditorApplication.delayCall += Apply;
    }
}
