using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 投人偶之后、真正进棋盘之前的那段文案演出。
/// 一页一行字，像 PPT 那样淡入、停顿，按空格/回车/左键翻页；最后一页按完就执行回调（通常去切场景）。
/// 文案写在 BoardGate 的 Intro Slides 里；不写就用根据入口标题自动拼的默认文案。
/// </summary>
public class BoardIntroUI : MonoBehaviour
{
    public static BoardIntroUI Instance { get; private set; }

    public static bool IsPlaying
    {
        get { return Instance != null && Instance.playing; }
    }

    public static BoardIntroUI Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("开场文案");
        return go.AddComponent<BoardIntroUI>();
    }

    // ---------------- 运行时状态 ----------------

    Canvas canvas;
    CanvasGroup group;
    Text titleText;
    Text bodyText;
    Text footerText;
    Font font;

    bool playing;
    bool advance;
    int index;
    float shownAt;
    float holdSeconds = 0.9f;
    List<string[]> pages = new List<string[]>();
    Action onDone;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>播一串文案，播完调 onDone。一行一页；「标题|正文」会把第一段当标题。</summary>
    public void Play(string[] lines, Action done)
    {
        if (playing) return;

        onDone = done;
        pages.Clear();
        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;
                var raw = lines[i].Replace("\r", "");
                int bar = raw.IndexOf('|');
                if (bar >= 0)
                    pages.Add(new[] { raw.Substring(0, bar).Trim(), raw.Substring(bar + 1).Trim() });
                else
                    pages.Add(new[] { "", raw.Trim() });
            }
        }

        if (pages.Count == 0) { if (done != null) done(); return; }

        Build();
        StartCoroutine(Run());
    }

    void Build()
    {
        if (canvas != null) return;

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var simhei = Font.CreateDynamicFontFromOSFont(new[] { "SimHei", "Microsoft YaHei" }, 32);
        if (simhei != null) font = simhei;

        canvas = RuntimeUI.CreateCanvas("开场文案", 75);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        var backdrop = RuntimeUI.Panel(canvas.transform, "黑幕", new Vector2(1920f, 1080f), Vector2.zero,
            new Color(0.03f, 0.035f, 0.045f, 0.97f));
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = Vector2.zero;
        backdrop.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().raycastTarget = false;

        titleText = RuntimeUI.Label(canvas.transform, "标题", "", 64,
            TextAnchor.MiddleCenter, new Color(0.98f, 0.94f, 0.86f), font,
            new Vector2(1500f, 120f), new Vector2(0f, 120f));
        titleText.raycastTarget = false;

        bodyText = RuntimeUI.Label(canvas.transform, "正文", "", 34,
            TextAnchor.UpperCenter, new Color(0.88f, 0.90f, 0.94f), font,
            new Vector2(1320f, 520f), new Vector2(0f, -80f));
        bodyText.raycastTarget = false;
        bodyText.lineSpacing = 1.5f;

        footerText = RuntimeUI.Label(canvas.transform, "页脚", "", 24,
            TextAnchor.MiddleCenter, new Color(0.62f, 0.66f, 0.72f), font,
            new Vector2(1500f, 40f), new Vector2(0f, -430f));
        footerText.raycastTarget = false;

        canvas.enabled = false;
    }

    IEnumerator Run()
    {
        playing = true;
        advance = false;
        canvas.enabled = true;
        group.alpha = 0f;

        // 先压黑
        float t = 0f;
        while (t < 0.28f)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / 0.28f);
            yield return null;
        }
        group.alpha = 1f;

        for (index = 0; index < pages.Count; index++)
            yield return ShowPage(pages[index]);

        // 收尾淡出
        t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / 0.3f);
            yield return null;
        }
        group.alpha = 0f;
        canvas.enabled = false;
        playing = false;

        var callback = onDone;
        onDone = null;
        if (callback != null) callback();
    }

    IEnumerator ShowPage(string[] page)
    {
        string title = page[0];
        string body = page[1];

        titleText.text = title;
        bodyText.text = body;
        footerText.text = "空格 / 回车 / 左键　翻页　　·　　" + (index + 1) + " / " + pages.Count;

        // 标题从下方 24px 处淡入，正文稍晚一点淡入 —— 就是那种 PPT 逐条出来的感觉
        var titleRect = titleText.rectTransform;
        Vector2 titleHome = titleRect.anchoredPosition;
        titleRect.anchoredPosition = titleHome + new Vector2(0f, -24f);
        SetAlpha(titleText, 0f);
        SetAlpha(bodyText, 0f);
        SetAlpha(footerText, 0f);

        float t = 0f;
        while (t < 0.42f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.42f);
            titleRect.anchoredPosition = Vector2.Lerp(titleHome + new Vector2(0f, -24f), titleHome, Ease(k));
            SetAlpha(titleText, k);
            yield return null;
        }
        titleRect.anchoredPosition = titleHome;
        SetAlpha(titleText, 1f);

        t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01((t - 0.12f) / 0.38f);
            SetAlpha(bodyText, k);
            SetAlpha(footerText, k);
            yield return null;
        }
        SetAlpha(bodyText, 1f);
        SetAlpha(footerText, 1f);

        shownAt = Time.unscaledTime;
        advance = false;

        while (!advance || Time.unscaledTime - shownAt < holdSeconds)
        {
            if (Time.unscaledTime - shownAt >= holdSeconds && WantsAdvance()) advance = true;
            yield return null;
        }
        advance = false;
    }

    static bool WantsAdvance()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.E)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetMouseButtonDown(0);
    }

    static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;
        var c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    static float Ease(float k)
    {
        return 1f - Mathf.Pow(1f - k, 3f);
    }
}
