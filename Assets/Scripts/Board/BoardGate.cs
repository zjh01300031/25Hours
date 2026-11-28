using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 棋盘入口：玩家走进这块区域，就弹出文案 + 选项。
/// 选项里选「投入物品」会列出背包里的东西，选中正确的那件（默认「人偶」）才放人进去，
/// 其它东西会被拒绝。投入成功会消耗掉那件物品，再转场进棋盘场景。
///
/// 按键和别处一样：W/S 选，空格确定，Esc 离开。
/// </summary>
public class BoardGate : MonoBehaviour
{
    [Header("文案")]
    public string gateTitle = "棋盘入口";
    [TextArea(2, 5)]
    public string introText = "石台上有一个凹槽，形状像一个人偶。";

    [Header("条件")]
    [Tooltip("要投入的物品 id（背包里的 itemId）")]
    public string requiredItemId = "item_doll";
    [Tooltip("提示里显示的名字")]
    public string requiredItemName = "人偶";
    [Tooltip("投进去以后去哪个场景（必须已在 Build Settings）")]
    public string boardScene = "Event_01";
    [Tooltip("投入后是否消耗这件物品")]
    public bool consumeItem = true;

    [System.Serializable]
    public class DollArt
    {
        public string itemId;
        public Sprite sprite;
    }

    [Header("人偶立绘（美术给了就填：itemId -> 立绘）")]
    public DollArt[] dollArts = new DollArt[0];

    [Tooltip("白盒阶段：背包里没有人偶时，也给一个「调试人偶」方便试玩。上线前关掉")]
    public bool allowDebugDoll = true;

    const string DebugKey = "__debug_doll";

    [Header("触发")]
    public float triggerRadius = 1.8f;
    public Color gateColor = new Color(0.55f, 0.78f, 0.95f, 1f);
    public Vector2 gateSize = new Vector2(1.1f, 1.6f);

    public static bool IsOpen { get; private set; }

    bool open;
    bool armed = true;
    int cursor;
    readonly List<KeyValuePair<string, int>> items = new List<KeyValuePair<string, int>>();

    Canvas canvas;
    Text listText;
    Text hintText;
    ShelterPlayer player;
    Transform actor;
    TopDownPlayer topdown;
    readonly List<GameObject> spawned = new List<GameObject>();
    Text promptText;

