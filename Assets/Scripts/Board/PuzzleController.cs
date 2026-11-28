using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 内层棋盘（白盒）：一个可以真的玩的最小棋盘。
///
/// 玩法：先点上面一个技能，再点棋盘里的一格 —— 技能形状覆盖到的格子会翻转（亮↔暗）。
/// 每个技能有次数限制，用完就没了。可用格全部亮起 = 通关。
///
/// UI 是运行时用 RuntimeUI 搭的（不依赖场景里预先摆好的界面），
/// 想改颜色/大小/技能，改 Inspector 上的字段就行。
/// </summary>
public class PuzzleController : EventRoomBase
{
    [Header("关卡数据（可选）")]
    [Tooltip("策划给的 JSON（Assets/Resources/Board/*.json）。填了就以它为准，覆盖下面的宽/高/哑格/技能。")]
    public TextAsset layoutJson;

    [Header("棋盘（没填 JSON 时用这里的值）")]
    [Tooltip("宽（格）")]
    public int width = 5;
    [Tooltip("高（格）")]
    public int height = 5;
    [Tooltip("哑格，格式 x,y（左下角是 0,0）")]
    public string[] blockedCells = new string[0];

    [Tooltip("勾上：技能会把格子翻转（亮↔暗），能拿来做唯一解的关卡；不勾：只点亮，白盒默认，保证打得通。")]
    public bool toggleCells = false;

    [Tooltip("覆盖率到多少算通关。1 = 100%")]
    [Range(0.1f, 1f)]
    public float clearThreshold = 1f;

    [Header("技能（留空就用默认三件）")]
    public PuzzleSkill[] skills;

    [Header("外观")]
    public Font font;
    public float cellSize = 112f;
    public float gap = 10f;
    public Color litColor = new Color(0.98f, 0.85f, 0.52f);
    public Color darkColor = new Color(0.16f, 0.18f, 0.22f);
    public Color blockedColor = new Color(0.07f, 0.08f, 0.10f);
    public Color frameColor = new Color(0.10f, 0.11f, 0.14f);
    public Color textColor = new Color(0.92f, 0.92f, 0.90f);
    public Color accentColor = new Color(1f, 0.78f, 0.45f);

    /// <summary>覆盖率变化时触发：(0~1 的覆盖率, 亮格数, 可用格数)</summary>
    public event Action<float, int, int> CoverageChanged;

    /// <summary>某一格状态变化时触发：(x, y, 现在是否亮)</summary>
    public event Action<int, int, bool> CellChanged;

    /// <summary>用了技能时触发：(技能序号, 技能名, 剩余次数)</summary>
    public event Action<int, string, int> SkillUsed;

    /// <summary>通关时触发一次</summary>
    public event Action Cleared;

    /// <summary>当前覆盖率（0~1），外部系统直接读这个。</summary>
    public float Coverage { get { return board == null ? 0f : board.Coverage; } }

    /// <summary>是否已经通关。</summary>
    public bool IsCleared { get { return finished; } }

    /// <summary>棋盘数据（只读用途：外部做小地图/存档时用）。</summary>
    public PuzzleBoard Board { get { return board; } }

    PuzzleBoard board;
    readonly List<Image> cellImages = new List<Image>();
    readonly List<Button> skillButtons = new List<Button>();
    readonly List<Text> skillTexts = new List<Text>();
    readonly List<Text> skillChargeTexts = new List<Text>();

    Text coverageText;
    Text hintText;
    Text winText;
    int[] initialCharges;
    string[] pendingLit;
    int selectedSkill = -1;
    bool finished;

    /// <summary>不画那个橙色占位方块（棋盘自己就是可见的）。</summary>
    protected override void Awake() { }

    protected override void Start()
    {
        base.Start();
        BuildBoard();
        BuildUI();
        Refresh();
    }

    void BuildBoard()
    {
        ApplyLayoutIfAny();

        if (skills == null || skills.Length == 0)
        {
            skills = new[]
            {
                new PuzzleSkill("微光", PuzzleShape.Single, 6),
                new PuzzleSkill("回响", PuzzleShape.Cross, 4),
                new PuzzleSkill("共振", PuzzleShape.Square3, 2),
            };
        }

        initialCharges = new int[skills.Length];
        for (int i = 0; i < skills.Length; i++) initialCharges[i] = skills[i].charges;

        board = new PuzzleBoard(width, height);
        board.SetBlockedCells(blockedCells);

        if (pendingLit != null)
        {
            foreach (var raw in pendingLit)
            {
                if (string.IsNullOrEmpty(raw)) continue;
                var parts = raw.Split(',');
                if (parts.Length != 2) continue;
                int x, y;
                if (int.TryParse(parts[0].Trim(), out x) && int.TryParse(parts[1].Trim(), out y))
                    board.SetLit(x, y, true);
            }
            pendingLit = null;
        }
    }

    // ---------------- 界面 ----------------

