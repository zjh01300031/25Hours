using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 以选中的第一级台阶为样板，复制出一整段楼梯。
/// 台阶只是视觉，真正能走的是「楼梯_斜坡」那个隐形碰撞体。
/// </summary>
public static class StepBuilder
{
    const int TotalSteps = 12;

    [MenuItem("工具/25小时/用选中方块生成 12 级台阶", false, 60)]
    static void BuildSteps()
    {
        var first = Selection.activeGameObject;
        if (first == null)
        {
            EditorUtility.DisplayDialog("生成台阶",
                "先在 Hierarchy 里点一下你建的第一级台阶（那个 0.5 x 0.5 的方块），再点这个菜单。", "好");
            return;
        }

        var t0 = first.transform;
        float w = Mathf.Abs(t0.lossyScale.x);
        float h = Mathf.Abs(t0.lossyScale.y);
        if (w < 0.01f || h < 0.01f)
        {
            EditorUtility.DisplayDialog("生成台阶", "这个物件的 Scale 太小了，看着不像台阶。先确认选中的是 0.5 x 0.5 那一级。", "好");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(first, "生成台阶");

        var parent = t0.parent;
        var pos0 = t0.localPosition;
        var rot0 = t0.localRotation;
        var scl0 = t0.localScale;
        var baseName = Regex.Replace(first.name, @"\s*[\(（]\d+[\)）]\s*$", "");

        for (int i = 1; i < TotalSteps; i++)
        {
            var go = Object.Instantiate(first, parent);
            Undo.RegisterCreatedObjectUndo(go, "生成台阶");
            go.name = baseName + " (" + (i + 1) + ")";
            var tr = go.transform;
            tr.localPosition = pos0 + new Vector3(-w * i, h * i, 0f);
            tr.localRotation = rot0;
            tr.localScale = scl0;
        }

        EditorSceneManager.MarkSceneDirty(first.scene);
        Debug.Log("[25Hours] 已按 " + first.name + " 生成 " + TotalSteps + " 级台阶，每级向左 " + w + "、向上 " + h + "。Ctrl + S 保存。");
    }

    [MenuItem("工具/25小时/给楼梯加隐形斜面（楼梯_斜坡）", false, 61)]
    static void BuildRamp()
    {
        var first = Selection.activeGameObject;
        if (first == null)
        {
            EditorUtility.DisplayDialog("楼梯斜面", "先选中第一级台阶（最下面那一级），我按它的尺寸算整段楼梯的斜面。", "好");
            return;
        }

        var t0 = first.transform;
        float w = Mathf.Abs(t0.lossyScale.x);
        float h = Mathf.Abs(t0.lossyScale.y);
        var parent = t0.parent;
        var pos0 = t0.localPosition;

        // 台阶顶面连成的那条线：从第一级顶到第十二级顶
        var p0 = new Vector2(pos0.x, pos0.y + h * 0.5f);
        var p1 = new Vector2(pos0.x - w * (TotalSteps - 1), pos0.y + h * 0.5f + h * (TotalSteps - 1));
        var delta = p1 - p0;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        // 法线朝上，把碰撞体往下压半个厚度，让它的上表面正好贴住那条线
        var normal = new Vector2(-delta.y, delta.x).normalized;
        if (normal.y < 0f) normal = -normal;
        const float thickness = 0.4f;
        var center = (p0 + p1) * 0.5f - normal * (thickness * 0.5f);

        var go = new GameObject("楼梯_斜坡");
        Undo.RegisterCreatedObjectUndo(go, "楼梯斜面");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(center.x, center.y, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(delta.magnitude + w, thickness);

        int ground = LayerMask.NameToLayer("Ground");
        go.layer = ground >= 0 ? ground : 8;

        EditorSceneManager.MarkSceneDirty(first.scene);
        Debug.Log("[25Hours] 已生成 楼梯_斜坡：中心 (" + center.x.ToString("0.00") + ", " + center.y.ToString("0.00") +
                  ")，角度 " + angle.ToString("0.0") + "°，长度 " + (delta.magnitude + w).ToString("0.00") +
                  "，Layer = Ground。Ctrl + S 保存。");
    }
}
