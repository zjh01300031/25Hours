using System;
using UnityEngine;

/// <summary>技能的覆盖形状。</summary>
public enum PuzzleShape
{
    Single,   // 单格
    Cross,    // 十字（中心 + 上下左右）
    Square3,  // 3×3
    Row3,     // 横向 3 格
    Col3,     // 纵向 3 格
    Col2,     // 纵向 2 格
    Square2,  // 2×2
    L3,       // L 形 3 格（(0,0)(1,0)(0,1)）
}

/// <summary>
/// 一个技能的配置：名字、形状、剩余次数。
/// 由 PuzzleController 在 Inspector 里配，运行时会消耗 charges。
/// </summary>
[Serializable]
public class PuzzleSkill
{
    public string name = "微光";
    public PuzzleShape shape = PuzzleShape.Single;
    public int charges = 5;

    public PuzzleSkill() { }

    public PuzzleSkill(string name, PuzzleShape shape, int charges)
    {
        this.name = name;
        this.shape = shape;
        this.charges = charges;
    }

    /// <summary>形状相对锚点的偏移（0,0 是锚点自己）。</summary>
    public static Vector2Int[] Offsets(PuzzleShape shape)
    {
        switch (shape)
        {
            case PuzzleShape.Cross:
                return new[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0), new Vector2Int(-1, 0),
                    new Vector2Int(0, 1), new Vector2Int(0, -1),
                };
            case PuzzleShape.Square3:
                return new[]
                {
                    new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
                    new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0),
                    new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
                };
            case PuzzleShape.Row3:
                return new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) };
            case PuzzleShape.Col3:
                return new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1) };
            case PuzzleShape.Col2:
                return new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) };
            case PuzzleShape.Square2:
                return new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1),
                };
            case PuzzleShape.L3:
                return new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
            default:
                return new[] { new Vector2Int(0, 0) };
        }
    }
}
