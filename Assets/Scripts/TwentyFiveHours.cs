using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 《第25小时》原型 v2 —— 「完成一关 → 点亮棋盘一格 → 全亮进入下一章」
///
/// 与 v1（整盘解谜）的区别：棋盘不是谜题本身，而是**进度盘**。
/// 每一格 = 一个小关卡；把这一格对应的小关卡通过，它就亮起来；
/// 整个棋盘亮满 → 进入下一章（更大的棋盘）。
///
/// 小关卡用「点灯谜题」(lights-out)：
///   点一格，它自己和上下左右一起翻转；目标是把所有灯点亮。
///   关卡由「从全亮状态回点若干次」生成，所以**一定有解**。
///
/// 界面全 ASCII，因为 Unity 内置字体没有中文字形。
/// </summary>
public class TwentyFiveHours : MonoBehaviour
{
    enum Phase { Map, Puzzle, ChapterClear, AllClear }

    // ---------------- 章节配置 ----------------
    // 第 n 章：mapN x mapN 个关卡，每个关卡是 puzzleN x puzzleN 的点灯谜题
    static readonly int[] CHAPTER_MAP = { 3, 4, 5 };
    static readonly int[] CHAPTER_LIGHT = { 3, 4, 4 };

    Phase phase = Phase.Map;
    int chapter = 0;
    int mapN => CHAPTER_MAP[chapter];
    int lightN => CHAPTER_LIGHT[chapter];

    bool[,] cleared;          // 本章已点亮的格子
    int clearedCount;

    // ---------------- 当前小关卡 ----------------
    bool[,] grid;             // 当前谜题灯态，true = 亮
    int cellR, cellC;         // 当前关卡在棋盘上的位置
    int steps;
    float lockUntil;

    string message = "";
    int moveCount;

    GUIStyle title, mid, small, cell, big;

    void Awake()
    {
        StartChapter(0);
    }

    void StartChapter(int idx)
    {
        chapter = idx;
        cleared = new bool[mapN, mapN];
        clearedCount = 0;
        phase = Phase.Map;
        message = "Chapter " + (chapter + 1) + " - clear every cell.";
        moveCount = 0;
    }

    // ---------------- 小关卡 ----------------
    void StartPuzzle(int r, int c)
    {
        cellR = r; cellC = c;
        int n = lightN;
        int scramble = 2 + ((r * mapN + c) % 4);   // 2~5 次回点，越后越难一点

        grid = new bool[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                grid[i, j] = true;                 // 从全亮开始

        var rnd = new System.Random(20261104 + chapter * 100 + r * 10 + c);
        for (int k = 0; k < scramble; k++)
            Toggle(rnd.Next(n), rnd.Next(n));

        // 保证不是一开始就全亮
        if (IsSolved()) Toggle(rnd.Next(n), rnd.Next(n));

        steps = 0;
        lockUntil = Time.time + 0.3f;
        phase = Phase.Puzzle;
        message = "Light them all up.";
    }

    void Toggle(int r, int c)
    {
        int n = lightN;
        int[] dr = { 0, -1, 1, 0, 0 };
        int[] dc = { 0, 0, 0, -1, 1 };
        for (int k = 0; k < 5; k++)
        {
            int rr = r + dr[k], cc = c + dc[k];
            if (rr < 0 || rr >= n || cc < 0 || cc >= n) continue;
            grid[rr, cc] = !grid[rr, cc];
        }
    }

    bool IsSolved()
    {
        int n = lightN;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (!grid[i, j]) return false;
        return true;
    }

    void OnPuzzleClick(int r, int c)
    {
        if (Time.time < lockUntil) return;
        Toggle(r, c);
        steps++;
        if (IsSolved())
        {
            cleared[cellR, cellC] = true;
            clearedCount++;
            moveCount += steps;
            if (clearedCount >= mapN * mapN)
            {
                phase = (chapter + 1 < CHAPTER_MAP.Length) ? Phase.ChapterClear : Phase.AllClear;
                message = "Board complete!";
            }
            else
            {
                phase = Phase.Map;
                message = "Cell " + (cellR + 1) + "," + (cellC + 1) + " lit in " + steps + " steps.";
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && phase == Phase.Puzzle)
        {
            phase = Phase.Map;
            message = "Left the puzzle - cell still dark.";
        }
        if (Input.GetKeyDown(KeyCode.R) && phase == Phase.Map)
            StartChapter(chapter);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (phase == Phase.ChapterClear) StartChapter(chapter + 1);
            else if (phase == Phase.AllClear) StartChapter(0);
        }
    }

    // ---------------- 界面 ----------------
    void InitStyles()
    {
        if (title != null) return;
        title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
        mid = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        small = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        big = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        cell = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
    }

    void OnGUI()
    {
        InitStyles();
        GUI.Label(new Rect(30, 20, 900, 34), "25 HOURS", title);

        if (phase == Phase.Map) DrawMap();
        else if (phase == Phase.Puzzle) DrawPuzzle();
        else DrawClear();
    }

    void DrawMap()
    {
        int n = mapN;
        int size = n <= 3 ? 120 : (n == 4 ? 96 : 84);
        int pad = 8;
        int gx = 30, gy = 110;

        GUI.Label(new Rect(30, 58, 900, 24),
            "CHAPTER " + (chapter + 1) + "  /  " + CHAPTER_MAP.Length +
            "     cells lit: " + clearedCount + " / " + (n * n) +
            "     one puzzle per cell", mid);
        GUI.Label(new Rect(30, 82, 900, 20),
            "Clear a cell's puzzle to light it. Light the whole board to open the next chapter.", small);

        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                var rect = new Rect(gx + c * (size + pad), gy + r * (size + pad), size, size);
                var old = GUI.backgroundColor;
                GUI.backgroundColor = cleared[r, c]
                    ? new Color(0.30f, 0.72f, 0.44f)
                    : new Color(0.22f, 0.24f, 0.31f);
                string label = cleared[r, c] ? "O" : "";
                if (GUI.Button(rect, label, cell) && !cleared[r, c]) StartPuzzle(r, c);
                GUI.backgroundColor = old;
            }
        }

