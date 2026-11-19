using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 《第 25 小时》L1-1 原型（策划示意图 v2：外层探索 + 三座装置内的棋盘）
/// 外层：走廊里和 NPC 说话、翻找三件旧物拿碎片文案、走到三座装置前交互进入棋盘
/// 内层：5x5 棋盘，棋子按方向走 N 格（1 次行动），技能不消耗行动，点亮所有非障碍格
/// 操作：外层 A/D 移动、E 交互；内层 方向键走向、回车结束本次移动、1~4 施放技能、R 重置、ESC 退出
/// </summary>
public class LevelL1_1 : MonoBehaviour
{
    enum Phase { Corridor, Board, Chapter }

    Phase phase = Phase.Corridor;

    // ---------------- 棋盘配置（三座装置各一） ----------------
    static readonly Vector2Int[][] BlockedSets =
    {
        new[] { new Vector2Int(2, 2) },
        new[] { new Vector2Int(2, 2), new Vector2Int(4, 4) },
        new[] { new Vector2Int(2, 2), new Vector2Int(0, 4), new Vector2Int(4, 2) },
    };

    const int BN = 5;
    const int MoveBudget = 8;
    static readonly int[] SkillMaxAll = { 4, 1, 2, 2 };   // 2x2, 十字, 横3, 竖3
    static readonly string[] SkillName = { "2x2", "十字", "横3", "竖3" };
    static readonly float[] DeviceX = { -7.0f, 0.0f, 5.6f };

    // ---------------- 外层 ----------------
    Camera cam;
    Transform player;
    Transform door;
    Vector3 doorOpenPos;
    float px = -8.4f;
    const float Speed = 4.2f;
    const float MinX = -9.2f;
    const float MaxX = 9.6f;
    const float Range = 1.5f;

    class Thing
    {
        public string name;
        public float x;
        public string text;
        public bool found;
        public SpriteRenderer sr;
    }

    readonly List<Thing> things = new List<Thing>();
    readonly SpriteRenderer[] deviceSr = new SpriteRenderer[3];
    int fragCount;
    bool taskGiven, doorOpen;
    readonly bool[] boardSolved = new bool[3];
    bool dialogOpen;
    readonly List<string> lines = new List<string>();
    int lineIdx;
    string hint = "";
    float hintUntil;
    bool finishing;
    float fade;
    float fadeIn;

    // ---------------- 内层棋盘 ----------------
    int boardIndex;
    readonly bool[,] blocked = new bool[BN, BN];
    readonly bool[,] lit = new bool[BN, BN];
    readonly float[,] flashAt = new float[BN, BN];
    Vector2Int pawn;
    Vector3 pawnVisual;
    int movesLeft;
    int litCount;
    readonly int[] skillLeft = new int[4];
    bool moving;
    int moveDir;
    const float CellSize = 78f;

    // ---------------- 音频 ----------------
    AudioSource audioSrc;
    AudioClip clipCast, clipLit, clipMove, clipSolve, clipDoor, clipPick, clipUi;

    // ---------------- 界面 ----------------
    Font cnFont;
    GUIStyle sBody, sTitle, sSmall, sCenter, sBig;

    static Color ColdBg { get { return new Color(0.09f, 0.10f, 0.14f); } }

    // =========================================================== 启动
    void Awake()
    {
        Application.targetFrameRate = 60;
        BuildAudio();
        BuildWorld();
    }

