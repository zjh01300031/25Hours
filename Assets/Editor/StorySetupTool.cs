using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 一次性把工程改成新流程：删掉 level01 / level02，第 0 层加收集进度与背包，
/// NPC 换成多选项，交互键改成空格，并导出「收集物」预制体。
/// 菜单：工具 / 25小时 / 应用新的收集流程
/// </summary>
public static class StorySetupTool
{
    const string ShelterScene = "Assets/Scenes/Shelter_L0.unity";
    const string PrefabPath = "Assets/Prefabs/收集物.prefab";
    const string FontPath = "Assets/Fonts/SimHei.ttf";

    [MenuItem("工具/25小时/应用新的收集流程", false, 5)]
    public static void ApplyAll()
    {
        DeleteOldScenes();
        FixBuildSettings();
        FixScene();
        BuildCollectiblePrefab();
        Debug.Log("[Setup] 新的收集流程已应用：交互=空格、无跳跃、Q=背包、NPC=多选项");
    }

    static void DeleteOldScenes()
    {
        string[] scenes = { "Assets/Scenes/level01.unity", "Assets/Scenes/level02.unity" };
        for (int i = 0; i < scenes.Length; i++)
        {
            if (File.Exists(scenes[i]) || File.Exists(scenes[i] + ".meta"))
            {
                AssetDatabase.DeleteAsset(scenes[i]);
                Debug.Log("[Setup] 已删除场景：" + scenes[i]);
            }
        }
    }

    static void FixBuildSettings()
    {
        var keep = new[] { ShelterScene, "Assets/Scenes/Event_01.unity", "Assets/Scenes/Event_02.unity" };
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        for (int i = 0; i < keep.Length; i++)
        {
            if (File.Exists(keep[i])) list.Add(new EditorBuildSettingsScene(keep[i], true));
        }
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[Setup] Build Settings：" + string.Join(" / ", System.Array.ConvertAll(list.ToArray(), s => Path.GetFileNameWithoutExtension(s.path))));
    }

    static void FixScene()
    {
        if (File.Exists(ShelterScene))
        {
            EditorSceneManager.OpenScene(ShelterScene, OpenSceneMode.Single);
        }

        var scene = SceneManager.GetActiveScene();
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        // 玩家：关跳跃、交互键换成空格
        var player = Object.FindObjectOfType<ShelterPlayer>();
        if (player != null)
        {
            player.allowJump = false;

            var interactor = player.GetComponent<ShelterInteractor>();
            if (interactor != null) interactor.interactKey = KeyCode.Space;
            Debug.Log("[Setup] 玩家已改：不能跳，交互键=空格");
        }
        else
        {
            Debug.LogWarning("[Setup] 场景里没找到玩家");
        }

        // NPC：SimpleTextTrigger → ChoiceNpc
        var npc = GameObject.Find("npc");
        if (npc != null)
        {
            var old = npc.GetComponent<SimpleTextTrigger>();
            if (old != null) Object.DestroyImmediate(old);

            var choice = npc.GetComponent<ChoiceNpc>();
            if (choice == null) choice = npc.AddComponent<ChoiceNpc>();
            choice.displayName = "白发少女";
            choice.verb = "对话";
            choice.speaker = "白发少女";
            choice.font = font;
            if (choice.options == null || choice.options.Length == 0)
            {
                choice.options = new ChoiceNpc.Option[0];
            }
            Debug.Log("[Setup] NPC 已换成多选项（选项在 Inspector 里填）");
        }
        else
        {
            Debug.LogWarning("[Setup] 场景里没有名为 npc 的物体");
        }

        // 收集进度
        var quest = Object.FindObjectOfType<CollectionQuest>();
        if (quest == null)
        {
            var go = new GameObject("收集进度");
            quest = go.AddComponent<CollectionQuest>();
            Debug.Log("[Setup] 已加 收集进度（CollectionQuest）");
        }
        quest.groupA = "A";
        quest.eventA = "event_01";
        quest.sceneA = "Event_01";
        quest.groupB = "B";
        quest.eventB = "event_02";
        quest.sceneB = "Event_02";

        // 背包
        var inventory = Object.FindObjectOfType<InventoryUI>();
        if (inventory == null)
        {
            var go = new GameObject("背包");
            inventory = go.AddComponent<InventoryUI>();
        }
        inventory.font = font;
        inventory.toggleKey = KeyCode.Q;
        Debug.Log("[Setup] 背包界面已就绪（Q 打开）");

        FixHelpText();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void FixHelpText()
    {
        var help = GameObject.Find("操作提示");
        if (help == null) { Debug.LogWarning("[Setup] 没找到 操作提示"); return; }

        var text = help.GetComponent<UnityEngine.UI.Text>();
        if (text != null)
        {
            text.text = "A / D 移动　　空格 交互　　　Q 背包";
            Debug.Log("[Setup] 操作提示已改成：A/D 移动、空格 交互、Q 背包");
        }
    }

    static void BuildCollectiblePrefab()
    {
        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var go = new GameObject("收集物");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.96f, 0.82f, 0.42f, 1f);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(0.6f, 0.6f);
        sr.sortingOrder = 5;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var item = go.AddComponent<Collectible>();
        item.displayName = "物品";
        item.verb = "拾取";
        item.itemId = "item_01";
        item.group = "A";
        item.requiresEventId = "";

        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0) go.layer = layer;

        Directory.CreateDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] 收集物预制体：" + PrefabPath);
    }

    // 工程被编辑器占用时用 flag 触发：Temp/story-setup.flag
    [InitializeOnLoadMethod]
    static void AutoRunIfRequested()
    {
        string root = Directory.GetCurrentDirectory();
        string flag = Path.Combine(root, "Temp", "story-setup.flag");
        if (!File.Exists(flag)) return;
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

        File.Delete(flag);
        EditorApplication.delayCall += () =>
        {
            ApplyAll();
            File.WriteAllText(Path.Combine(root, "Temp", "story-setup.log"),
                System.DateTime.Now.ToString("HH:mm:ss") + " 完成\n");
        };
    }
}
