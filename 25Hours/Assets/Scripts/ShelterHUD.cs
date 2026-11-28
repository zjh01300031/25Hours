using UnityEngine;
using UnityEngine.UI;

/// <summary>第0层的 HUD：左下操作提示、右下提示条、碎片计数、气泡消息。</summary>
public class ShelterHUD : MonoBehaviour
{
    public static ShelterHUD Instance { get; private set; }

    public GameObject promptRoot;
    public Text promptText;

    string lastPrompt;
    public Text fragmentText;
    public Text titleText;
    public Text helpText;
    public Text toastText;

    float toastUntil;

    void Awake()
    {
        Instance = this;
        SetPrompt(null);
    }

    void Start()
    {
        var progress = GameProgress.Instance;
        if (progress != null) SetFragments(progress.FragmentCount, GameProgress.FragmentTotal);
        if (toastText != null) toastText.text = "";
    }

    void Update()
    {
        // 对话打开时把左下角的按键提示藏起来，别和对话框叠在一起
        if (helpText != null)
        {
            bool showHelp = !ShelterDialogue.IsOpen;
            if (helpText.enabled != showHelp) helpText.enabled = showHelp;
        }

        if (toastText != null && toastText.text.Length > 0 && Time.unscaledTime > toastUntil)
        {
            toastText.text = "";
        }
    }

    public void SetPrompt(string text)
    {
        // 每帧调用：文字没变就什么都别做，否则 UGUI 每帧重建文字网格，高刷屏上会明显掉帧
        if (text == lastPrompt) return;
        lastPrompt = text;

        bool show = !string.IsNullOrEmpty(text);
        if (promptRoot != null && promptRoot.activeSelf != show) promptRoot.SetActive(show);
        if (promptText != null) promptText.text = show ? text : "";
    }

    public void SetFragments(int found, int total)
    {
        if (fragmentText != null) fragmentText.text = "碎片 " + found + " / " + total;
    }

    public void Toast(string message, float seconds = 2.6f)
    {
        if (toastText == null) return;
        toastText.text = message;
        toastUntil = Time.unscaledTime + seconds;
    }
}
