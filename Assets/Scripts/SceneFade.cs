using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 场景转场：先压黑 → 停一下 → 载入新场景 → 慢慢浮现。
/// 用法：SceneFade.FadeTo("Event_01");
/// 自己会建一块常驻的黑幕（DontDestroyOnLoad），不用往场景里放东西。
/// </summary>
public static class SceneFade
{
    static Fader fader;

    public static bool Busy { get { return fader != null && fader.Busy; } }

    public static void FadeTo(string sceneName, float fadeOut = 0.5f, float hold = 0.35f, float fadeIn = 0.9f)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[Fade] 场景名为空，没切");
            return;
        }

        if (fader != null && fader.Busy) return;

        fader = Fader.Ensure();
        fader.Begin(sceneName, fadeOut, hold, fadeIn);
    }
}

public class Fader : MonoBehaviour
{
    static Fader instance;

    Image curtain;
    public bool Busy { get; private set; }

    public static Fader Ensure()
    {
        if (instance != null) return instance;

        var go = new GameObject("场景转场黑幕");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Fader>();
        return instance;
    }

    void Awake()
    {
        instance = this;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        var imgGo = new GameObject("黑幕", typeof(RectTransform), typeof(Image));
        imgGo.transform.SetParent(transform, false);
        var rt = (RectTransform)imgGo.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        curtain = imgGo.GetComponent<Image>();
        curtain.color = new Color(0f, 0f, 0f, 0f);
        curtain.raycastTarget = true;   // 转场时挡住点击，防止连点
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void Begin(string sceneName, float fadeOut, float hold, float fadeIn)
    {
        if (Busy) return;
        StartCoroutine(Run(sceneName, fadeOut, hold, fadeIn));
    }

    IEnumerator Run(string sceneName, float fadeOut, float hold, float fadeIn)
    {
        Busy = true;
        curtain.raycastTarget = true;

        yield return FadeTo(1f, fadeOut);
        yield return new WaitForSecondsRealtime(hold);

        SceneManager.LoadScene(sceneName);
        yield return null;              // 让新场景的 Start 跑完一帧

        yield return FadeTo(0f, fadeIn);
        curtain.raycastTarget = false;
        Busy = false;
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float from = curtain.color.a;
        if (duration <= 0.01f)
        {
            SetAlpha(target);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, target, Mathf.Clamp01(t / duration)));
            yield return null;
        }

        SetAlpha(target);
    }

    void SetAlpha(float a)
    {
        var c = curtain.color;
        c.a = a;
        curtain.color = c;
    }
}
