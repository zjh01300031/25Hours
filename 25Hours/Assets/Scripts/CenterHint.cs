using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>屏幕中间出一行大字，淡入、停几秒、淡出。</summary>
public class CenterHint : MonoBehaviour
{
    static CenterHint instance;

    Canvas canvas;
    Text text;
    CanvasGroup group;

    public static void Show(string message, float seconds = 3.2f)
    {
        if (string.IsNullOrEmpty(message)) return;
        Ensure();
        instance.StartCoroutine(instance.Run(message, seconds));
    }

    static void Ensure()
    {
        if (instance != null) return;

        var go = new GameObject("中心提示");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<CenterHint>();
        instance.Build();
    }

    void Build()
    {
        Font font = Resources.Load<Font>("Fonts/SimHei");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        canvas = RuntimeUI.CreateCanvas("中心提示画布", 95);
        canvas.transform.SetParent(transform, false);

        group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        text = RuntimeUI.Label(canvas.transform, "大字", "", 52, TextAnchor.MiddleCenter,
            new Color(1f, 0.97f, 0.9f, 1f), font, new Vector2(1600f, 200f), new Vector2(0f, 40f));

        var shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(2f, -3f);
    }

    IEnumerator Run(string message, float seconds)
    {
        text.text = message;

        float t = 0f;
        while (t < 0.35f) { t += Time.unscaledDeltaTime; group.alpha = Mathf.Clamp01(t / 0.35f); yield return null; }
        group.alpha = 1f;

        yield return new WaitForSecondsRealtime(seconds);

        t = 0f;
        while (t < 0.5f) { t += Time.unscaledDeltaTime; group.alpha = 1f - Mathf.Clamp01(t / 0.5f); yield return null; }
        group.alpha = 0f;
        text.text = "";
    }
}
