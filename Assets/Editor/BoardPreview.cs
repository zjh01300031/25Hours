using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 编辑模式下的静态预览：把棋盘的样子摆进场景（不会玩、只是看），
/// 这样不按 ▶ 也能在 Game / Scene 视图里看到格子、技能条、覆盖率。
/// 进入 Play 时 PuzzleController 会先删掉这些预览物件，再画真正可交互的那套。
/// </summary>
public static class BoardPreview
{
    const string FontPath = "Assets/Fonts/SimHei.ttf";

    public static void Build(PuzzleController board)
    {
        Remove();
        if (board == null) return;

        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGo = new GameObject("棋盘预览(编辑用)", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(BoardPreviewMarker));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 49;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Board(board, font, canvasGo.transform);
    }

    static void Board(PuzzleController c, Font font, Transform root)
    {
        Panel(root, "背景", new Vector2(4000f, 3000f), Vector2.zero, c.frameColor);
        Label(root, "覆盖率", "行动 " + (c.MoveLimit > 0 ? c.MoveLimit + " / " + c.MoveLimit : "不限") + "　·　点亮 0 / " + (c.width * c.height), 28, font, c.accentColor, new Vector2(1200f, 44f), new Vector2(0f, 470f));

        float step = c.cellSize + c.gap;
        var grid = new GameObject("格子系统", typeof(RectTransform));
        grid.transform.SetParent(root, false);
        var gr = grid.GetComponent<RectTransform>();
        gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 0.5f);
        gr.pivot = new Vector2(0.5f, 0.5f);
        gr.sizeDelta = new Vector2(c.width * step, c.height * step);
        gr.anchoredPosition = new Vector2(0f, 60f);

        for (int y = 0; y < c.height; y++)
        {
            for (int x = 0; x < c.width; x++)
            {
                var pos = new Vector2((x - (c.width - 1) * 0.5f) * step, (y - (c.height - 1) * 0.5f) * step);
                var cell = Panel(grid.transform, "格_" + x + "_" + y, new Vector2(c.cellSize, c.cellSize), pos, c.darkColor);
                Label(cell, "字", "", 30, font, new Color(0.85f, 0.87f, 0.9f, 0.55f), new Vector2(c.cellSize, c.cellSize), Vector2.zero);
            }
        }

        // 小人站中间
        var avatar = Panel(grid.transform, "小人", new Vector2(c.cellSize * 0.5f, c.cellSize * 0.72f), new Vector2(0f, c.cellSize * 0.3f), Color.white);
        var avatarImage = avatar.GetComponent<Image>();
        avatarImage.sprite = c.avatarSprite;
        avatarImage.type = Image.Type.Simple;
        avatarImage.color = new Color(0.9f, 0.33f, 0.28f, 1f);

        // 技能条
        var names = new[] { "微光", "回响", "共振" };
        var charges = new[] { 6, 4, 2 };
        float totalWidth = names.Length * 230f + (names.Length - 1) * 16f;
        for (int i = 0; i < names.Length; i++)
        {
            float x = -totalWidth * 0.5f + 115f + i * 246f;
            var slot = Panel(root, "技能_" + names[i], new Vector2(230f, 76f), new Vector2(x, -350f), new Color(0.22f, 0.25f, 0.31f, 1f));
            Icon(slot, i, c.accentColor);
            Label(slot, "次", "× " + charges[i], 24, font, c.accentColor, new Vector2(220f, 30f), new Vector2(0f, -16f));
        }

        Panel(root, "重置", new Vector2(170f, 56f), new Vector2(820f, -350f), new Color(0.22f, 0.25f, 0.31f, 1f));
        Label(root, "重置字", "重置 (R)", 26, font, c.textColor, new Vector2(170f, 56f), new Vector2(820f, -350f));
        Label(root, "提示", "WASD 移动：换方向走 1 格 = 1 次行动，同向连走免费　·　1-6 选技能 · ←→↑↓ 翻转 · 空格 释放 · Z 撤销 · Esc 退出", 24, font,
            new Color(0.7f, 0.72f, 0.75f), new Vector2(1400f, 40f), new Vector2(0f, -430f));
    }

    static void Icon(Transform slot, int index, Color on)
    {
        bool[,] grid = new bool[3, 3];
        switch (index)
        {
            case 0: grid[1, 1] = true; break;
            case 1:
                grid[1, 1] = grid[1, 0] = grid[1, 2] = grid[0, 1] = grid[2, 1] = true; break;
            default:
                for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) grid[x, y] = true; break;
        }

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                bool lit = grid[x + 1, y + 1];
                Panel(slot, "点", new Vector2(16f, 16f), new Vector2(x * 20f, 8f + y * 20f),
                    lit ? on : new Color(0.45f, 0.48f, 0.54f, 0.35f));
            }
        }
    }

    static RectTransform Panel(Transform parent, string name, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    static Text Label(Transform parent, string name, string text, int size, Font font, Color color, Vector2 box, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = box;
        rect.anchoredPosition = pos;
        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public static void Remove()
    {
        foreach (var m in Object.FindObjectsOfType<BoardPreviewMarker>(true))
            Object.DestroyImmediate(m.gameObject);
    }
}