    Sprite White()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    SpriteRenderer Block(string name, Transform parent, Vector2 pos, Vector2 size, Color color, int order, bool local = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (local) go.transform.localPosition = pos; else go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = White();
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    void BuildWorld()
    {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColdBg;
        cam.transform.position = new Vector3(-5.5f, 0.6f, -10f);

        var root = new GameObject("World").transform;
        Block("BackWall", root, new Vector2(0f, 0.4f), new Vector2(28f, 7f), new Color(0.10f, 0.11f, 0.15f), -5);
        Block("Floor", root, new Vector2(0f, -2.6f), new Vector2(28f, 1.2f), new Color(0.16f, 0.17f, 0.22f), 0);
        Block("Ceiling", root, new Vector2(0f, 3.5f), new Vector2(28f, 0.5f), new Color(0.13f, 0.14f, 0.19f), -4);

        AddThing("旧收音机", -5f, new Vector2(0.8f, 0.5f), new Color(0.55f, 0.48f, 0.40f), 5, -1.75f,
            "收音机里只剩一段沙沙声：「……第 25 个小时，钟停在 24。」");
        AddThing("风铃", -1.4f, new Vector2(0.3f, 0.9f), new Color(0.62f, 0.68f, 0.78f), 5, 0.9f,
            "风铃上刻着一行小字：「有人听着，灯才算亮。」");
        AddThing("水管", 2.6f, new Vector2(0.36f, 1.0f), new Color(0.42f, 0.50f, 0.52f), 5, -1.5f,
            "水管深处传来回声：「……别把它喊哑。」");

        for (int i = 0; i < 3; i++)
        {
            deviceSr[i] = Block("Device" + i, root, new Vector2(DeviceX[i], -1.35f), new Vector2(0.9f, 1.3f),
                new Color(0.35f, 0.30f, 0.55f), 4);
            Block("DeviceGlow" + i, root, new Vector2(DeviceX[i], -1.35f), new Vector2(1.5f, 1.8f),
                new Color(0.55f, 0.45f, 1f, 0.10f), 3);
        }

        var doorSr = Block("Door", root, new Vector2(8.8f, -0.5f), new Vector2(1.4f, 3.0f), new Color(0.26f, 0.20f, 0.16f), 1);
        door = doorSr.transform;
        doorOpenPos = new Vector3(door.position.x, door.position.y + 3.4f, door.position.z);

        Block("NPC", root, new Vector2(7.4f, -1.15f), new Vector2(0.8f, 1.7f), new Color(0.78f, 0.74f, 0.62f), 2);
        Block("NPCHead", root, new Vector2(7.4f, -0.10f), new Vector2(0.52f, 0.52f), new Color(0.90f, 0.86f, 0.74f), 3);

        var p = Block("Player", root, new Vector2(px, -1.25f), new Vector2(0.7f, 1.5f), new Color(0.55f, 0.78f, 0.95f), 6);
        Block("PlayerHead", p.transform, new Vector2(0f, 0.62f), new Vector2(1f, 0.5f), new Color(0.93f, 0.93f, 0.90f), 1, true);
        player = p.transform;
    }

    void AddThing(string name, float x, Vector2 size, Color color, int order, float y, string text)
    {
        var sr = Block(name, transform, new Vector2(x, y), size, color, order);
        things.Add(new Thing { name = name, x = x, text = text, sr = sr });
    }

    // =========================================================== 音频
    void BuildAudio()
    {
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.volume = 0.5f;
        clipCast = MakeSweep("cast", 0.22f, 660f, 300f, 0.35f);
        clipLit = MakeSweep("lit", 0.16f, 1320f, 880f, 0.30f);
        clipMove = MakeNoise("move", 0.07f, 0.18f);
        clipSolve = MakeArp("solve", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.11f, 0.32f);
        clipDoor = MakeSweep("door", 0.8f, 150f, 70f, 0.30f);
        clipPick = MakeArp("pick", new[] { 1046.5f, 1318.5f }, 0.09f, 0.26f);
        clipUi = MakeSweep("ui", 0.09f, 900f, 700f, 0.18f);
    }

    AudioClip MakeSweep(string name, float sec, float f0, float f1, float amp)
    {
        int n = Mathf.CeilToInt(sec * 44100f);
        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(f0, f1, t);
            phase += 2f * Mathf.PI * f / 44100f;
            float env = Mathf.Pow(1f - t, 1.6f);
            data[i] = Mathf.Sign(Mathf.Sin(phase)) * env * amp * 0.6f;
        }
        var clip = AudioClip.Create(name, n, 1, 44100, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip MakeNoise(string name, float sec, float amp)
    {
        int n = Mathf.CeilToInt(sec * 44100f);
        var data = new float[n];
        var rnd = new System.Random(7);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            data[i] = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Pow(1f - t, 3f) * amp;
        }
        var clip = AudioClip.Create(name, n, 1, 44100, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip MakeArp(string name, float[] freqs, float noteSec, float amp)
    {
        int per = Mathf.CeilToInt(noteSec * 44100f);
        int n = per * freqs.Length;
        var data = new float[n];
        for (int k = 0; k < freqs.Length; k++)
        {
            for (int i = 0; i < per; i++)
            {
                float t = (float)i / per;
                float env = Mathf.Pow(1f - t, 2.2f);
                data[k * per + i] = Mathf.Sin(2f * Mathf.PI * freqs[k] * i / 44100f) * env * amp;
            }
        }
        var clip = AudioClip.Create(name, n, 1, 44100, false);
        clip.SetData(data, 0);
        return clip;
    }

    void Play(AudioClip c, float vol = 1f)
    {
        if (audioSrc != null && c != null) audioSrc.PlayOneShot(c, vol);
    }

    // =========================================================== 每帧
    void Update()
    {
        if (phase == Phase.Corridor) UpdateCorridor();
        else if (phase == Phase.Board) UpdateBoard();
        else UpdateChapter();
    }

    int SolvedCount()
    {
        int n = 0;
        for (int i = 0; i < 3; i++) if (boardSolved[i]) n++;
        return n;
    }

    void UpdateCorridor()
    {
        if (dialogOpen)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                lineIdx++;
                Play(clipUi, 0.5f);
                if (lineIdx >= lines.Count) { dialogOpen = false; lines.Clear(); }
            }
            return;
        }

        if (finishing)
        {
            fade = Mathf.MoveTowards(fade, 1f, Time.deltaTime / 0.7f);
            if (fade >= 1f) phase = Phase.Chapter;
            return;
        }

        fadeIn = Mathf.MoveTowards(fadeIn, 0f, Time.deltaTime / 0.35f);

        float dir = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir += 1f;
        px = Mathf.Clamp(px + dir * Speed * Time.deltaTime, MinX, MaxX);
        player.position = new Vector3(px, player.position.y, player.position.z);

        float camX = Mathf.Clamp(px, -5.5f, 6.6f);
        cam.transform.position = new Vector3(Mathf.Lerp(cam.transform.position.x, camX, 1f - Mathf.Exp(-8f * Time.deltaTime)), 0.6f, -10f);

        if (doorOpen) door.position = Vector3.MoveTowards(door.position, doorOpenPos, 2.2f * Time.deltaTime);

        for (int i = 0; i < 3; i++)
        {
            var target = boardSolved[i] ? new Color(0.55f, 0.85f, 0.55f) : new Color(0.35f, 0.30f, 0.55f);
            deviceSr[i].color = Color.Lerp(deviceSr[i].color, target, Time.deltaTime * 3f);
        }

        if (Input.GetKeyDown(KeyCode.E)) Interact();
        if (doorOpen && px > 8.3f) finishing = true;
    }

    void Interact()
    {
        float bestD = Range;
        int bestKind = -1;   // 0=thing 1=device 2=npc
        int bestIdx = -1;

        for (int i = 0; i < things.Count; i++)
        {
            float d = Mathf.Abs(things[i].x - px);
            if (d <= bestD) { bestD = d; bestKind = 0; bestIdx = i; }
        }
        for (int i = 0; i < 3; i++)
        {
            float d = Mathf.Abs(DeviceX[i] - px);
            if (d <= bestD) { bestD = d; bestKind = 1; bestIdx = i; }
        }
        float dNpc = Mathf.Abs(7.4f - px);
        if (dNpc <= bestD) { bestD = dNpc; bestKind = 2; bestIdx = 0; }

        if (bestKind == 0)
        {
            var t = things[bestIdx];
            if (t.found) { ShowHint("已经翻过了。"); return; }
            t.found = true;
            fragCount++;
            t.sr.color = new Color(0.45f, 0.42f, 0.36f);
            Play(clipPick, 0.7f);
            ShowHint("碎片 " + fragCount + "/3：" + t.text);
            return;
        }
        if (bestKind == 1)
        {
            int i = bestIdx;
            if (boardSolved[i]) { ShowHint("这座装置已经亮了。"); return; }
            EnterBoard(i);
            return;
        }
        if (bestKind == 2)
        {
            if (SolvedCount() == 3)
                OpenDialog(new[] { "九：「门开了。」", "九：「第 25 个小时里，能听见自己的人不多。谢谢你。」" });
            else if (!taskGiven)
            {
                taskGiven = true;
                OpenDialog(new[]
                {
                    "九：「……你也是被这盏灯叫醒的？」",
                    "九：「这扇门后面有东西在喊。可我喊不出来——我的声音留在里面了。」",
                    "九：「走廊里有三座装置。你一座一座走进去，把它们点亮。」",
                });
            }
            else if (fragCount < 3)
                OpenDialog(new[] { "九：「走廊上的旧东西里，还留着几句话说。你翻翻看。」" });
            else
                OpenDialog(new[] { "九：「还有 " + (3 - SolvedCount()) + " 座装置没有亮。」", "九：「别急，灯是一格一格亮的。」" });
            return;
        }

        ShowHint("这里没有可以交互的东西。");
    }

    void OpenDialog(string[] content)
    {
        lines.Clear();
        lines.AddRange(content);
        lineIdx = 0;
        dialogOpen = true;
        Play(clipUi, 0.6f);
    }

    void ShowHint(string text)
    {
        hint = text;
        hintUntil = Time.time + 3.6f;
    }

    // =========================================================== 内层棋盘
    void EnterBoard(int idx)
    {
        boardIndex = idx;
        for (int r = 0; r < BN; r++)
            for (int c = 0; c < BN; c++)
            {
                blocked[r, c] = false;
                lit[r, c] = false;
                flashAt[r, c] = -10f;
            }
        foreach (var b in BlockedSets[idx]) blocked[b.y, b.x] = true;

        pawn = new Vector2Int(0, 0);
        pawnVisual = new Vector3(0, 0, 0);
        movesLeft = MoveBudget;
        for (int i = 0; i < 4; i++) skillLeft[i] = SkillMaxAll[i];
        litCount = 0;
        moving = false;
        fadeIn = 1f;
        phase = Phase.Board;
        Play(clipUi, 0.5f);
        ShowHint("方向键走向（连按同向 = 这次多走几格，仍只花 1 行动）；回车结束本次移动。");
    }

    int TargetsTotal()
    {
        int n = 0;
        for (int r = 0; r < BN; r++)
            for (int c = 0; c < BN; c++) if (!blocked[r, c]) n++;
        return n;
    }

    void UpdateBoard()
    {
        fadeIn = Mathf.MoveTowards(fadeIn, 0f, Time.deltaTime / 0.3f);
        pawnVisual = Vector3.Lerp(pawnVisual, new Vector3(pawn.x, pawn.y, 0f), 1f - Mathf.Exp(-16f * Time.deltaTime));

        if (Input.GetKeyDown(KeyCode.Escape)) { phase = Phase.Corridor; Play(clipUi, 0.5f); return; }
        if (Input.GetKeyDown(KeyCode.R))
        {
            for (int r = 0; r < BN; r++)
                for (int c = 0; c < BN; c++) { lit[r, c] = false; flashAt[r, c] = -10f; }
            pawn = new Vector2Int(0, 0);
            movesLeft = MoveBudget;
            for (int i = 0; i < 4; i++) skillLeft[i] = SkillMaxAll[i];
            litCount = 0;
            moving = false;
            Play(clipUi, 0.5f);
            ShowHint("棋盘已重置。");
            return;
        }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) moving = false;

        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                moving = false;
                TryCast(i);
            }
        }

        int dir = -1;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) dir = 0;
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) dir = 1;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) dir = 2;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) dir = 3;
        if (dir >= 0) StepMove(dir);

        if (litCount >= TargetsTotal()) SolveBoard();
    }

    void SolveBoard()
    {
        boardSolved[boardIndex] = true;
        moving = false;
        Play(clipSolve, 0.7f);
        phase = Phase.Corridor;
        fadeIn = 0.9f;
        px = DeviceX[boardIndex] + 0.9f;
        player.position = new Vector3(px, player.position.y, player.position.z);
        if (SolvedCount() == 3)
        {
            doorOpen = true;
            Play(clipDoor, 0.8f);
            ShowHint("三座装置都亮了。门后面传来一声很轻的「咔」。");
        }
        else ShowHint("这座装置亮了（" + SolvedCount() + "/3）。");
    }

    int DirDx(int d) { return d == 0 ? 1 : d == 2 ? -1 : 0; }
    int DirDy(int d) { return d == 1 ? 1 : d == 3 ? -1 : 0; }

    void StepMove(int dir)
    {
        if (!moving || dir != moveDir)
        {
            int probe = 0, x = pawn.x, y = pawn.y;
            while (true)
            {
                x += DirDx(dir); y += DirDy(dir);
                if (x < 0 || y < 0 || x >= BN || y >= BN) break;
                if (blocked[y, x]) break;
                probe++;
            }
            if (probe == 0) { ShowHint("那个方向走不通。"); moving = false; return; }
            if (movesLeft <= 0) { ShowHint("行动次数用完了，按 R 重置棋盘。"); moving = false; return; }
            movesLeft--;
            moving = true;
            moveDir = dir;
            Play(clipMove, 0.5f);
        }

        int tx = pawn.x + DirDx(dir), ty = pawn.y + DirDy(dir);
        if (tx < 0 || ty < 0 || tx >= BN || ty >= BN || blocked[ty, tx])
        {
            ShowHint("走不动了。");
            moving = false;
            return;
        }
        pawn = new Vector2Int(tx, ty);
    }

    void TryCast(int idx)
    {
        if (skillLeft[idx] <= 0) { ShowHint("这个技能用完了。"); return; }
        skillLeft[idx]--;
        Play(clipCast, 0.7f);

        int before = litCount;
        foreach (var off in Offsets(idx))
        {
            int cx = pawn.x + off.x, cy = pawn.y + off.y;
            if (cx < 0 || cy < 0 || cx >= BN || cy >= BN) continue;
            if (blocked[cy, cx]) continue;
            if (!lit[cy, cx]) flashAt[cy, cx] = Time.time;
            lit[cy, cx] = true;
        }
        litCount = 0;
        for (int r = 0; r < BN; r++)
            for (int c = 0; c < BN; c++) if (lit[r, c]) litCount++;

        if (litCount > before) Play(clipLit, 0.6f);
        ShowHint(SkillName[idx] + " 施放于 (" + pawn.x + "," + pawn.y + ")，已点亮 " + litCount + "/" + TargetsTotal());
    }

    Vector2Int[] Offsets(int idx)
    {
        switch (idx)
        {
            case 0: return new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
            case 1: return new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            case 2: return new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
            default: return new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) };
        }
    }

    void UpdateChapter()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
        {
            phase = Phase.Corridor;
            finishing = false;
            fade = 0f;
            fadeIn = 0.6f;
            px = 7.0f;
            player.position = new Vector3(px, player.position.y, player.position.z);
        }
    }

    // =========================================================== 绘制
    void InitStyles()
    {
        if (sBody != null) return;
        if (cnFont == null)
            cnFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "SimSun" }, 18);
        sBody = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
        sTitle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
        sSmall = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        sCenter = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        sBig = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        if (cnFont != null) { sBody.font = cnFont; sTitle.font = cnFont; sSmall.font = cnFont; sCenter.font = cnFont; sBig.font = cnFont; }
    }

    void OnGUI()
    {
        InitStyles();
        float W = Screen.width, H = Screen.height;

        if (phase == Phase.Corridor)
        {
            GUI.Label(new Rect(24, 16, 760, 30), "第 25 小时 · 第一章 · 走廊", sTitle);
            string q = SolvedCount() == 3 ? "走进尽头的门"
                     : (taskGiven ? "点亮剩下的装置（" + SolvedCount() + "/3）" : "和走廊尽头的人说话");
            GUI.Label(new Rect(24, 52, 760, 26), q + "     碎片 " + fragCount + "/3", sBody);
            GUI.Label(new Rect(24, H - 34, 900, 24), "A/D 移动     E 交互 / 继续对话", sSmall);

            if (dialogOpen)
            {
                var box = new Rect(W * 0.5f - 340, H - 220, 680, 132);
                GUI.Box(box, GUIContent.none);
                GUI.Label(new Rect(box.x + 18, box.y + 14, box.width - 36, 92), lines[Mathf.Clamp(lineIdx, 0, lines.Count - 1)], sBody);
                GUI.Label(new Rect(box.x + 18, box.y + box.height - 30, box.width - 36, 24), "按 E 继续", sSmall);
            }

            if (finishing)
            {
                var col = GUI.color;
                GUI.color = new Color(0, 0, 0, fade);
                GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
                GUI.color = col;
            }
            else if (fadeIn > 0f)
            {
                var col = GUI.color;
                GUI.color = new Color(0, 0, 0, fadeIn);
                GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
                GUI.color = col;
            }
        }
        else if (phase == Phase.Board)
        {
            var col = GUI.color;
            GUI.color = new Color(0.07f, 0.08f, 0.11f, 1f);
            GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
            GUI.color = col;
            DrawBoard(W, H);
            if (fadeIn > 0f)
            {
                var c2 = GUI.color;
                GUI.color = new Color(0, 0, 0, fadeIn);
                GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
                GUI.color = c2;
            }
        }
        else DrawChapter(W, H);

        if (phase != Phase.Chapter && Time.time < hintUntil) GUI.Label(new Rect(24, H - 68, W - 48, 26), hint, sBody);
    }

    Vector2 BoardOrigin()
    {
        float total = CellSize * BN;
        return new Vector2((Screen.width - total) * 0.5f - 150f, 130f);
    }

    void DrawBoard(float W, float H)
    {
        var origin = BoardOrigin();
        float total = CellSize * BN;

        GUI.Label(new Rect(0, 14, W, 30), "装置 " + (boardIndex + 1) + " / 3 · 点亮所有格子", sBig);
        GUI.Label(new Rect(24, 52, 470, 26), "剩余行动次数：" + movesLeft + "    棋子 (" + pawn.x + "," + pawn.y + ")", sBody);
        GUI.Label(new Rect(24, 78, 470, 26), "已点亮：" + litCount + " / " + TargetsTotal(), sBody);

        var old = GUI.color;
        GUI.color = new Color(0.12f, 0.13f, 0.17f);
        GUI.DrawTexture(new Rect(origin.x - 8, origin.y - 8, total + 16, total + 16), Texture2D.whiteTexture);
        GUI.color = old;

        for (int r = 0; r < BN; r++)
        {
            for (int c = 0; c < BN; c++)
            {
                int screenRow = BN - 1 - r;
                var rect = new Rect(origin.x + c * CellSize + 2, origin.y + screenRow * CellSize + 2, CellSize - 4, CellSize - 4);
                Color fill = blocked[r, c] ? new Color(0.05f, 0.05f, 0.07f)
                           : lit[r, c] ? new Color(0.95f, 0.72f, 0.30f)
                           : new Color(0.28f, 0.29f, 0.35f);
                var tmp = GUI.color;
                GUI.color = fill;
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = tmp;

                if (blocked[r, c]) GUI.Label(rect, "X", sCenter);

                if (lit[r, c])
                {
                    float a = 1f - Mathf.Clamp01((Time.time - flashAt[r, c]) / 0.35f);
                    if (a > 0f)
                    {
                        var c3 = GUI.color;
                        GUI.color = new Color(1f, 1f, 1f, a * 0.85f);
                        GUI.DrawTexture(rect, Texture2D.whiteTexture);
                        GUI.color = c3;
                    }
                }
            }
        }

        // 棋子（平滑位移）
        {
            int screenRow = BN - 1 - pawn.y;
            var cur = new Rect(origin.x + pawnVisual.x * CellSize + 2, origin.y + (BN - 1 - pawnVisual.y) * CellSize + 2, CellSize - 4, CellSize - 4);
            var pr = new Rect(cur.x + 16, cur.y + 16, cur.width - 32, cur.height - 32);
            var c4 = GUI.color;
            GUI.color = new Color(0.90f, 0.24f, 0.20f);
            GUI.DrawTexture(pr, Texture2D.whiteTexture);
            GUI.color = c4;
        }

        float bx = origin.x + total + 40;
        GUI.Label(new Rect(bx, origin.y - 32, 340, 26), "技能（按数字在棋子所在格施放）", sBody);
        for (int i = 0; i < 4; i++)
        {
            var rect = new Rect(bx, origin.y + i * 62, 300, 52);
            var tmp = GUI.color;
            GUI.color = skillLeft[i] > 0 ? new Color(0.30f, 0.42f, 0.62f) : new Color(0.18f, 0.19f, 0.23f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = tmp;
            GUI.Label(new Rect(rect.x + 12, rect.y + 10, rect.width - 24, 30), (i + 1) + ".  " + SkillName[i] + "    x" + skillLeft[i], sBody);
        }
        GUI.Label(new Rect(bx, origin.y + 4 * 62 + 12, 340, 26), "R 重置      ESC 退出", sSmall);
        GUI.Label(new Rect(origin.x, origin.y + total + 14, total + 60, 26), "方向键走向（连按同向 = 多走几格，仍 1 行动）· 回车结束移动", sSmall);
    }

    void DrawChapter(float W, float H)
    {
        var col = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
        GUI.color = col;

        GUI.Label(new Rect(0, 70, W, 40), "第一章", sBig);

        float cell = 78f, gap = 12f, total = cell * 3 + gap * 2;
        float ox = (W - total) * 0.5f, oy = 150f;
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                bool litCell = (r == 2 && c == 0);
                var rect = new Rect(ox + c * (cell + gap), oy + r * (cell + gap), cell, cell);
                var tmp = GUI.color;
                GUI.color = litCell ? new Color(0.20f, 0.52f, 0.30f) : new Color(0.16f, 0.17f, 0.21f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = tmp;
                if (litCell) GUI.Label(rect, "O", sCenter);
            }
        }

        GUI.Label(new Rect(0, oy + total + 22, W, 30), "第一章 · 1 / 9", sCenter);
        GUI.Label(new Rect(0, oy + total + 60, W, 26), "（原型到此为止 · 按 E / ESC 回走廊）", sCenter);
    }
}