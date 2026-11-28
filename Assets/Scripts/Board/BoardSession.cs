using UnityEngine;

/// <summary>
/// 跨场景传一点小数据：这次进棋盘，玩家投进去的是哪个人偶、长什么样。
/// 棋盘场景（PuzzleController）会把它当成棋盘上走动的那个小人。
/// </summary>
public static class BoardSession
{
    /// <summary>投进去的物品 id（背包里的 itemId）。</summary>
    public static string DollItemId = "";

    /// <summary>这个人偶的立绘（美术给了就填；空的话棋盘用默认小人）。</summary>
    public static Sprite DollSprite;

    /// <summary>显示名字。</summary>
    public static string DollName = "";

    public static void Set(string itemId, string displayName, Sprite sprite)
    {
        DollItemId = itemId;
        DollName = displayName;
        DollSprite = sprite;
    }

    public static void Clear()
    {
        DollItemId = "";
        DollName = "";
        DollSprite = null;
    }
}