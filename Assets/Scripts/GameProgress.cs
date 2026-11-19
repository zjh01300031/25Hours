using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 跨场景的进度：碎片、事件完成记录、回程坐标。
/// 场景里放一个 GameProgress 物体，切场景后会自动保留。
///
/// 【别人的代码怎么用】
///   GameProgress.Ensure().AddFragment("frag_07");
///   GameProgress.Ensure().MarkEventCleared("event_01");
///   GameProgress.Ensure().IsEventCleared("event_01");
///   GameProgress.EventCleared += 收到通知的方法;   // 事件完成时回调
/// 不用去场景里找物体，静态方法会自己补一个。
/// </summary>
public class GameProgress : MonoBehaviour
{
    public static GameProgress Instance { get; private set; }

    /// <summary>第0层一共藏了多少片文案碎片。</summary>
    public const int FragmentTotal = 6;

    /// <summary>回程坐标（由 ShelterFlow 维护，这里保留是为了兼容旧的读取方）。</summary>
    public bool hasReturnPos;
    public Vector3 shelterReturnPos;

    readonly HashSet<string> fragments = new HashSet<string>();
    readonly HashSet<string> clearedEvents = new HashSet<string>();

    public int FragmentCount { get { return fragments.Count; } }

    /// <summary>收到一片碎片时触发：(碎片 id, 现在有几片, 总共有几片)</summary>
    public static event Action<string, int, int> FragmentChanged;

    /// <summary>某个事件第一次被标记完成时触发：(事件 id)</summary>
    public static event Action<string> EventCleared;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool HasFragment(string id)
    {
        return !string.IsNullOrEmpty(id) && fragments.Contains(id);
    }

    public bool AddFragment(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (!fragments.Add(id)) return false;

        var handler = FragmentChanged;
        if (handler != null) handler(id, fragments.Count, FragmentTotal);
        return true;
    }

    public bool IsEventCleared(string eventId)
    {
        return !string.IsNullOrEmpty(eventId) && clearedEvents.Contains(eventId);
    }

    /// <summary>标记事件完成。第一次标记时返回 true 并广播 EventCleared。</summary>
    public bool MarkEventCleared(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return false;
        if (!clearedEvents.Add(eventId)) return false;

        var handler = EventCleared;
        if (handler != null) handler(eventId);
        return true;
    }

    public void RememberShelterPos(Vector3 pos)
    {
        shelterReturnPos = pos;
        hasReturnPos = true;
    }

    /// <summary>没有进度物体时补一个（比如直接从事件场景开始运行）。</summary>
    public static GameProgress Ensure()
    {
        if (Instance == null)
        {
            Instance = new GameObject("GameProgress").AddComponent<GameProgress>();
        }

        return Instance;
    }
}
