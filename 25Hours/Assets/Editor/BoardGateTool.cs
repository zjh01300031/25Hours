using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 棋盘入口的两件事：导出一个预制体，以及把它放进当前打开的场景。
/// </summary>
public static class BoardGateTool
{
    const string PrefabPath = "Assets/Prefabs/棋盘入口.prefab";

    [MenuItem("工具/25小时/导出棋盘入口预制体", false, 14)]
    public static void ExportPrefab()
    {
        var go = BuildObject();
        System.IO.Directory.CreateDirectory("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.Refresh();
        Debug.Log("[入口] 已导出：" + PrefabPath);
    }

    [MenuItem("工具/25小时/把棋盘入口放进当前场景", false, 15)]
    public static void PlaceInScene()
    {
        var go = BuildObject();
        go.transform.position = new Vector3(3f, 0.8f, 0f);
        Undo.RegisterCreatedObjectUndo(go, "放置棋盘入口");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Debug.Log("[入口] 已放进当前场景（位置 3, 0.8），记得 Ctrl+S");
    }

    static GameObject BuildObject()
    {
        var go = new GameObject("棋盘入口");
        go.layer = LayerMask.NameToLayer("Interactable");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(1.1f, 1.6f);
        sr.color = new Color(0.55f, 0.78f, 0.95f, 1f);
        sr.sortingOrder = 3;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.9f;

        go.AddComponent<BoardGate>();
        return go;
    }
}