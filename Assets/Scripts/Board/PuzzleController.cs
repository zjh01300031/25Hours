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

    [Tooltip("通关后回哪个场景。填了（比如 Event_01）就只回这一层，不回第0层")]
    public string returnScene;

    [Header("小人（WASD 走动，空格放技能）")]
    [Tooltip("留空就用内置白方块。想换成立绘就拖 Assets/Art/Characters/player.png 进来")]
    public Sprite avatarSprite;
    [Tooltip("走动时每格间隔（秒）")]
    public float moveRepeat = 0.14f;

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

    int startX = -1, startY = -1;   // 设计稿给的起始格；<0 表示用默认
    int moveLimit;                  // 设计稿给的放技能次数上限；0 = 不限
    int movesUsed;

    [Tooltip("白盒阶段：按 Tab 直接把所有可用格点亮并通关，方便连着看下一块棋盘。上线前关掉")]
    public bool allowDebugClear = true;

    Image avatarImage;
    Vector2Int cursor;
    float nextMove;

    Vector2 avatarFrom, avatarTo;
    float avatarT = 1f;
    float avatarStep = 0.085f;   // 一格走多久（越小越快）
    float castCooldown;          // 施法冷却，防止连按空格抖
    const float CastInterval = 0.12f;

    /// <summary>不画那个橙色占位方块（棋盘自己就是可见的）。</summary>
    protected override void Awake() { }

    protected override void Start()
    {
        base.Start();
        RemoveEditorPreview();
        BuildBoard();
        BuildUI();
        if (selectedSkill < 0 && skills.Length > 0) selectedSkill = 0;
        if (startX >= 0 && startX < width && startY >= 0 && startY < height)
            cursor = new Vector2Int(startX, startY);
        Refresh();
        SnapCursor();
    }

    /// <summary>进入 Play 时先删掉编辑模式留下的静态预览，再画能点的那套。</summary>
    static void RemoveEditorPreview()
    {
        var previews = FindObjectsOfType<BoardPreviewMarker>(true);
        for (int i = 0; i < previews.Length; i++)
            Destroy(previews[i].gameObject);
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

        string titleText = string.IsNullOrEmpty(BoardSession.DollName)
            ? "事件一 · 内层棋盘（白盒）"
            : "事件一 · " + BoardSession.DollName + " 的内层棋盘";
        RuntimeUI.Label(canvas.transform, "标题", titleText, 42,
            TextAnchor.MiddleCenter, textColor, font, new Vector2(1200f, 60f), new Vector2(0f, 470f));

        coverageText = RuntimeUI.Label(canvas.transform, "覆盖率", "", 30,
            TextAnchor.MiddleCenter, accentColor, font, new Vector2(1000f, 44f), new Vector2(0f, 412f));

        // 棋盘
        var boardRoot = new GameObject("棋盘", typeof(RectTransform));
        boardRoot.transform.SetParent(canvas.transform, false);
        var boardRect = boardRoot.GetComponent<RectTransform>();
        boardRect.anchorMin = boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.pivot = new Vector2(0.5f, 0.5f);
        boardRect.anchoredPosition = new Vector2(0f, 0f);
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

        // 小人
        cursor = new Vector2Int(width / 2, height / 2);
        var avatar = RuntimeUI.Panel(boardRoot.transform, "小人",
            new Vector2(cellSize * 0.5f, cellSize * 0.72f), CellPosition(cursor.x, cursor.y), Color.white);
        avatarImage = avatar.GetComponent<Image>();
        var sprite = BoardSession.DollSprite != null ? BoardSession.DollSprite : avatarSprite;
        avatarImage.sprite = sprite != null ? sprite : Marker.White;
        avatarImage.type = Image.Type.Simple;
        avatarImage.color = new Color(0.9f, 0.33f, 0.28f, 1f);
        avatarImage.raycastTarget = false;

        // 技能按钮
        float totalWidth = skills.Length * 230f + (skills.Length - 1) * 16f;
        for (int i = 0; i < skills.Length; i++)
        {
            int index = i;
            float x = -totalWidth * 0.5f + 115f + i * 246f;

            var slot = RuntimeUI.Panel(canvas.transform, "技能_" + skills[i].name,
                new Vector2(230f, 76f), new Vector2(x, -350f), new Color(0.18f, 0.20f, 0.25f, 1f));
            var image = slot.GetComponent<Image>();
            image.raycastTarget = true;

            var button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => SelectSkill(index));
            skillButtons.Add(button);

            BuildSkillIcon(slot, skills[i].shape);

            skillChargeTexts.Add(RuntimeUI.Label(slot, "次", "", 24,
                TextAnchor.MiddleCenter, accentColor, font, new Vector2(220f, 30f), new Vector2(0f, -16f)));
        }

        // 重置
        var reset = RuntimeUI.Panel(canvas.transform, "重置", new Vector2(170f, 56f),
            new Vector2(820f, -350f), new Color(0.18f, 0.20f, 0.25f, 1f));
        var resetImage = reset.GetComponent<Image>();
        resetImage.raycastTarget = true;
        var resetButton = reset.gameObject.AddComponent<Button>();
        resetButton.targetGraphic = resetImage;
        resetButton.onClick.AddListener(ResetPuzzle);
        RuntimeUI.Label(reset, "字", "重置 (R)", 26, TextAnchor.MiddleCenter, textColor, font,
            new Vector2(170f, 56f), Vector2.zero);

        hintText = RuntimeUI.Label(canvas.transform, "提示", "WASD / 方向键 走路 · 空格 放技能 · 1/2/3 换技能 · R 重置 · Esc 退出　　Tab 直接完成（调试）", 24,
            TextAnchor.MiddleCenter, new Color(0.7f, 0.72f, 0.75f), font,
            new Vector2(1400f, 40f), new Vector2(0f, -430f));

        winText = RuntimeUI.Label(canvas.transform, "通关", "", 46, TextAnchor.MiddleCenter,
            accentColor, font, new Vector2(1200f, 70f), new Vector2(0f, 0f));
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

        if (layout.startX >= 0) { startX = layout.startX; startY = layout.startY; }
        moveLimit = layout.moveLimit;
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

    /// <summary>技能按钮里画一个 3×3 的小图：亮起来的那几格就是这个技能会点亮的位置。</summary>
    void BuildSkillIcon(RectTransform slot, PuzzleShape shape)
    {
        var offsets = new List<Vector2Int>(PuzzleSkill.Offsets(shape));
        float cell = 16f, step = 20f;

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                bool on = offsets.Contains(new Vector2Int(x, y));
                var pos = new Vector2(x * step, 8f + y * step);
                var dot = RuntimeUI.Panel(slot, "点", new Vector2(cell, cell), pos,
                    on ? accentColor : new Color(0.45f, 0.48f, 0.54f, 0.35f));
                var image = dot.GetComponent<Image>();
                image.raycastTarget = false;
            }
        }
    }

    void SelectSkill(int index)
    {
        if (finished) return;
        if (skills[index].charges <= 0)
        {
            SetHint("「" + skills[index].name + "」已经用完了");
            return;
        }

        selectedSkill = index;
        var slotImage = skillButtons[index].GetComponent<Image>();
        if (slotImage != null && isActiveAndEnabled) StartCoroutine(PulseRoutine(slotImage, 0f));
        SetHint("已选第 " + (index + 1) + " 个技能（看按钮上的小图案），空格或点格子放");
        Refresh();
    }

    void OnCellClicked(int x, int y)
    {
        if (finished) return;
        cursor = new Vector2Int(x, y);
        UpdateCursor();
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
        var affected = new List<Vector2Int>(PuzzleSkill.Offsets(skill.shape));
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

        // 施法反馈：被点到的格子一个个闪，小人弹一下
        for (int i = 0; i < affected.Count; i++)
        {
            int ax = x + affected[i].x;
            int ay = y + affected[i].y;
            if (ax < 0 || ax >= width || ay < 0 || ay >= height) continue;
            if (board.IsBlocked(ax, ay)) continue;
            float delay = Mathf.Abs(affected[i].x) + Mathf.Abs(affected[i].y);
            PulseCell(ax, ay, delay * 0.022f);
        }
        PulseAvatar();

        if (!finished && board.Coverage >= clearThreshold - 0.0001f)
        {
            finished = true;
            StartCoroutine(WinRoutine());
        }
    }

    void DebugClear()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (!board.IsBlocked(x, y)) board.SetLit(x, y, true);

        Refresh();
        SetHint("[调试] 一键完成：全部点亮");

        if (!finished)
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

        if (!string.IsNullOrEmpty(returnScene) && Application.CanStreamedLevelBeLoaded(returnScene))
        {
            GameProgress.Ensure().MarkEventCleared(EventId);
            SceneFade.FadeTo(returnScene, 0.45f, 0.3f, 0.85f);
            yield break;
        }

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
        movesUsed = 0;
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

            if (i < skillTexts.Count) skillTexts[i].color = usable ? textColor : new Color(0.55f, 0.55f, 0.55f);
            skillButtons[i].interactable = usable;
        }

        int lit = board.LitCount;
        int playable = board.PlayableCount;
        int percent = Mathf.RoundToInt(board.Coverage * 100f);
        if (coverageText != null)
            coverageText.text = "覆盖 " + lit + " / " + playable + " · " + percent + "%"
            + (moveLimit > 0 ? "　·　已用 " + movesUsed + " / " + moveLimit + " 步" : "");

        CoverageChanged?.Invoke(board.Coverage, lit, playable);
    }

    Vector2 CursorPosition()
    {
        return CellPosition(cursor.x, cursor.y) + new Vector2(0f, cellSize * 0.3f);
    }

    void SnapCursor()
    {
        if (avatarImage == null) return;
        avatarTo = CursorPosition();
        avatarFrom = avatarTo;
        avatarT = 1f;
        avatarImage.rectTransform.anchoredPosition = avatarTo;
    }

    void UpdateCursor()
    {
        if (avatarImage == null) return;
        avatarFrom = avatarImage.rectTransform.anchoredPosition;
        avatarTo = CursorPosition();
        avatarT = 0f;
    }

    /// <summary>每帧把小人往目标格平滑推进（不是瞬移）。</summary>
    void AnimateAvatar()
    {
        if (avatarImage == null || avatarT >= 1f) return;
        avatarT = Mathf.Min(1f, avatarT + Time.deltaTime / Mathf.Max(0.02f, avatarStep));
        float k = Mathf.SmoothStep(0f, 1f, avatarT);
        avatarImage.rectTransform.anchoredPosition = Vector2.LerpUnclamped(avatarFrom, avatarTo, k);
    }

    /// <summary>格子闪一下：亮起来的地方放大 + 提亮，再回落到正常色。</summary>
    void PulseCell(int x, int y, float delay = 0f)
    {
        int index = y * width + x;
        if (index < 0 || index >= cellImages.Count) return;
        var image = cellImages[index];
        if (image != null && isActiveAndEnabled) StartCoroutine(PulseRoutine(image, delay));
    }

    IEnumerator PulseRoutine(Image image, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        if (image == null) yield break;

        var rt = image.rectTransform;
        Color baseColor = image.color;
        Color flash = Color.Lerp(baseColor, Color.white, 0.65f);
        float t = 0f, dur = 0.18f;

        while (t < dur && image != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            image.color = Color.Lerp(flash, baseColor, k);
            rt.localScale = Vector3.one * (1.12f - 0.12f * k);
            yield return null;
        }

        if (image != null)
        {
            image.color = baseColor;
            rt.localScale = Vector3.one;
        }
    }

    void PulseAvatar()
    {
        if (avatarImage != null && isActiveAndEnabled) StartCoroutine(PulseAvatarRoutine());
    }

    IEnumerator PulseAvatarRoutine()
    {
        var rt = avatarImage.rectTransform;
        float t = 0f, dur = 0.14f;
        while (t < dur && avatarImage != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            rt.localScale = Vector3.one * (1f + 0.14f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        if (avatarImage != null) rt.localScale = Vector3.one;
    }

    void MoveCursor(int dx, int dy)
    {
        var next = cursor;
        if (dx != 0) next = new Vector2Int(Mathf.Clamp(cursor.x + dx, 0, width - 1), cursor.y);
        else if (dy != 0) next = new Vector2Int(cursor.x, Mathf.Clamp(cursor.y + dy, 0, height - 1));

        if (next == cursor) return;
        if (board.IsBlocked(next.x, next.y)) { SetHint("那边是哑格，走不过去"); return; }

        cursor = next;
        UpdateCursor();
    }

    void CastSelected()
    {
        if (selectedSkill < 0) { SetHint("先按 1/2/3 选一个技能"); return; }
        if (moveLimit > 0 && movesUsed >= moveLimit)
        {
            SetHint("步数用完了（" + movesUsed + " / " + moveLimit + "）· 按 R 重来");
            return;
        }
        if (finished) return;

        int before = board.LitCount;
        OnCellClicked(cursor.x, cursor.y);
        if (board.LitCount != before) movesUsed++;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!finished) Leave();
            return;
        }

        if (finished) return;

        if (Input.GetKeyDown(KeyCode.R)) { ResetPuzzle(); return; }

        if (allowDebugClear && Input.GetKeyDown(KeyCode.Tab))
        {
            DebugClear();
            return;
        }

        for (int i = 0; i < skills.Length && i < 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SelectSkill(i);

        AnimateAvatar();
        if (castCooldown > 0f) castCooldown -= Time.deltaTime;

        bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        int dx = right ? 1 : (left ? -1 : 0);
        int dy = up ? 1 : (down ? -1 : 0);
        if (dx != 0 && dy != 0) dy = 0;   // 斜着按就先走横向

        bool pressedNow =
            Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow);

        if ((dx != 0 || dy != 0) && (pressedNow || Time.time >= nextMove))
        {
            MoveCursor(dx, dy);
            nextMove = Time.time + (pressedNow ? 0.26f : moveRepeat);
        }

        if (Input.GetKeyDown(KeyCode.Space) && castCooldown <= 0f)
        {
            CastSelected();
            castCooldown = CastInterval;
        }
    }
}
