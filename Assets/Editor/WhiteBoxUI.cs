using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>生成 UI 的小工具：Canvas、面板、文字、按钮。</summary>
public static class WhiteBoxUI
{
    public static Font Font;
    public static Sprite PanelSprite;
    public static Sprite WhiteSprite;

    public static void Load()
    {
        Font = AssetDatabase.LoadAssetAtPath<Font>(WhiteBoxArt.FontAssetPath);
        PanelSprite = WhiteBoxArt.Square;
        WhiteSprite = WhiteBoxArt.Square;

        if (Font == null) Debug.LogError("[WhiteBoxUI] 中文字体没有导入，UI 会缺字");
    }

    public static Canvas MakeCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform Node(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color color)
    {
        var rect = Node(name, parent, anchor, pivot, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = PanelSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    public static Text Label(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, string content, int fontSize, TextAnchor alignment, Color color)
    {
        var rect = Node(name, parent, anchor, pivot, position, size);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = Font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.lineSpacing = 1.15f;

        var shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    public static Image Stretch(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.sprite = WhiteSprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static Button Button(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color color, out Image image)
    {
        var rect = Node(name, parent, anchor, pivot, position, size);
        image = rect.gameObject.AddComponent<Image>();
        image.sprite = WhiteSprite;
        image.color = color;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }
}
