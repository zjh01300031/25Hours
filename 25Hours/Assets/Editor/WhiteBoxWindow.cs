using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 白盒工具箱：重建、打开场景、以及两个手动微调的小工具（加平台 / 对齐网格）。
/// 打开方式：菜单「工具 / 25小时 / 白盒工具箱」。
/// </summary>
public class WhiteBoxWindow : EditorWindow
{
    Vector2 platformSize = new Vector2(2f, 0.5f);
    float grid = 0.25f;

    [MenuItem("工具/25小时/白盒工具箱", false, 0)]
    public static void Open()
    {
        var window = GetWindow<WhiteBoxWindow>("白盒工具箱");
        window.minSize = new Vector2(340f, 320f);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("第 0 层白盒 · 一键操作", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("重建会覆盖 Assets/Scenes/Shelter_L0.unity、Event_01.unity、Event_02.unity（美术占位图也会重画）。" +
                                "\n手动摆的东西请放在自己新建的场景里，或者先备份。", MessageType.Warning);

        if (GUILayout.Button("重建第0层（场景 + 占位图 + UI）", GUILayout.Height(30f)))
        {
            Level0Builder.BuildAll();
        }

        if (GUILayout.Button("只截图预览（世界）"))
        {
            Level0Builder.CapturePreviews();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("打开 避难所 1 区"))
            {
                EditorSceneManager.OpenScene(Level0Builder.ShelterScenePath);
            }
            if (GUILayout.Button("打开 事件一"))
            {
                EditorSceneManager.OpenScene(Level0Builder.Event01ScenePath);
            }
            if (GUILayout.Button("打开 事件二"))
            {
                EditorSceneManager.OpenScene(Level0Builder.Event02ScenePath);
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("手动微调小工具", EditorStyles.boldLabel);

        grid = EditorGUILayout.FloatField("网格大小", grid);
        if (GUILayout.Button("把选中物体对齐到网格"))
        {
            SnapSelection();
        }

        platformSize = EditorGUILayout.Vector2Field("平台尺寸（宽, 高）", platformSize);
        if (GUILayout.Button("在选中物体位置加一块可站立平台"))
        {
            AddPlatform();
        }

        EditorGUILayout.HelpBox("加出来的平台只有碰撞体（Ground 层），没有外观。\n" +
                                "想要看起来也像块砖，就从 01_建筑 里复制一个方块，改它的 BoxCollider2D 尺寸。", MessageType.Info);
    }

    static void SnapSelection()
    {
        if (Selection.transforms.Length == 0)
        {
            Debug.LogWarning("[白盒] 先在 Hierarchy / Scene 里选中物体");
            return;
        }

        foreach (var t in Selection.transforms)
        {
            Undo.RecordObject(t, "对齐网格");
            var p = t.position;
            t.position = new Vector3(Mathf.Round(p.x / 0.25f) * 0.25f, Mathf.Round(p.y / 0.25f) * 0.25f, p.z);
        }
    }

    void AddPlatform()
    {
        var selected = Selection.activeTransform;
        if (selected == null)
        {
            Debug.LogWarning("[白盒] 先选中一个物体，平台会加在它的位置");
            return;
        }

        var go = new GameObject("平台_" + selected.name);
        Undo.RegisterCreatedObjectUndo(go, "加平台");
        go.transform.SetParent(selected.parent, false);
        go.transform.position = selected.position;
        go.layer = LayerMask.NameToLayer("Ground");

        var box = go.AddComponent<BoxCollider2D>();
        box.size = platformSize;

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("[白盒] 已加平台 " + go.name + " 尺寸 " + platformSize);
    }
}
