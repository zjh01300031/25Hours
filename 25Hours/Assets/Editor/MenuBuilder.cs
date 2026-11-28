using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 生成白盒主菜单场景：开始 / 继续 / 设置 / 关于我们 / 退出。
/// 菜单：工具 / 25小时 / 生成主菜单场景（白盒）
/// </summary>
public static class MenuBuilder
{
    public const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string FlagPath = "Temp/menu-build.flag";
    const string LogPath = "Temp/menu-build.log";

    static readonly Color Bg = new Color(0.045f, 0.05f, 0.062f, 1f);
    static readonly Color PanelColor = new Color(0.07f, 0.078f, 0.095f, 0.97f);
    static readonly Color ButtonColor = new Color(0.16f, 0.18f, 0.22f, 1f);
    static readonly Color ButtonColorAlt = new Color(0.20f, 0.22f, 0.27f, 1f);
    static readonly Color TextColor = new Color(0.90f, 0.91f, 0.94f, 1f);
    static readonly Color DimText = new Color(0.58f, 0.60f, 0.66f, 1f);
    static readonly Color Accent = new Color(0.93f, 0.83f, 0.55f, 1f);
    static readonly Color SliderBg = new Color(0.10f, 0.11f, 0.135f, 1f);
    static readonly Color SliderFill = new Color(0.72f, 0.75f, 0.82f, 1f);

    [MenuItem("工具/25小时/生成主菜单场景（白盒）", false, 3)]
    public static void Build()
    {
        Font font = LoadFont();

        EditorSceneManager.SaveOpenScenes();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 相机（纯色底）
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Bg;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.tag = "MainCamera";

        // 画布
        var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var view = MakeRect(canvasGo.transform, "主菜单视图", new Vector2(1920f, 1080f), Vector2.zero);
        FillAll(view);
        var ui = view.gameObject.AddComponent<MainMenuUI>();

        // ---- 标题 ----
        var title = MakeText(view, "标题", "第 25 小时", 86, Accent, TextAnchor.MiddleCenter);
        PlaceRect(title.rectTransform, new Vector2(0f, 330f), new Vector2(1200f, 120f));


        // ---- 主菜单面板 ----
        var mainPanel = MakePanel(view, "主菜单", new Vector2(640f, 560f), new Vector2(0f, -30f));
        ui.mainPanel = mainPanel.gameObject;

        var btnStart = MakeButton(mainPanel, "按钮_开始游戏", "开始游戏", font, ButtonColor, 460f, 78f, new Vector2(0f, 120f));
        var btnContinue = MakeButton(mainPanel, "按钮_继续游戏", "继续游戏", font, ButtonColor, 460f, 78f, new Vector2(0f, 16f));
        var btnSettings = MakeButton(mainPanel, "按钮_设置", "设置", font, ButtonColor, 460f, 78f, new Vector2(0f, -88f));
        var btnQuit = MakeButton(mainPanel, "按钮_退出", "退出", font, ButtonColor, 460f, 78f, new Vector2(0f, -192f));

        var hint = MakeText(mainPanel, "继续提示", "", 22, DimText, TextAnchor.MiddleCenter);
        PlaceRect(hint.rectTransform, new Vector2(0f, -244f), new Vector2(560f, 30f));

        ui.buttonStart = btnStart;
        ui.buttonContinue = btnContinue;
        ui.buttonSettings = btnSettings;
        ui.buttonQuit = btnQuit;
        ui.continueHint = hint;

        // ---- 设置面板 ----
        var settings = MakePanel(view, "设置面板", new Vector2(900f, 660f), new Vector2(0f, -20f));
        ui.settingsPanel = settings.gameObject;

        var settingsTitle = MakeText(settings, "设置标题", "设置", 56, Accent, TextAnchor.MiddleCenter);
        PlaceRect(settingsTitle.rectTransform, new Vector2(0f, 262f), new Vector2(600f, 80f));

        var musicLabel = MakeText(settings, "背景音量标签", "背景音量", 28, TextColor, TextAnchor.MiddleLeft);
        PlaceRect(musicLabel.rectTransform, new Vector2(-330f, 120f), new Vector2(260f, 40f));
        var sliderMusic = MakeSlider(settings, "背景音量滑块", font, new Vector2(40f, 120f), 440f);
        var musicValue = MakeText(settings, "背景音量数值", "80%", 26, TextColor, TextAnchor.MiddleRight);
        PlaceRect(musicValue.rectTransform, new Vector2(340f, 120f), new Vector2(100f, 40f));

        var sfxLabel = MakeText(settings, "互动音量标签", "互动音量", 28, TextColor, TextAnchor.MiddleLeft);
        PlaceRect(sfxLabel.rectTransform, new Vector2(-330f, 30f), new Vector2(260f, 40f));
        var sliderSfx = MakeSlider(settings, "互动音量滑块", font, new Vector2(40f, 30f), 440f);
        var sfxValue = MakeText(settings, "互动音量数值", "80%", 26, TextColor, TextAnchor.MiddleRight);
        PlaceRect(sfxValue.rectTransform, new Vector2(340f, 30f), new Vector2(100f, 40f));

        var btnAbout = MakeButton(settings, "按钮_关于我们", "关于我们", font, ButtonColorAlt, 320f, 74f, new Vector2(0f, -130f));
        var btnBackSettings = MakeButton(settings, "按钮_设置返回", "返回", font, ButtonColor, 320f, 74f, new Vector2(0f, -232f));

        ui.sliderMusic = sliderMusic;
        ui.sliderSfx = sliderSfx;
        ui.musicValueText = musicValue;
        ui.sfxValueText = sfxValue;
        ui.buttonAbout = btnAbout;
        ui.buttonBackFromSettings = btnBackSettings;

        // ---- 关于面板 ----
        var about = MakePanel(view, "关于面板", new Vector2(900f, 700f), new Vector2(0f, -20f));
        ui.aboutPanel = about.gameObject;

        var aboutTitle = MakeText(about, "关于标题", "关于我们", 56, Accent, TextAnchor.MiddleCenter);
        PlaceRect(aboutTitle.rectTransform, new Vector2(0f, 280f), new Vector2(600f, 80f));


        var btnBackAbout = MakeButton(about, "按钮_关于返回", "返回", font, ButtonColor, 320f, 74f, new Vector2(0f, -282f));
        ui.buttonBackFromAbout = btnBackAbout;

        // EventSystem
        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        settings.gameObject.SetActive(false);
        about.gameObject.SetActive(false);
        mainPanel.gameObject.SetActive(true);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        SetupBuildSettings();

        Debug.Log("[Menu] 主菜单场景已生成：" + ScenePath);
        WriteLog("[Menu] 主菜单场景已生成：" + ScenePath);
    }