        int px = gx + n * (size + pad) + 24;
        GUI.Label(new Rect(px, gy, 360, 26), "BOARD", mid);
        GUI.Label(new Rect(px, gy + 30, 360, 160),
            "Each cell = one lights-out puzzle.\n" +
            "  O = lit (done)\n" +
            "  dark = not yet\n\n" +
            "All cells lit -> next chapter\n" +
            "(boards grow: 3x3 -> 4x4 -> 5x5)", small);
        GUI.Label(new Rect(px, gy + 200, 360, 24), message, small);
        GUI.Label(new Rect(30, 470, 900, 24), "R = reset this chapter", small);
    }

    void DrawPuzzle()
    {
        int n = lightN;
        int size = 84, pad = 8;
        int gx = 30, gy = 130;

        GUI.Label(new Rect(30, 58, 900, 24),
            "PUZZLE  -  cell " + (cellR + 1) + "," + (cellC + 1) +
            "     steps: " + steps, mid);
        GUI.Label(new Rect(30, 82, 900, 20),
            "Click a cell: it and its 4 neighbours flip. Make every light ON.", small);

        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++)
            {
                var rect = new Rect(gx + c * (size + pad), gy + r * (size + pad), size, size);
                var old = GUI.backgroundColor;
                GUI.backgroundColor = grid[r, c]
                    ? new Color(0.95f, 0.85f, 0.45f)
                    : new Color(0.20f, 0.22f, 0.28f);
                if (GUI.Button(rect, grid[r, c] ? "*" : "", cell)) OnPuzzleClick(r, c);
                GUI.backgroundColor = old;
            }
        }

        GUI.Label(new Rect(gx + n * (size + pad) + 30, gy, 380, 200),
            "ESC = back to board\n(the cell stays dark)", mid);
        GUI.Label(new Rect(gx + n * (size + pad) + 30, gy + 60, 380, 24), message, small);
    }

    void DrawClear()
    {
        bool all = (phase == Phase.AllClear);
        GUI.Label(new Rect(30, 150, 900, 40),
            all ? "ALL CHAPTERS CLEAR" : "CHAPTER " + (chapter + 1) + " CLEAR", big);
        GUI.Label(new Rect(30, 200, 900, 60),
            all ? "You lit the whole 25th hour.\nENTER = play again from chapter 1"
            : "The board is fully lit.\nENTER = open chapter " + (chapter + 2), mid);
        GUI.Label(new Rect(30, 300, 900, 40),
            "puzzles solved this chapter: " + clearedCount, small);
    }
}
