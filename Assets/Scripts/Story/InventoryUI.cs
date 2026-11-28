using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包：Q 打开 / 关闭。W/S 选物品，空格「使用」。
/// 物品列表直接读 GameProgress（捡到的东西会自动出现）。
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [System.Serializable]
    public class BoardLink
    {
        [Tooltip("背包里哪件物品")] public string itemId;
        [Tooltip("用掉它以后进哪个棋盘场景（要已在 Build Settings）")] public string sceneName;
        [Tooltip("提示文字，可留空")] public string label;
    }

    [Header("背包里对应能开棋盘的收集物")]
    public BoardLink[] boards = new BoardLink[0];

    public class ItemUse
    {
        public string itemId = "";
        [TextArea(1, 3)] public string useText = "（这个还没写用法）";
    }

    [Tooltip("界面用的字体（一般不用改）")]
    public Font font;

    [Tooltip("按 Q 打开/关闭")]
    public KeyCode toggleKey = KeyCode.Q;

    [Tooltip("每种物品「使用」时显示的话，没写就用默认提示")]
    public ItemUse[] uses = new ItemUse[0];

    static InventoryUI instance;
    static readonly List<string> ids = new List<string>();
    static readonly List<string> names = new List<string>();
    static readonly List<int> counts = new List<int>();
    static List<Text> rows = new List<Text>();
    static Text footer;
    static int index;
    static ShelterPlayer player;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (BoardIntroUI.IsPlaying) return;
        // 有别的弹窗（棋盘入口 / 多选项）的时候，背包键不响应
        if (BoardGate.IsOpen || ChoiceUI.IsOpen) return;

        if (Input.GetKeyDown(toggleKey))
        {
            if (IsOpen) Close();
            else Open();
            return;
        }

        if (!IsOpen) return;

        if (ids.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            index = (index - 1 + ids.Count) % ids.Count;
            Refresh();
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            index = (index + 1) % ids.Count;
            Refresh();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            Use(index);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    void Open()
    {
        EnsureUI();
        Rebuild();
        IsOpen = true;
        panel.gameObject.SetActive(true);
        Refresh();

        player = FindObjectOfType<ShelterPlayer>();
        if (player != null) player.InputEnabled = false;
        if (ShelterHUD.Instance != null) ShelterHUD.Instance.SetPrompt(null);
    }

    void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        if (panel != null) panel.gameObject.SetActive(false);
        if (player != null) player.InputEnabled = true;
    }

    static RectTransform panel;

    void EnsureUI()
    {
        if (panel != null) return;

        var canvas = RuntimeUI.CreateCanvas("背包界面", 70);
        // 把界面挂在这个组件自己身上，Q 关了以后对象还在
        canvas.transform.SetParent(transform, false);

        panel = RuntimeUI.Panel(canvas.transform, "面板", new Vector2(760f, 520f),
            new Vector2(0f, 0f), new Color(0.04f, 0.05f, 0.07f, 0.95f));

        RuntimeUI.Label(panel, "标题", "背包", 32, TextAnchor.MiddleLeft,
            new Color(0.96f, 0.82f, 0.45f), font, new Vector2(680f, 46f), new Vector2(0f, 210f));

        rows = RuntimeUI.Rows(panel, font, 26, 46f, 680f, 140f);

        RuntimeUI.Label(panel, "空提示", "（还没有东西）", 24, TextAnchor.MiddleCenter,
            new Color(0.6f, 0.62f, 0.68f), font, new Vector2(680f, 30f), new Vector2(0f, 60f)).name = "空提示";

        footer = RuntimeUI.Label(panel, "提示", "W / S 选择　空格 使用　Q 关闭", 22, TextAnchor.MiddleCenter,
            new Color(0.72f, 0.75f, 0.8f), font, new Vector2(680f, 34f), new Vector2(0f, -220f));
    }

    void Rebuild()
    {
        ids.Clear();
        names.Clear();
        counts.Clear();

        var progress = GameProgress.Ensure();
        foreach (var pair in progress.Items)
        {
            if (pair.Value <= 0) continue;
            ids.Add(pair.Key);
            names.Add(progress.ItemName(pair.Key));
            counts.Add(pair.Value);
        }

        if (index >= ids.Count) index = 0;
    }

    void Refresh()
    {
        var empty = panel.Find("空提示");
        if (empty != null) empty.gameObject.SetActive(ids.Count == 0);

        for (int i = 0; i < rows.Count; i++)
        {
            if (i >= ids.Count)
            {
                rows[i].gameObject.SetActive(false);
                continue;
            }

            rows[i].gameObject.SetActive(true);
            bool on = i == index;
            string count = counts[i] > 1 ? "  ×" + counts[i] : "";
            rows[i].text = (on ? "▶  " : "　  ") + names[i] + count;
            rows[i].color = on ? new Color(1f, 0.9f, 0.55f) : new Color(0.78f, 0.8f, 0.85f);
        }

        if (footer != null && ids.Count > 0)
        {
            footer.text = "「" + names[index] + "」　W / S 选择　空格 使用　Q 关闭";
        }
    }

    void Use(int i)
    {
        if (i < 0 || i >= ids.Count) return;

        string id = ids[i];
        string name = names[i];

        // 1) 先看是不是「能开棋盘的收集物」
        if (boards != null)
        {
            for (int k = 0; k < boards.Length; k++)
            {
                var link = boards[k];
                if (link == null || string.IsNullOrEmpty(link.itemId) || link.itemId != id) continue;
                if (string.IsNullOrEmpty(link.sceneName)) continue;

                if (!Application.CanStreamedLevelBeLoaded(link.sceneName))
                {
                    if (ShelterHUD.Instance != null)
                        ShelterHUD.Instance.Toast("场景 " + link.sceneName + " 不在 Build Settings 里");
                    return;
                }

                BoardSession.Set(id, name, null);
                Close();
                if (ShelterHUD.Instance != null)
                    ShelterHUD.Instance.Toast(string.IsNullOrEmpty(link.label) ? "带着「" + name + "」进入棋盘…" : link.label);
                SceneFade.FadeTo(link.sceneName, 0.45f, 0.3f, 0.85f);
                return;
            }
        }

        // 2) 普通物品：显示 ItemUse 里写好的话
        string text = null;
        if (uses != null)
        {
            for (int k = 0; k < uses.Length; k++)
            {
                if (uses[k] != null && uses[k].itemId == id) { text = uses[k].useText; break; }
            }
        }

        if (ShelterHUD.Instance != null)
        {
            ShelterHUD.Instance.Toast(text != null ? text : "「" + name + "」现在还用不上");
        }
    }
}
