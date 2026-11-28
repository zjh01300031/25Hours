using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 内层棋盘的纯逻辑（不碰 UI、不碰场景）。
/// 规则：格子有「亮 / 暗」两种状态；用技能时，技能形状覆盖到的格子会**翻转**。
/// 覆盖率 = 亮格 / 可用格；可用格全部亮起来就算通关。
/// 棋盘是 x 向右、y 向上，左下角是 (0,0)。
/// </summary>
public class PuzzleBoard
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    readonly bool[] lit;
    readonly bool[] blocked;

    public PuzzleBoard(int width, int height)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        lit = new bool[Width * Height];
        blocked = new bool[Width * Height];
    }

    int Index(int x, int y) { return y * Width + x; }

    public bool InBounds(int x, int y) { return x >= 0 && x < Width && y >= 0 && y < Height; }

    public bool IsBlocked(int x, int y)
    {
        return !InBounds(x, y) || blocked[Index(x, y)];
    }

    public bool IsLit(int x, int y)
    {
        return InBounds(x, y) && lit[Index(x, y)];
    }

    /// <summary>用字符串列表标出哑格，格式 "x,y"（可以写多组）。</summary>
    public void SetBlockedCells(IEnumerable<string> cells)
    {
        for (int i = 0; i < blocked.Length; i++) blocked[i] = false;
        if (cells == null) return;

        foreach (var raw in cells)
        {
            if (string.IsNullOrEmpty(raw)) continue;
            var parts = raw.Split(',');
            if (parts.Length != 2) continue;
            int x, y;
            if (!int.TryParse(parts[0].Trim(), out x)) continue;
            if (!int.TryParse(parts[1].Trim(), out y)) continue;
            if (InBounds(x, y)) blocked[Index(x, y)] = true;
        }
    }

    public int PlayableCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < blocked.Length; i++) if (!blocked[i]) n++;
            return n;
        }
    }

    public int LitCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < lit.Length; i++) if (lit[i] && !blocked[i]) n++;
            return n;
        }
    }

    /// <summary>0 ~ 1 的覆盖率。</summary>
    public float Coverage
    {
        get
        {
            int playable = PlayableCount;
            return playable <= 0 ? 0f : (float)LitCount / playable;
        }
    }

    public bool IsCleared
    {
        get
        {
            int playable = PlayableCount;
            return playable > 0 && LitCount >= playable;
        }
    }

    /// <summary>
    /// 以 anchor 为锚点、用某个形状覆盖格子。
    /// toggle = true：亮↔暗 翻转（做唯一解棋盘的关要用）；
    /// toggle = false：只把格子点亮（白盒默认，保证一定打得通）。
    /// 返回真正被改变的格子数（0 表示这次点击什么都没改变，调用方据此不扣次数）。
    /// </summary>
    public int Apply(PuzzleShape shape, Vector2Int anchor, bool toggle = true)
    {
        int changed = 0;
        foreach (var offset in PuzzleSkill.Offsets(shape))
        {
            int x = anchor.x + offset.x;
            int y = anchor.y + offset.y;
            if (!InBounds(x, y)) continue;
            int i = Index(x, y);
            if (blocked[i]) continue;

            if (toggle)
            {
                lit[i] = !lit[i];
                changed++;
            }
            else if (!lit[i])
            {
                lit[i] = true;
                changed++;
            }
        }
        return changed;
    }

    /// <summary>直接把某一格设成亮/暗（给「重置」「调试」用）。</summary>
    public void SetLit(int x, int y, bool value)
    {
        if (!InBounds(x, y)) return;
        lit[Index(x, y)] = value;
    }

    public void Reset()
    {
        for (int i = 0; i < lit.Length; i++) lit[i] = false;
    }
}
