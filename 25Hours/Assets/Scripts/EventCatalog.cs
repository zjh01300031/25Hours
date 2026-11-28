using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>一条事件登记：id → 场景名。在 Inspector 里直接加。</summary>
[Serializable]
public class EventInfo
{
    [Tooltip("事件 id，代码里用它判断完成状态，例如 event_01")]
    public string id = "event_01";

    [Tooltip("只在调试里用的名字")]
    public string displayName = "回声室";

    [Tooltip("场景名（不带 .unity），必须在 Build Settings 里")]
    public string sceneName = "Event_Template";
}

/// <summary>
/// 事件登记表：把「事件 id」映射到「场景名」。
/// 放在第0层场景里（06_系统 下面的「事件登记表」），别人的事件场景拖进 Build Settings 后，
/// 在这里加一条就能被避难所里的入口物件调用。
/// </summary>
public class EventCatalog : MonoBehaviour
{
    public static EventCatalog Instance { get; private set; }

    public List<EventInfo> events = new List<EventInfo>();

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool TryGet(string eventId, out EventInfo info)
    {
        info = null;
        if (string.IsNullOrEmpty(eventId)) return false;

        for (int i = 0; i < events.Count; i++)
        {
            if (events[i] != null && events[i].id == eventId)
            {
                info = events[i];
                return true;
            }
        }

        return false;
    }
}
