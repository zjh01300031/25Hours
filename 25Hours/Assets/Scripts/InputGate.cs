using UnityEngine;

/// <summary>
/// 全局输入闸门：为 true 时所有角色操作、背包、选项界面都不响应键盘。
/// 演出（开场文案）、转场、过场动画期间置 true，播完置 false。
/// </summary>
public static class InputGate
{
    public static bool Blocked;

    public static void Release()
    {
        Blocked = false;
    }
}
