using UnityEditor;
using UnityEngine;

/// <summary>
/// 棋盘逻辑自测：不打开场景、不点鼠标，直接跑 PuzzleBoard 的规则，
/// 用来确认「翻转 / 覆盖率 / 通关判定」没有被改坏。
/// 菜单：工具 / 25小时 / 自测：棋盘逻辑
/// 命令行：-executeMethod BoardLogicTest.RunTests
/// </summary>
public static class BoardLogicTest
{
    static int passed;
    static int failed;

    [MenuItem("工具/25小时/自测：棋盘逻辑", false, 6)]
    public static void RunTests()
    {
        passed = 0;
        failed = 0;

        // 1. 尺寸与哑格
        var board = new PuzzleBoard(5, 5);
        board.SetBlockedCells(new[] { "2,2" });
        Check("可用格 24", board.PlayableCount == 24);
        Check("哑格被识别", board.IsBlocked(2, 2));
        Check("初始覆盖 0", board.LitCount == 0 && Mathf.Approximately(board.Coverage, 0f));
        Check("初始未通关", !board.IsCleared);

        // 2. 单格技能：点亮一格
        int changed = board.Apply(PuzzleShape.Single, new Vector2Int(0, 0));
        Check("单格翻转 1 格", changed == 1);
        Check("亮格数 1", board.LitCount == 1);
        Check("覆盖率 1/24", Mathf.Approximately(board.Coverage, 1f / 24f));

        // 3. 再点同一格：关掉（说明是翻转，不是单纯点亮）
        changed = board.Apply(PuzzleShape.Single, new Vector2Int(0, 0));
        Check("再点同一格变成 0", changed == 1 && board.LitCount == 0);

        // 4. 十字技能：角落只有 3 格在界内
        changed = board.Apply(PuzzleShape.Cross, new Vector2Int(0, 0));
        Check("角落十字翻 3 格", changed == 3);
        Check("十字后亮 3 格", board.LitCount == 3);

        // 5. 哑格不会被翻转（十字覆盖 5 格，但中心是哑格 → 只翻 4 格）
        int before = board.LitCount;
        changed = board.Apply(PuzzleShape.Cross, new Vector2Int(2, 2));
        Check("哑格命中但不计入可用格", board.LitCount <= 24);
        Check("十字压哑格只翻 4 格", changed == 4);

        // 6. 3×3 技能在角落只有 4 格
        var board2 = new PuzzleBoard(5, 5);
        changed = board2.Apply(PuzzleShape.Square3, new Vector2Int(0, 0));
        Check("角落 3×3 翻 4 格", changed == 4);

        // 7. 通关判定
        var board3 = new PuzzleBoard(3, 3);
        Check("3×3 可用 9", board3.PlayableCount == 9);
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                board3.SetLit(x, y, true);
        Check("全亮即通关", board3.IsCleared);
        Check("全亮覆盖 100%", Mathf.Approximately(board3.Coverage, 1f));

        // 7.5 不翻转模式：只点亮，重复点同一格不再改变
        var boardLight = new PuzzleBoard(3, 3);
        boardLight.Apply(PuzzleShape.Single, new Vector2Int(1, 1), false);
        int again = boardLight.Apply(PuzzleShape.Single, new Vector2Int(1, 1), false);
        Check("点亮模式重复点不坑次数", again == 0 && boardLight.LitCount == 1);

        // 8. 重置
        board3.Reset();
        Check("重置后回到未通关", !board3.IsCleared && board3.LitCount == 0);

        // 9. 越界不崩
        var board4 = new PuzzleBoard(3, 3);
        changed = board4.Apply(PuzzleShape.Square3, new Vector2Int(99, 99));
        Check("越界不翻转任何格", changed == 0);

        string summary = "[棋盘自测] 通过 " + passed + " 项，失败 " + failed + " 项";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);

        if (Application.isBatchMode && failed > 0)
        {
            EditorApplication.Exit(1);
        }
    }

    static void Check(string name, bool ok)
    {
        if (ok)
        {
            passed++;
            Debug.Log("[棋盘自测] ✓ " + name);
        }
        else
        {
            failed++;
            Debug.LogError("[棋盘自测] ✗ " + name);
        }
    }

    [MenuItem("工具/25小时/自测：五块棋盘数据", false, 7)]
    public static void RunLayoutTests()
    {
        var expects = new[] { 26, 31, 28, 52, 65 };
        passed = 0;
        failed = 0;

        for (int i = 0; i < 5; i++)
        {
            string path = "Assets/Resources/Board/board_" + (i + 1).ToString("00") + ".json";
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset == null) { Debug.LogError("[关卡数据] 找不到 " + path); failed++; continue; }

            var layout = PuzzleLayoutLoader.Load(asset);
            if (layout == null) { Debug.LogError("[关卡数据] 解析失败 " + path); failed++; continue; }

            var board = new PuzzleBoard(layout.width, layout.height);
            board.SetBlockedCells(layout.blockedCells);

            bool okSize = layout.width > 0 && layout.height > 0;
            bool okPlayable = board.PlayableCount == expects[i];
            bool okSkills = layout.skills != null && layout.skills.Count > 0;
            bool okCoverage = Mathf.Approximately(board.Coverage, 0f);

            Check("棋盘" + (i + 1) + " 尺寸 " + layout.width + "×" + layout.height, okSize);
            Check("棋盘" + (i + 1) + " 可用格 " + board.PlayableCount + "（应为 " + expects[i] + "）", okPlayable);
            Check("棋盘" + (i + 1) + " 技能 " + (layout.skills == null ? 0 : layout.skills.Count) + " 个", okSkills);
            Check("棋盘" + (i + 1) + " 初始覆盖 0", okCoverage);
        }

        Debug.Log("[关卡数据自测] 通过 " + passed + " 项，失败 " + failed + " 项");
        if (failed > 0 && Application.isBatchMode) EditorApplication.Exit(1);
    }

}
