using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 一键：导出「收集检查点」预制体，并把它放进第 0 层（Shelter_L0）。
/// 菜单：工具 / 25小时 / 生成收集检查点（第0层）
/// </summary>
public static class CollectionGateTool
{
    const string PrefabPath = "Assets/Prefabs/收集检查点.prefab";
    const string ShelterPath = "Assets/Scenes/Shelter_L0.unity";

    [MenuItem("工具/25小时/生成收集检查点（第0层）", false, 18)]
    public static void Build()
    {
        ExportPrefab();
        PlaceInShelter();
        AssetDatabase.SaveAssets();
        Debug.Log("[收集检查点] 完成：预制体 " + PrefabPath + "，并已放进 Shelter_L0");
    }

    static void ExportPrefab()
    {
        Directory.CreateDirectory("Assets/Prefabs");

        var go = new GameObject("收集检查点");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(1.2f, 1.6f);
        sr.color = new Color(0.72f, 0.86f, 0.62f, 1f);
        sr.sortingOrder = 4;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.9f;

        var gate = go.AddComponent<CollectionGate>();
        gate.displayName = "收集检查点";
        gate.group = "A";
        gate.eventId = "event_01";
        gate.sceneName = "Event_01";
        gate.readyText = "东西齐了。";

        go.layer = LayerMask.NameToLayer("Interactable");

        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
    }

    static void PlaceInShelter()
    {
        var current = SceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(ShelterPath, OpenSceneMode.Single);

        // 已经有了就不重复放
        foreach (var g in Object.FindObjectsOfType<CollectionGate>(true))
        {
            if (g != null)
            {
                Debug.Log("[收集检查点] 场景里已经有：" + g.name + "，跳过摆放");
                EditorSceneManager.SaveScene(scene, ShelterPath);
                if (!string.IsNullOrEmpty(current) && File.Exists(current)) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
                return;
            }
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "收集检查点";
        instance.transform.position = new Vector3(3f, 0.9f, 0f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ShelterPath);

        if (!string.IsNullOrEmpty(current) && File.Exists(current)) EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
    }

    [InitializeOnLoadMethod]
    static void AutoRunIfRequested()
    {
        var flag = Path.Combine(Directory.GetCurrentDirectory(), "Temp/collection-gate.flag");
        if (!File.Exists(flag)) return;
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

        File.Delete(flag);
        EditorApplication.delayCall += Build;
    }
}
