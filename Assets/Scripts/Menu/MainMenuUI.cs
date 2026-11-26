using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单：开始 / 继续 / 设置 / 关于我们 / 退出。
/// 全是白盒：深色底板 + 白字，没有任何美术资源。
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

    // ---------- 按钮 ----------

    void StartGame()
    {
        // 新游戏：先把存档删掉，免得继承上一次的进度
        if (File.Exists(GameProgress.SavePath)) File.Delete(GameProgress.SavePath);
        SceneManager.LoadScene(SceneNames.ShelterL0);
    }

    void ContinueGame()
    {
        if (!File.Exists(GameProgress.SavePath))
        {
            if (continueHint != null) continueHint.text = "还没有存档，先开始游戏吧";
            return;
        }

        SceneManager.LoadScene(SceneNames.ShelterL0);
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