    /// <summary>主菜单放第一个（游戏启动就进它）。</summary>
    static void SetupBuildSettings()
    {
        string[] order =
        {
            ScenePath,
            "Assets/Scenes/Shelter_L0.unity",
            "Assets/Scenes/Event_01.unity",
            "Assets/Scenes/Event_02.unity"
        };

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var path in order)
        {
            if (File.Exists(path)) list.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[Menu] Build Settings：" + string.Join(" / ", list.ConvertAll(s => Path.GetFileNameWithoutExtension(s.path))));
    }

    // ---------------- 造 UI 的小工具 ----------------

    static Font LoadFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/SimHei.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font;
    }

    static RectTransform MakeRect(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    static void FillAll(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    static void PlaceRect(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static RectTransform MakePanel(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        var rt = MakeRect(parent, name, size, pos);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = PanelColor;
        img.raycastTarget = false;
        return rt;
    }

    static Text MakeText(Transform parent, string name, string content, int size, Color color, TextAnchor anchor)
    {
        var rt = MakeRect(parent, name, new Vector2(400f, 60f), Vector2.zero);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = LoadFont();
        text.fontSize = size;
        text.text = content;
        text.color = color;
        text.alignment = anchor;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static Button MakeButton(Transform parent, string name, string label, Font font,
                         Color color, float width, float height, Vector2 pos)
    {
        var rt = MakeRect(parent, name, new Vector2(width, height), pos);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;

        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.86f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var text = MakeText(rt, "文字", label, 34, TextColor, TextAnchor.MiddleCenter);
        FillAll(text.rectTransform);
        return button;
    }

    static Slider MakeSlider(Transform parent, string name, Font font, Vector2 pos, float width)
    {
        var rt = MakeRect(parent, name, new Vector2(width, 30f), pos);
        var slider = rt.gameObject.AddComponent<Slider>();

        // 底槽
        var bg = MakeRect(rt, "背景", new Vector2(width, 10f), Vector2.zero);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = SliderBg;

        // 已填充部分：左右各留 10，和把手活动范围对齐
        var fillArea = MakeRect(rt, "填充区", new Vector2(width - 20f, 10f), Vector2.zero);
        var fill = MakeRect(fillArea, "填充", new Vector2(width - 20f, 10f), Vector2.zero);
        FillAll(fill);
        var fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.color = SliderFill;

        // 把手
        var handleArea = MakeRect(rt, "把手区", new Vector2(width - 20f, 30f), Vector2.zero);
        var handle = MakeRect(handleArea, "把手", new Vector2(22f, 30f), Vector2.zero);
        var handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.color = new Color(0.94f, 0.94f, 0.97f, 1f);

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImg;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = GameAudio.DefaultVolume;
        return slider;
    }

    // ---------------- 自动跑（编辑器已打开时用 flag 触发） ----------------

    [InitializeOnLoadMethod]
    static void AutoRunIfRequested()
    {
        if (!File.Exists(FlagPath)) return;
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

        try { File.Delete(FlagPath); } catch { }
        EditorApplication.delayCall += () =>
        {
            try
            {
                Build();
                WriteLog("[Menu] 完成 " + System.DateTime.Now.ToString("HH:mm:ss"));
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Menu] 生成失败：" + e);
                WriteLog("[Menu] 失败：" + e);
            }
        };
    }

    static void WriteLog(string line)
    {
        try { File.AppendAllText(LogPath, line + "\n"); } catch { }
    }
}
