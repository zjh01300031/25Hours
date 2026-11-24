using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 空事件房间（模板）：不带任何玩法，只演示「事件场景怎么和避难所对接」。
/// 别人的正式事件可以把整个场景内容换掉，只保留 EventRoomBase 的约定。
/// </summary>
public class EventRoomTemplate : EventRoomBase
{
    public Text statusText;

    // 刚切场景那一帧，玩家按门的 E 还「按着」，会被这里接住 -> 立刻 Complete。
    // 所以进场景后先闭麦一会儿。
    float inputReadyTime;

    protected override void Start()
    {
        base.Start();
        inputReadyTime = Time.time + 0.35f;
        Refresh();
    }

    public override void OnEnter()
    {
        Refresh();
    }

    void Update()
    {
        if (Time.time < inputReadyTime) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Complete();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Leave();
        }
    }

    void Refresh()
    {
        if (statusText == null) return;

        bool cleared = GameProgress.Ensure().IsEventCleared(EventId);
        statusText.text = cleared
            ? "这个事件已经完成过了 · 按 E 再回一次避难所"
            : "事件进行中（这里是空模板，没有玩法）";
    }
}