    void BuildUI()
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvas = RuntimeUI.CreateCanvas("棋盘UI", 50);
        RuntimeUI.Panel(canvas.transform, "背景", new Vector2(4000f, 3000f), Vector2.zero, frameColor);

        RuntimeUI.Label(canvas.transform, "标题", "事件一 · 内层棋盘（白盒）", 42,
            TextAnchor.MiddleCenter, textColor, font, new Vector2(1200f, 60f), new Vector2(0f, 440f));

        coverageText = RuntimeUI.Label(canvas.transform, "覆盖率", "", 30,
            TextAnchor.MiddleCenter, accentColor, font, new Vector2(1000f, 44f), new Vector2(0f, 384f));

        // 棋盘
        var boardRoot = new GameObject("棋盘", typeof(RectTransform));
        boardRoot.transform.SetParent(canvas.transform, false);
        var boardRect = boardRoot.GetComponent<RectTransform>();
        boardRect.anchorMin = boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.pivot = new Vector2(0.5f, 0.5f);
        boardRect.anchoredPosition = new Vector2(0f, 60f);
        boardRect.sizeDelta = new Vector2(width * (cellSize + gap), height * (cellSize + gap));

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int cx = x, cy = y;
                var cell = RuntimeUI.Panel(boardRoot.transform, "格_" + x + "_" + y,
                    new Vector2(cellSize, cellSize), CellPosition(x, y), darkColor);
                var image = cell.GetComponent<Image>();
                image.raycastTarget = true;

                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => OnCellClicked(cx, cy));

                var label = RuntimeUI.Label(cell, "字", "", 30, TextAnchor.MiddleCenter,
                    new Color(0.85f, 0.87f, 0.9f, 0.55f), font,
                    new Vector2(cellSize, cellSize), Vector2.zero);
                if (board.IsBlocked(cx, cy)) label.text = "—";

                cellImages.Add(image);
            }
        }

        // 技能按钮
        float totalWidth = skills.Length * 230f + (skills.Length - 1) * 16f;
        for (int i = 0; i < skills.Length; i++)
        {
            int index = i;
            float x = -totalWidth * 0.5f + 115f + i * 246f;

            var slot = RuntimeUI.Panel(canvas.transform, "技能_" + skills[i].name,
                new Vector2(230f, 76f), new Vector2(x, -300f), new Color(0.18f, 0.20f, 0.25f, 1f));
            var image = slot.GetComponent<Image>();
            image.raycastTarget = true;

            var button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SelectSkill(index));
            skillButtons.Add(button);

            skillTexts.Add(RuntimeUI.Label(slot, "名", skills[i].name, 30,
                TextAnchor.MiddleCenter, textColor, font, new Vector2(220f, 40f), new Vector2(0f, 10f)));

            skillChargeTexts.Add(RuntimeUI.Label(slot, "次", "", 24,
                TextAnchor.MiddleCenter, accentColor, font, new Vector2(220f, 30f), new Vector2(0f, -16f)));
        }

        // 重置
        var reset = RuntimeUI.Panel(canvas.transform, "重置", new Vector2(170f, 56f),
            new Vector2(760f, -430f), new Color(0.18f, 0.20f, 0.25f, 1f));
        var resetImage = reset.GetComponent<Image>();
        resetImage.raycastTarget = true;
        var resetButton = reset.gameObject.AddComponent<Button>();
        resetButton.targetGraphic = resetImage;
        resetButton.onClick.AddListener(ResetPuzzle);
        RuntimeUI.Label(reset, "字", "重置 (R)", 26, TextAnchor.MiddleCenter, textColor, font,
            new Vector2(170f, 56f), Vector2.zero);

        hintText = RuntimeUI.Label(canvas.transform, "提示", "先点技能，再点格子 · R 重置 · Esc 退出", 24,
            TextAnchor.MiddleCenter, new Color(0.7f, 0.72f, 0.75f), font,
            new Vector2(1400f, 40f), new Vector2(0f, -430f));

        winText = RuntimeUI.Label(canvas.transform, "通关", "", 46, TextAnchor.MiddleCenter,
            accentColor, font, new Vector2(1200f, 70f), new Vector2(0f, 210f));
        winText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 有 JSON 就以 JSON 为准：大小、哑格、技能、初始亮格、翻转开关、通关阈值。
    /// 没有 JSON 就用 Inspector 上的值。
    /// </summary>
    void ApplyLayoutIfAny()
    {
        var layout = PuzzleLayoutLoader.Load(layoutJson);
        if (layout == null) return;

        if (layout.width > 0) width = layout.width;
        if (layout.height > 0) height = layout.height;

        if (layout.blockedCells != null) blockedCells = layout.blockedCells;

        toggleCells = layout.toggleCells;
        if (layout.clearThreshold > 0f) clearThreshold = layout.clearThreshold;

        if (layout.skills != null && layout.skills.Count > 0)
        {
            var list = new List<PuzzleSkill>();
            foreach (var s in layout.skills)
            {
                list.Add(new PuzzleSkill(s.name, PuzzleLayoutLoader.ParseShape(s.shape), s.charges));
            }
            skills = list.ToArray();
        }

        pendingLit = layout.litCells;

        Debug.Log("[棋盘] 已按 JSON 加载：" + layoutJson.name + "（" + width + "×" + height + "，" +
                  (skills == null ? 0 : skills.Length) + " 个技能）");
    }

    Vector2 CellPosition(int x, int y)
    {
        float step = cellSize + gap;
        return new Vector2((x - (width - 1) * 0.5f) * step, (y - (height - 1) * 0.5f) * step);
    }

    // ---------------- 交互 ----------------

    void SelectSkill(int index)
    {
        if (finished) return;
        if (skills[index].charges <= 0)
        {
            SetHint("「" + skills[index].name + "」已经用完了");
            return;
        }

        selectedSkill = index;
        SetHint("已选「" + skills[index].name + "」，点棋格里的一格");
        Refresh();
    }

    void OnCellClicked(int x, int y)
    {
        if (finished) return;
        if (board.IsBlocked(x, y))
        {
            SetHint("这格是哑格，不能点");
            return;
        }

        if (selectedSkill < 0)
        {
            SetHint("先在下面选一个技能");
            return;
        }

        var skill = skills[selectedSkill];
        if (skill.charges <= 0)
        {
            SetHint("「" + skill.name + "」已经用完了");
            return;
        }

        int skillIndex = selectedSkill;
        int changed = board.Apply(skill.shape, new Vector2Int(x, y), toggleCells);
        if (changed <= 0)
        {
            SetHint("这一下没碰到任何格子");
            return;
        }

        skill.charges--;
        if (skill.charges <= 0) selectedSkill = -1;

        Refresh();
        SkillUsed?.Invoke(skillIndex, skill.name, skill.charges);

        if (!finished && board.Coverage >= clearThreshold - 0.0001f)
        {
            finished = true;
            StartCoroutine(WinRoutine());
        }
    }

    IEnumerator WinRoutine()
    {
        Cleared?.Invoke();
        winText.text = "全部点亮 · 通关";
        winText.gameObject.SetActive(true);
        SetHint("事件一完成，正在返回第 0 层…");
        ShelterHUD.Instance?.Toast("覆盖 " + Mathf.RoundToInt(board.Coverage * 100f) + "% · 事件一完成");

        yield return new WaitForSecondsRealtime(1.4f);
        Complete();
    }

    /// <summary>重置棋盘。restoreCharges = true 时技能次数也回到初始值（外部调也行）。</summary>
    public void ResetBoard(bool restoreCharges = false)
    {
        if (finished) return;
        board.Reset();

        if (restoreCharges && initialCharges != null)
        {
            for (int i = 0; i < skills.Length && i < initialCharges.Length; i++)
                skills[i].charges = initialCharges[i];
        }

        selectedSkill = -1;
        SetHint(restoreCharges ? "棋盘与技能次数都已重置" : "棋盘已清空（技能次数不返还）");
        Refresh();
    }

    void ResetPuzzle() { ResetBoard(false); }

    void SetHint(string text)
    {
        if (hintText != null) hintText.text = text;
    }

    void Refresh()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var image = cellImages[y * width + x];
                if (image == null) continue;

                if (board.IsBlocked(x, y))
                {
                    image.color = blockedColor;
                }
                else
                {
                    bool on = board.IsLit(x, y);
                    image.color = on ? litColor : darkColor;
                    CellChanged?.Invoke(x, y, on);
                }
            }
        }

        for (int i = 0; i < skills.Length; i++)
        {
            bool usable = skills[i].charges > 0;
            skillChargeTexts[i].text = "× " + skills[i].charges;
            skillChargeTexts[i].color = usable ? accentColor : new Color(0.5f, 0.5f, 0.5f);

            var image = skillButtons[i].GetComponent<Image>();
            if (i == selectedSkill) image.color = new Color(0.95f, 0.72f, 0.35f, 1f);
            else image.color = usable ? new Color(0.22f, 0.25f, 0.31f, 1f) : new Color(0.13f, 0.14f, 0.16f, 1f);

            skillTexts[i].color = usable ? textColor : new Color(0.55f, 0.55f, 0.55f);
            skillButtons[i].interactable = usable;
        }

        int lit = board.LitCount;
        int playable = board.PlayableCount;
        int percent = Mathf.RoundToInt(board.Coverage * 100f);
        if (coverageText != null)
            coverageText.text = "覆盖 " + lit + " / " + playable + " · " + percent + "%";

        CoverageChanged?.Invoke(board.Coverage, lit, playable);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!finished) Leave();
            return;
        }

        if (Input.GetKeyDown(KeyCode.R)) ResetPuzzle();
    }
}
