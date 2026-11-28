using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 投人偶之后、真正进棋盘之前的那段文案演出。
/// 一页一行字：先整页淡入，标题与正文同时出现在屏幕上，停一会儿，按空格/回车/左键翻页。
/// 最后一页翻完就执行回调（通常去切场景）。
///
/// 文案写在 BoardGate 的 Intro Slides 里，一行一页；「标题|正文」会把第一段当标题。
/// 不写就用 BoardGate.DefaultSlides() 的三页占位。
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

    [Tooltip("每页自动停留多少秒，到点自动翻下一页")]
    public float pageSeconds = 2.6f;

    [Tooltip("长按这个键多少秒可以跳过整段演出")]
    public float skipHoldSeconds = 0.6f;

    Canvas canvas;
    CanvasGroup group;
    Text titleText;
    Text bodyText;
    Text footerText;
    Font font;

    bool playing;
    int index;
    float pageShownAt;
    Action onDone;
    readonly List<string[]> pages = new List<string[]>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Play(string[] lines, Action done)
    {
        if (playing) return;

        pages.Clear();
        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null) continue;
                var raw = lines[i].Replace("\r", "");
                if (raw.Trim().Length == 0) continue;

                int bar = raw.IndexOf('|');
                if (bar >= 0)
                    pages.Add(new[] { raw.Substring(0, bar).Trim(), raw.Substring(bar + 1).Trim() });
                else
                    pages.Add(new[] { "", raw.Trim() });
            }
        }

        if (pages.Count == 0)
        {
            Debug.LogWarning("[开场文案] 没有可播放的文字，直接继续");
            if (done != null) done();
            return;
        }

        onDone = done;
        Build();
        gameObject.SetActive(true);
        StartCoroutine(Run());
    }

    void Build()
    {
        if (canvas != null) return;

        font = Resources.Load<Font>("Fonts/SimHei");
        if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "SimHei", "Microsoft YaHei" }, 32);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        canvas = RuntimeUI.CreateCanvas("开场文案", 75);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;

        // 全屏黑幕（盖住探索区，顺便挡住点击）
        var backdrop = RuntimeUI.Panel(canvas.transform, "黑幕", new Vector2(1920f, 1080f), Vector2.zero,
            new Color(0f, 0f, 0f, 1f));
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = Vector2.zero;
        backdrop.offsetMax = Vector2.zero;

        titleText = RuntimeUI.Label(canvas.transform, "标题", "", 60,
            TextAnchor.LowerCenter, new Color(0.98f, 0.94f, 0.87f, 1f), font,
            new Vector2(1500f, 110f), new Vector2(0f, 70f));
        titleText.raycastTarget = false;

        bodyText = RuntimeUI.Label(canvas.transform, "正文", "", 34,
            TextAnchor.UpperCenter, new Color(0.88f, 0.90f, 0.94f, 1f), font,
            new Vector2(1300f, 560f), new Vector2(0f, -90f));
        bodyText.raycastTarget = false;
        bodyText.lineSpacing = 1.55f;

        footerText = RuntimeUI.Label(canvas.transform, "页脚", "", 24,
            TextAnchor.MiddleCenter, new Color(0.62f, 0.66f, 0.72f, 1f), font,
            new Vector2(1500f, 40f), new Vector2(0f, -440f));
        footerText.raycastTarget = false;

        canvas.enabled = false;
    }

    IEnumerator Run()
    {
        playing = true;
        canvas.enabled = true;
        group.alpha = 0f;

        bool wasPlayerEnabled = true;
        var player = FindObjectOfType<ShelterPlayer>();
        if (player != null) { wasPlayerEnabled = player.InputEnabled; player.InputEnabled = false; }

        var walker = FindObjectOfType<TopDownPlayer>();
        bool wasWalkerEnabled = true;
        if (walker != null) { wasWalkerEnabled = walker.inputEnabled; walker.inputEnabled = false; }

        // 先压黑，避免上一帧的按键被当成翻页
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / 0.25f);
            yield return null;
        }
        group.alpha = 1f;

        // 开场这一下按的键不算翻页
        yield return null;
        yield return null;

        float holdSkip = 0f;
        bool skipped = false;

        for (index = 0; index < pages.Count && !skipped; index++)
        {
            Show(index);
            pageShownAt = Time.unscaledTime;

            // 自动播放：到 pageSeconds 就翻页；期间长按 F 直接跳过整段
            while (Time.unscaledTime - pageShownAt < pageSeconds)
            {
                if (Input.GetKey(KeyCode.F))
                {
                    holdSkip += Time.unscaledDeltaTime;
                    if (holdSkip >= skipHoldSeconds)
                    {
                        Debug.Log("[开场文案] 长按 F 跳过");
                        skipped = true;
                        break;
                    }
                }
                else holdSkip = 0f;

                yield return null;
            }
        }

        // 播完保持全黑，交给 SceneFade 直接切场景——中间不再露出探索区
        group.alpha = 1f;
        playing = false;

        if (player != null) player.InputEnabled = wasPlayerEnabled;
        if (walker != null) walker.inputEnabled = wasWalkerEnabled;

        var callback = onDone;
        onDone = null;
        if (callback != null) callback();
    }

    void Show(int pageIndex)
    {
        var page = pages[pageIndex];
        titleText.text = page[0];
        bodyText.text = string.IsNullOrEmpty(page[1]) ? "（这一页还没有文字）" : page[1];
        footerText.text = "自动播放　·　长按 F 跳过　　" + (pageIndex + 1) + " / " + pages.Count;

        Debug.Log("[开场文案] 第 " + (pageIndex + 1) + " / " + pages.Count + " 页：" +
                  (titleText.text.Length > 0 ? titleText.text + " | " : "") + bodyText.text);
    }

}
