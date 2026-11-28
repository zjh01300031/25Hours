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
    readonly bool[] mirror;

    public PuzzleBoard(int width, int height)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        lit = new bool[Width * Height];
        blocked = new bool[Width * Height];
        mirror = new bool[Width * Height];
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

    public bool IsMirror(int x, int y)
    {
        return InBounds(x, y) && mirror[Index(x, y)];
    }

    static void ParseCells(string[] cells, int w, int h, System.Action<int, int> mark)
    {
        if (cells == null) return;
        foreach (var raw in cells)
        {
            if (string.IsNullOrEmpty(raw)) continue;
            var parts = raw.Split(',');
            if (parts.Length != 2) continue;
            int x, y;
            if (!int.TryParse(parts[0].Trim(), out x)) continue;
            if (!int.TryParse(parts[1].Trim(), out y)) continue;
            if (x < 0 || x >= w || y < 0 || y >= h) continue;
            mark(x, y);
        }
    }

    /// <summary>镜像格：技能覆盖到它时，整块技能会以它为中心再出现一次（180° 点对称）。</summary>
    public void SetMirrorCells(string[] cells)
    {
        for (int i = 0; i < mirror.Length; i++) mirror[i] = false;
        ParseCells(cells, Width, Height, (x, y) => mirror[Index(x, y)] = true);
    }

    /// <summary>按设计稿规则展开一次施法：先取锚点周围的形状，再对覆盖到的镜像格做 180° 复制。</summary>
    public List<Vector2Int> Expand(IList<Vector2Int> offsets, Vector2Int anchor)
    {
        var baseCells = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();

        foreach (var offset in offsets)
        {
            int x = anchor.x + offset.x;
            int y = anchor.y + offset.y;
            if (!InBounds(x, y)) continue;
            if (blocked[Index(x, y)]) continue;
            var cell = new Vector2Int(x, y);
            if (seen.Add(cell)) baseCells.Add(cell);
        }

        var result = new List<Vector2Int>(baseCells);

        for (int i = 0; i < Width * Height; i++)
        {
            if (!mirror[i]) continue;
            var m = new Vector2Int(i % Width, i / Width);
            if (!baseCells.Contains(m)) continue;

            foreach (var cell in baseCells)
            {
                int rx = 2 * m.x - cell.x;
                int ry = 2 * m.y - cell.y;
                if (!InBounds(rx, ry)) continue;
                if (blocked[Index(rx, ry)]) continue;
                var rc = new Vector2Int(rx, ry);
                if (seen.Add(rc)) result.Add(rc);
            }
        }

        return result;
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
        return ApplyOffsets(PuzzleSkill.Offsets(shape), anchor, toggle);
    }

    /// <summary>按任意偏移表施放（控制器用它支持翻转镜像）。</summary>
    public int ApplyOffsets(IList<Vector2Int> offsets, Vector2Int anchor, bool toggle = true)
    {
        int changed = 0;
        foreach (var offset in offsets)
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
