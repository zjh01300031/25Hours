using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 能「一次说好几句、让你选」的 NPC。
/// 按交互键打开选项 → W/S 选 → 空格确定 → 播放这句对应的台词。
/// Options 留空时退化成只说 Talk Lines。
/// </summary>
public class ChoiceNpc : InteractableBase
{
    [System.Serializable]
    public class Option
    {
        [Tooltip("选项上显示的文字")]
        public string text = "选项";

        [Tooltip("选这一项之后说的话（一行一句）")]
        [TextArea(1, 4)]
        public string[] lines = new string[0];

        [Tooltip("可选：背包里要有这个物品才会出现这个选项。留空 = 一直有")]
        public string requiresItemId = "";
    }

    [Tooltip("对话框左上角的名字")]
    public string speaker = "白发少女";

    [Tooltip("没有选项时直接说的话")]
    [TextArea(1, 4)]
    public string[] talkLines = new string[0];

    [Tooltip("W/S 选择的选项；requiresItemId 可以用来做「要带着某样东西才有的选项」")]
    public Option[] options = new Option[0];

    [Tooltip("界面用的字体（一般不用改）")]
    public Font font;

    [Tooltip("说完话之后，屏幕中间出的那行大字（留空不出）")]
    public string afterTalkHint = "";

    [Tooltip("非空时：这个事件通关之前，她不理你（填 event_02）。留空 = 一开始就能说话")]
    public string requireEventId = "";

    [Tooltip("事件没通关时按交互键的提示")]
    public string lockedToast = "她还没打算和你说话。";

    public override void Interact(ShelterPlayer player)
    {
        if (!string.IsNullOrEmpty(requireEventId)
            && !GameProgress.Ensure().IsEventCleared(requireEventId))
        {
            if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast(lockedToast);
            else Debug.Log("[NPC] " + lockedToast);
            return;
        }

        ChoiceUI.Open(this, font);
    }

    public List<Option> Available()
    {
        var list = new List<Option>();
        var progress = GameProgress.Ensure();
        for (int i = 0; i < options.Length; i++)
        {
            var o = options[i];
            if (o == null) continue;
            if (!string.IsNullOrEmpty(o.requiresItemId) && progress.ItemCount(o.requiresItemId) <= 0) continue;
            list.Add(o);
        }
        return list;
    }

    public void Say(string[] lines)
    {
        var use = (lines != null && lines.Length > 0) ? lines : talkLines;
        if (use == null || use.Length == 0)
        {
            if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast("（还没填文字）");
            return;
        }

        if (ShelterDialogue.Instance != null) ShelterDialogue.Instance.Show(speaker, use);
        else Debug.Log(speaker + "：" + string.Join(" / ", use));

        if (!string.IsNullOrEmpty(afterTalkHint)) StartCoroutine(HintAfterDialogue());
    }

    /// <summary>等对话关掉之后，屏幕中间出那行大字。</summary>
    IEnumerator HintAfterDialogue()
    {
        yield return null;
        while (ShelterDialogue.IsOpen) yield return null;

        yield return new WaitForSecondsRealtime(0.25f);
        CenterHint.Show(afterTalkHint);
    }
}

/// <summary>选项界面：运行时自己搭，W/S 选，空格确定。</summary>
public class ChoiceUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    static ChoiceUI instance;
    static ChoiceNpc current;
    static List<ChoiceNpc.Option> options = new List<ChoiceNpc.Option>();
    static List<Text> rows = new List<Text>();
    static Text title;
    static int index;
    static int openedFrame;
    static ShelterPlayer player;

    public static void Open(ChoiceNpc npc, Font font)
    {
        if (IsOpen) return;

        options = npc.Available();
        if (options.Count == 0)
        {
            npc.Say(npc.talkLines);
            return;
        }

        current = npc;
        EnsureUI(npc, font);
        index = 0;
        openedFrame = Time.frameCount;
        IsOpen = true;
        instance.gameObject.SetActive(true);
        RefreshRows();

        player = FindObjectOfType<ShelterPlayer>();
        if (player != null) player.InputEnabled = false;
        if (ShelterHUD.Instance != null) ShelterHUD.Instance.SetPrompt(null);
    }

    static void EnsureUI(ChoiceNpc npc, Font font)
    {
        if (instance != null) return;

        var canvas = RuntimeUI.CreateCanvas("选项界面", 80);
        instance = canvas.gameObject.AddComponent<ChoiceUI>();

        var panel = RuntimeUI.Panel(canvas.transform, "面板", new Vector2(900f, 460f),
            new Vector2(0f, -120f), new Color(0.04f, 0.05f, 0.07f, 0.95f));

        title = RuntimeUI.Label(panel, "名字", npc.speaker, 30, TextAnchor.MiddleLeft,
            new Color(0.96f, 0.82f, 0.45f), font, new Vector2(820f, 44f), new Vector2(0f, 180f));

        rows = RuntimeUI.Rows(panel, font, 28, 52f, 820f, 120f);

        RuntimeUI.Label(panel, "提示", "W / S 选择　空格 确定　ESC 取消", 22, TextAnchor.MiddleCenter,
            new Color(0.72f, 0.75f, 0.8f), font, new Vector2(820f, 34f), new Vector2(0f, -196f));
    }

    static void RefreshRows()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (i >= options.Count)
            {
                rows[i].gameObject.SetActive(false);
                continue;
            }

            rows[i].gameObject.SetActive(true);
            bool on = i == index;
            rows[i].text = (on ? "▶  " : "　  ") + options[i].text;
            rows[i].color = on ? new Color(1f, 0.9f, 0.55f) : new Color(0.78f, 0.8f, 0.85f);
        }
    }

    void Update()
    {
        if (BoardIntroUI.IsPlaying || InputGate.Blocked) return;
        if (!IsOpen) return;
        if (Time.frameCount == openedFrame) return;   // 打开这一帧按的空格别当成确定

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            index = (index - 1 + options.Count) % options.Count;
            RefreshRows();
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            index = (index + 1) % options.Count;
            RefreshRows();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
        {
            Close();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            var chosen = options[index];
            var npc = current;
            Close();
            if (npc != null) npc.Say(chosen.lines);
        }
    }

    public static void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        if (instance != null) instance.gameObject.SetActive(false);
        if (player != null) player.InputEnabled = true;
    }
}
