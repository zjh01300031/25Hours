using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单：开始 / 继续 / 设置 / 关于我们 / 退出。
/// 全是白盒：深色底板 + 白字，没有任何美术资源。
/// 点开始/继续时先压黑一小会儿再切场景，避免"啪"一下就跳走。
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("面板")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject aboutPanel;

    [Header("主菜单按钮")]
    public Button buttonStart;
    public Button buttonContinue;
    public Button buttonSettings;
    public Button buttonQuit;
    public Text continueHint;

    [Header("设置面板")]
    public Slider sliderMusic;
    public Slider sliderSfx;
    public Text musicValueText;
    public Text sfxValueText;
    public Button buttonAbout;
    public Button buttonBackFromSettings;

    [Header("关于面板")]
    public Button buttonBackFromAbout;

    [Header("转场")]
    [Tooltip("压黑用多久（秒）")]
    public float fadeDuration = 0.45f;
    [Tooltip("全黑之后停顿多久再切场景（秒）")]
    public float holdBlack = 0.25f;

    Image fadeImage;
    bool leaving;

    void Start()
    {
        GameAudio.Load();

        if (sliderMusic != null)
        {
            sliderMusic.value = GameAudio.MusicVolume;
            sliderMusic.onValueChanged.AddListener(OnMusicChanged);
        }

        if (sliderSfx != null)
        {
            sliderSfx.value = GameAudio.SfxVolume;
            sliderSfx.onValueChanged.AddListener(OnSfxChanged);
        }

        Wire(buttonStart, StartGame);
        Wire(buttonContinue, ContinueGame);
        Wire(buttonSettings, OpenSettings);
        Wire(buttonQuit, QuitGame);
        Wire(buttonAbout, OpenAbout);
        Wire(buttonBackFromSettings, ShowMain);
        Wire(buttonBackFromAbout, ShowMain);

        // 转场黑幕由 SceneFade 统一管理，这里不再自己造
        RefreshVolumeLabels();
        ShowMain();
        RefreshContinueButton();
    }

    static void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    /// <summary>在同一个 Canvas 最上层放一块全屏黑，平时透明。</summary>
    void EnsureFadeImage()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("转场黑幕", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsLastSibling();

        fadeImage = go.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;
    }

    // ---------- 按钮 ----------

    void StartGame()
    {
        if (leaving) return;
        // 新游戏：先把存档删掉，免得继承上一次的进度
        if (File.Exists(GameProgress.SavePath)) File.Delete(GameProgress.SavePath);
        StartCoroutine(FadeThenLoad());
    }

    void ContinueGame()
    {
        if (leaving) return;

        if (!File.Exists(GameProgress.SavePath))
        {
            if (continueHint != null) continueHint.text = "还没有存档，先开始游戏吧";
            return;
        }

        StartCoroutine(FadeThenLoad());
    }

    /// <summary>压黑 → 停一下 → 切场景（进游戏后再慢慢浮现）。</summary>
    IEnumerator FadeThenLoad()
    {
        leaving = true;
        SetButtonsInteractable(false);

        SceneFade.FadeTo(SceneNames.ShelterL0, fadeDuration, holdBlack, 0.75f);
        yield break;
    }

    void SetButtonsInteractable(bool value)
    {
        if (buttonStart != null) buttonStart.interactable = value;
        if (buttonContinue != null) buttonContinue.interactable = value;
        if (buttonSettings != null) buttonSettings.interactable = value;
        if (buttonQuit != null) buttonQuit.interactable = value;
    }

    void OpenSettings()
    {
        ShowOnly(settingsPanel);
    }

    void OpenAbout()
    {
        ShowOnly(aboutPanel);
    }

    void ShowMain()
    {
        ShowOnly(mainPanel);
    }

    void ShowOnly(GameObject panel)
    {
        if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
        if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
        if (aboutPanel != null) aboutPanel.SetActive(panel == aboutPanel);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- 音量 ----------

    void OnMusicChanged(float value)
    {
        GameAudio.SetMusic(value);
        RefreshVolumeLabels();
    }

    void OnSfxChanged(float value)
    {
        GameAudio.SetSfx(value);
        RefreshVolumeLabels();
    }

    void RefreshVolumeLabels()
    {
        if (musicValueText != null) musicValueText.text = Mathf.RoundToInt(GameAudio.MusicVolume * 100f) + "%";
        if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(GameAudio.SfxVolume * 100f) + "%";
    }

    void RefreshContinueButton()
    {
        bool hasSave = File.Exists(GameProgress.SavePath);
        if (buttonContinue != null)
        {
            var colors = buttonContinue.colors;
            colors.normalColor = hasSave ? new Color(0.16f, 0.18f, 0.22f) : new Color(0.12f, 0.13f, 0.15f);
            buttonContinue.colors = colors;
        }

        if (continueHint != null)
            continueHint.text = hasSave ? "" : "（还没有存档）";
    }
}
