using UnityEngine;

/// <summary>
/// 能「按交互键进去」的门（和棋盘入口一样的交互方式）：
/// 走近会显示「进入 避难所二层」，按空格就黑屏切场景。
/// </summary>
public class RoomDoor : InteractableBase
{
    [Header("去哪")]
    public string targetScene = "Shelter_2F";

    [Header("到了之后站在哪个物体旁边")]
    public string spawnObjectName = "到达点_二层";

    [Header("转场时间（秒）")]
    public float fadeOut = 0.7f;
    public float blackHold = 2.0f;
    public float fadeIn = 1.0f;

    public override void Interact(ShelterPlayer player)
    {
        if (string.IsNullOrEmpty(targetScene)) return;

        RoomSpawn.PendingName = spawnObjectName;
        SceneFade.FadeTo(targetScene, fadeOut, blackHold, fadeIn);
    }
}
