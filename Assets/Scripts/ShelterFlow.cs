using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 避难所 ↔ 事件场景的进出流程。
///
/// 【别人的事件代码只需要这两个调用】
///   ShelterFlow.ReturnToShelter();          // 事件结束，回第0层（自动放回离开的位置）
///   ShelterFlow.BeginEvent(id, sceneName);  // 想从别处主动进事件时用
/// 一般不用自己调 LoadScene。
/// </summary>
public static class ShelterFlow
{
    /// <summary>当前正在跑的事件 id（不在事件里时为 null）。</summary>
    public static string CurrentEventId { get; private set; }

    /// <summary>回第0层时要把玩家放回的位置。</summary>
    public static Vector3 ReturnPoint { get; private set; }

    public static bool HasReturnPoint { get; private set; }

    /// <summary>最近一次在避难所里的位置（给"再进一次事件"用）。</summary>
    public static Vector3 LastShelterPosition { get; private set; }

    public static event Action<string> EventStarted;
    public static event Action<string> EventFinished;

    /// <summary>离开避难所前记下位置。</summary>
    public static void RememberShelterPosition(Vector3 position)
    {
        LastShelterPosition = position;
        ReturnPoint = position;
        HasReturnPoint = true;
    }

    /// <summary>加载事件场景。场景必须在 Build Settings 里，否则返回 false。</summary>
    public static bool BeginEvent(string eventId, string sceneName)
    {
        if (string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[ShelterFlow] 事件 id 或场景名为空");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning("[ShelterFlow] 场景不在 Build Settings 里：" + sceneName +
                             "（菜单 File > Build Settings 里把场景拖进去）");
            return false;
        }

        CurrentEventId = eventId;
        var handler = EventStarted;
        if (handler != null) handler(eventId);

        SceneManager.LoadScene(sceneName);
        return true;
    }

    /// <summary>回第0层。</summary>
    public static void ReturnToShelter()
    {
        string id = CurrentEventId;
        CurrentEventId = null;

        var handler = EventFinished;
        if (handler != null) handler(id);

        SceneManager.LoadScene(SceneNames.ShelterL0);
    }

    /// <summary>玩家读走回程坐标后调用，避免每次重生都回同一个点。</summary>
    public static void ConsumeReturnPoint()
    {
        HasReturnPoint = false;
    }
}
