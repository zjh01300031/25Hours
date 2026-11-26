using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用的「运行时搭一个界面」小工具：不依赖场景里预先摆好的 UI，
/// 组件里给一个 Font 就能自己画面板。
/// </summary>
public static class RuntimeUI
{
    public static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    public static RectTransform Panel(Transform parent, string name, Vector2 size,
        Vector2 anchoredPosition, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return rect;
    }

    public static Text Label(Transform parent, string name, string text, int fontSize,
        TextAnchor anchor, Color color, Font font, Vector2 size, Vector2 anchoredPosition)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        var label = go.GetComponent<Text>();
        label.text = text;
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = anchor;
        label.color = color;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        return label;
    }

    /// <summary>竖直滚动的选项列表：返回每一行的 Text，外面控制选中高亮。</summary>
    public static List<Text> Rows(Transform parent, Font font, int fontSize, float rowHeight,
        float width, float topY)
    {
        var list = new List<Text>();
        for (int i = 0; i < 12; i++)
        {
            var row = Label(parent, "行_" + i, "", fontSize, TextAnchor.MiddleLeft, Color.white, font,
                new Vector2(width, rowHeight), new Vector2(0f, topY - rowHeight * i));
            list.Add(row);
            row.gameObject.SetActive(false);
        }
        return list;
    }
}
