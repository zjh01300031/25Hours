using UnityEngine;

/// <summary>
/// 给「空物体」补一个看得见的占位：
///   Marker.EnsureBox —— 运行时在物体下面生成一个色块（没贴图也看得见）
///   Marker.Wire      —— Scene 视图里画线框（不按 Play 也看得见）
/// 事件点、收尾点这类纯逻辑物体都用它，省得测试时找不到东西。
/// </summary>
public static class Marker
{
    static Sprite white;

    public static Sprite White
    {
        get
        {
            if (white == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color32[16];
                for (int i = 0; i < px.Length; i++)
                {
                    px[i] = new Color32(255, 255, 255, 255);
                }
                tex.SetPixels32(px);
                tex.Apply();
                tex.filterMode = FilterMode.Point;

                white = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                white.name = "占位白方块";
            }
            return white;
        }
    }

    /// <summary>没有 SpriteRenderer 就补一个色块；返回新加的（已有则返回 null）。</summary>
    public static SpriteRenderer EnsureBox(Transform parent, Color color, float size)
    {
        if (parent == null || parent.GetComponentInChildren<SpriteRenderer>() != null)
        {
            return null;
        }

        var go = new GameObject("占位图");
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(size, size, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = White;
        sr.color = color;
        sr.sortingOrder = 10;
        return sr;
    }

    /// <summary>在 Scene 视图里画个线框（编辑器里也看得到）。</summary>
    public static void Wire(Transform target, Color color, float size)
    {
        if (target == null)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawWireCube(target.position, new Vector3(size, size, 0.1f));
    }
}