    void Start()
    {
        if (GetComponent<SpriteRenderer>() == null)
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Marker.White;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = gateSize;
            sr.color = gateColor;
            sr.sortingOrder = 3;
        }

        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = Mathf.Max(0.4f, triggerRadius * 0.5f);
        }
    }

    void Update()
    {
        if (actor == null)
        {
            player = FindObjectOfType<ShelterPlayer>();
            topdown = FindObjectOfType<TopDownPlayer>();
            actor = player != null ? player.transform : (topdown != null ? topdown.transform : null);
            if (actor == null) return;
        }

        float dist = Vector2.Distance(actor.position, transform.position);

        if (!open)
        {
            bool near = dist <= triggerRadius;
            SetPrompt(near ? "空格  与「" + gateTitle + "」交互" : null);

            if (near && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)))
            {
                SetPrompt(null);
                Open();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) { Close("先不投"); return; }

        if (items.Count > 0)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) cursor = (cursor + items.Count - 1) % items.Count;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) cursor = (cursor + 1) % items.Count;
            Refresh();
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) PutIn();
    }

    void SetPrompt(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            if (promptText != null) promptText.gameObject.SetActive(false);
            return;
        }

        if (promptText == null)
        {
            var canvas = RuntimeUI.CreateCanvas("入口提示", 62);
            spawned.Add(canvas.gameObject);
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 26);
            promptText = RuntimeUI.Label(canvas.transform, "提示", "", 26, TextAnchor.MiddleCenter,
                new Color(0.95f, 0.86f, 0.62f), font, new Vector2(1200f, 44f), new Vector2(0f, -300f));
        }

        promptText.text = text;
        promptText.gameObject.SetActive(true);
    }

    void Open()
    {
        open = true;
        armed = false;
        IsOpen = true;
        if (player != null) player.InputEnabled = false;
        if (topdown != null) topdown.inputEnabled = false;

        items.Clear();
        var progress = GameProgress.Ensure();
        foreach (var kv in progress.Items) items.Add(kv);
        if (allowDebugDoll)
        {
            bool hasDoll = false;
            for (int i = 0; i < items.Count; i++)
                if (items[i].Key == requiredItemId) { hasDoll = true; break; }

            if (!hasDoll) items.Add(new KeyValuePair<string, int>(DebugKey, 1));
        }
        cursor = 0;

        BuildUI();
        Refresh();
    }

    void Close(string hint)
    {
        open = false;
        IsOpen = false;
        if (canvas != null) Destroy(canvas.gameObject);
        canvas = null;
        if (player != null) player.InputEnabled = true;
        if (topdown != null) topdown.inputEnabled = true;
        if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast(hint);
        else Debug.Log("[棋盘入口] " + hint);
    }

    void PutIn()
    {
        if (items.Count == 0)
        {
            Say("背包里没有可以投的东西");
            return;
        }

        var picked = items[Mathf.Clamp(cursor, 0, items.Count - 1)];

        if (picked.Key == DebugKey)
        {
            Sprite debugArt = null;
            if (dollArts != null)
                for (int i = 0; i < dollArts.Length; i++)
                    if (dollArts[i] != null && dollArts[i].itemId == requiredItemId) debugArt = dollArts[i].sprite;

            if (string.IsNullOrEmpty(boardScene))
            {
                Say("这块棋盘还在设计，先把人偶收着吧");
                return;
            }

            BoardSession.Set(requiredItemId, requiredItemName, debugArt);
            Close("（调试）放了一个空人偶进去");
            SceneFade.FadeTo(boardScene, 0.45f, 0.25f, 0.8f);
            return;
        }

        if (picked.Key != requiredItemId)
        {
            Say("放上去又弹了出来 —— 得是「" + requiredItemName + "」");
            return;
        }

        Sprite art = null;
        if (dollArts != null)
        {
            for (int i = 0; i < dollArts.Length; i++)
                if (dollArts[i] != null && dollArts[i].itemId == picked.Key) art = dollArts[i].sprite;
        }
        if (string.IsNullOrEmpty(boardScene))
        {
            Say("这块棋盘还在设计，先把人偶收着吧");
            return;
        }

        BoardSession.Set(picked.Key, GameProgress.Ensure().ItemName(picked.Key), art);

        if (consumeItem) GameProgress.Ensure().RemoveItem(requiredItemId, 1);
        Close("「" + requiredItemName + "」嵌了进去");
        SceneFade.FadeTo(boardScene, 0.45f, 0.25f, 0.8f);
    }

    void BuildUI()
    {
        canvas = RuntimeUI.CreateCanvas("棋盘入口UI", 85);

        RuntimeUI.Panel(canvas.transform, "底", new Vector2(4000f, 3000f),
            Vector2.zero, new Color(0f, 0f, 0f, 0.55f));

        var panel = RuntimeUI.Panel(canvas.transform, "面板", new Vector2(980f, 520f),
            Vector2.zero, new Color(0.08f, 0.09f, 0.12f, 0.97f));

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var simhei = Font.CreateDynamicFontFromOSFont("SimHei", 28);
        if (simhei != null) font = simhei;

        RuntimeUI.Label(panel, "标题", gateTitle, 38, TextAnchor.MiddleCenter,
            new Color(1f, 0.86f, 0.6f), font, new Vector2(900f, 56f), new Vector2(0f, 200f));

        RuntimeUI.Label(panel, "说明", introText, 26, TextAnchor.MiddleCenter,
            new Color(0.86f, 0.87f, 0.9f), font, new Vector2(900f, 90f), new Vector2(0f, 130f));

        listText = RuntimeUI.Label(panel, "物品", "", 28, TextAnchor.UpperLeft,
            new Color(0.9f, 0.92f, 0.95f), font, new Vector2(820f, 220f), new Vector2(0f, -20f));

        hintText = RuntimeUI.Label(panel, "提示", "", 24, TextAnchor.MiddleCenter,
            new Color(0.7f, 0.72f, 0.76f), font, new Vector2(900f, 44f), new Vector2(0f, -210f));
    }

    void Say(string text)
    {
        if (hintText != null) hintText.text = text;
        if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast(text);
    }

    void Refresh()
    {
        if (listText == null) return;

        if (items.Count == 0)
        {
            listText.text = "背包是空的。";
        }
        else
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                sb.Append(i == cursor ? "▶ " : "   ");
                sb.Append(items[i].Key == DebugKey ? requiredItemName + "（调试用）" : GameProgress.Ensure().ItemName(items[i].Key));
                sb.Append("  ×");
                sb.Append(items[i].Value);
                sb.AppendLine();
            }
            listText.text = sb.ToString();
        }

        if (hintText != null)
            hintText.text = "W / S 选 · 空格 投进去 · Esc 离开（要投「" + requiredItemName + "」）";
    }
}