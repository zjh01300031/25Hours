using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对话框：一句一句打字显示，E / 空格 / 鼠标左键继续，ESC 关掉。
/// 打开时会把玩家输入关掉，关掉后恢复。
/// </summary>
public class ShelterDialogue : MonoBehaviour
{
    public static ShelterDialogue Instance { get; private set; }
    public static bool IsOpen { get; private set; }
    public static int LastClosedFrame = -10;

    public GameObject panel;
    public Text speakerText;
    public Text bodyText;
    public Text hintText;
    public ShelterPlayer player;

    [Tooltip("每秒打出几个字")]
    public float charsPerSecond = 45f;

    string[] lines;
    int index;
    int openedFrame;
    float reveal;
    string currentLine = "";

    void Awake()
    {
        Instance = this;
        IsOpen = false;
        if (panel != null) panel.SetActive(false);
    }

    public void Show(string speaker, string[] newLines)
    {
        if (newLines == null || newLines.Length == 0) return;

        lines = newLines;
        index = 0;
        openedFrame = Time.frameCount;
        if (speakerText != null) speakerText.text = string.IsNullOrEmpty(speaker) ? "……" : speaker;
        if (hintText != null) hintText.text = "E / 空格 继续　·　ESC 关闭";
        if (panel != null) panel.SetActive(true);

        IsOpen = true;
        if (player != null)
        {
            player.InputEnabled = false;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        BeginLine();
    }

    void BeginLine()
    {
        currentLine = lines[index] ?? "";
        reveal = 0f;
        if (bodyText != null) bodyText.text = "";
    }

    void Update()
    {
        if (InputGate.Blocked) return;
        if (!IsOpen) return;
        if (Time.frameCount == openedFrame) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        bool advance = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);

        if (!advance)
        {
            if (bodyText != null && reveal < currentLine.Length)
            {
                reveal += Time.unscaledDeltaTime * charsPerSecond;
                int count = Mathf.Clamp(Mathf.FloorToInt(reveal), 0, currentLine.Length);
                bodyText.text = currentLine.Substring(0, count);
            }

            return;
        }

        // 还在打字 → 先把这句显示完
        if (bodyText != null && bodyText.text.Length < currentLine.Length)
        {
            bodyText.text = currentLine;
            reveal = currentLine.Length;
            return;
        }

        index++;
        if (index >= lines.Length) Close();
        else BeginLine();
    }

    public void Close()
    {
        IsOpen = false;
        LastClosedFrame = Time.frameCount;
        if (panel != null) panel.SetActive(false);
        if (player != null) player.InputEnabled = true;
    }
}
