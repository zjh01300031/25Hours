using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋盘的「关卡数据」：大小、哑格、技能配置、初始亮格。
/// 策划把设计结果写成这个 JSON，丢进工程，PuzzleController 直接读，不用改代码。
///
/// 一个 5×5 的例子（Assets/Resources/Board/sample_layout.json）：
///
/// {
///   "width": 5,
///   "height": 5,
///   "blockedCells": ["2,2"],
///   "toggleCells": false,
///   "clearThreshold": 1.0,
///   "skills": [
///     { "name": "微光", "shape": "Single",  "charges": 6 },
///     { "name": "回响", "shape": "Cross",   "charges": 4 },
///     { "name": "共振", "shape": "Square3", "charges": 2 }
///   ],
///   "litCells": []
/// }
///
/// 字段都可以省：省了就用控制器 Inspector 上的值。
/// </summary>
[Serializable]
public class PuzzleLayout
{
    public int width;
    public int height;
    public string[] blockedCells;
    public bool toggleCells;
    public float clearThreshold = 1f;
    public List<PuzzleSkillData> skills = new List<PuzzleSkillData>();
    public string[] litCells;
}

[Serializable]
public class PuzzleSkillData
{
    public string name = "微光";
    public string shape = "Single";
    public int charges = 1;
}

public static class PuzzleLayoutLoader
{
    /// <summary>从 TextAsset 读 JSON；失败返回 null 并打日志。</summary>
    public static PuzzleLayout Load(TextAsset asset)
    {
        if (asset == null || string.IsNullOrEmpty(asset.text)) return null;

        try
        {
            var layout = JsonUtility.FromJson<PuzzleLayout>(asset.text);
            if (layout == null) Debug.LogWarning("[棋盘] JSON 解析结果是空的：" + asset.name);
            return layout;
        }
        catch (Exception e)
        {
            Debug.LogError("[棋盘] JSON 解析失败（" + asset.name + "）：" + e.Message);
            return null;
        }
    }

    /// <summary>"Cross" / "square3" / "single" 这类字符串 → 枚举。认不出就当单格。</summary>
    public static PuzzleShape ParseShape(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return PuzzleShape.Single;
        var key = raw.Trim().ToLowerInvariant();

        switch (key)
        {
            case "cross":
            case "十字":
                return PuzzleShape.Cross;
            case "square3":
            case "3x3":
            case "九宫":
                return PuzzleShape.Square3;
            case "row3":
            case "横3":
                return PuzzleShape.Row3;
            case "square2":
            case "2x2":
            case "四方":
                return PuzzleShape.Square2;
            case "col2":
            case "竖2":
                return PuzzleShape.Col2;
            case "l3":
            case "l":
            case "l形":
                return PuzzleShape.L3;
            case "col3":
            case "竖3":
                return PuzzleShape.Col3;
            default:
                return PuzzleShape.Single;
        }
    }
}
