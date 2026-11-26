using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 一层（Level_01 / Level_02 …）的收尾逻辑：
/// 这一层里的事件全部完成后，自动回到第0层。
///
/// 用法：在这一层的场景里建一个空物体 → Add Component → Level Flow。
/// Required Events 留空时，会自动收集本场景里所有 EventRoomBase 的事件 id。
/// </summary>
public class LevelFlow : MonoBehaviour
{
    [Tooltip("全部完成后回到哪个场景（必须是 Build Settings 里的场景名）")]
    public string returnScene = "Shelter_L0";

    [Tooltip("最后一个事件完成后等几秒再切场景；0 = 立刻")]
    public float delay = 1.4f;

    [Tooltip("这一层包含哪些事件 id。留空 = 自动收集本场景里所有 EventRoomBase")]
    public string[] requiredEvents = new string[0];

    [Tooltip("这一层的 id，给第0层门的 Requires Level 用。留空 = 自动用当前场景名（level01）")]
    public string levelId;

    [Header("测试时看得见的标记")]
    [Tooltip("收尾点的标记颜色。它其实是整场景生效的控制器，这里只是方便你在测试时确认它在。")]
    public Color markerColor = new Color(0.55f, 0.80f, 0.45f, 0.9f);

    public static LevelFlow Instance { get; private set; }

    readonly List<string> watching = new List<string>();
    bool returning;

    void Awake()
    {
        Instance = this;
        Marker.EnsureBox(transform, markerColor, 1.0f);
    }

    void OnDrawGizmos()
    {
        Marker.Wire(transform, markerColor, 1.0f);
    }

    void OnDestroy()
    {
        GameProgress.EventCleared -= OnEventCleared;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Start()
    {
        watching.Clear();

        if (requiredEvents != null)
        {
            for (int i = 0; i < requiredEvents.Length; i++)
            {
                if (!string.IsNullOrEmpty(requiredEvents[i]))
                {
                    watching.Add(requiredEvents[i]);
                }
            }
        }

        if (watching.Count == 0)
        {
            var rooms = FindObjectsOfType<EventRoomBase>();
            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i] != null && !string.IsNullOrEmpty(rooms[i].EventId))
                {
                    watching.Add(rooms[i].EventId);
                }
            }
        }

        if (watching.Count == 0)
        {
            Debug.LogWarning("[LevelFlow] 这一层没有登记任何事件，不会自动回第0层。", this);
            return;
        }

        GameProgress.EventCleared += OnEventCleared;

        if (string.IsNullOrEmpty(levelId))
        {
            levelId = SceneManager.GetActiveScene().name;
        }

        // 启动这一下只统计、不返回：
        // 重进一个已经做完的层，不该一进门就被踢回第0层。
        Refresh(false);
    }

    void OnEventCleared(string eventId)
    {
        Refresh(true);
    }

    /// <summary>
    /// 数一数还有几个事件没完成：全完成就回第0层，否则提示还剩几个。
    /// allowReturn = false 时只统计不返回（场景刚加载时用）。
    /// </summary>
    public void Refresh(bool allowReturn = true)
    {
        if (returning)
        {
            return;
        }

        var progress = GameProgress.Ensure();
        int left = 0;
        for (int i = 0; i < watching.Count; i++)
        {
            if (!progress.IsEventCleared(watching[i]))
            {
                left++;
            }
        }

        if (left == 0)
        {
            if (!allowReturn)
            {
                // 这一层在之前已经做完了，但玩家主动又进来逛
                Debug.Log("[LevelFlow] 这一层的事件都已完成，等你手动离开。", this);
                return;
            }

            StartCoroutine(ReturnRoutine());
            return;
        }

        if (ShelterHUD.Instance != null)
        {
            ShelterHUD.Instance.Toast("事件完成 · 还剩 " + left + " 个");
        }
    }

    IEnumerator ReturnRoutine()
    {
        returning = true;

        if (ShelterHUD.Instance != null)
        {
            ShelterHUD.Instance.Toast("这一层结束 · 回到第 0 层");
        }

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (string.IsNullOrEmpty(returnScene) || !Application.CanStreamedLevelBeLoaded(returnScene))
        {
            Debug.LogWarning("[LevelFlow] 回不去：" + returnScene + " 不在 Build Settings 里。", this);
            returning = false;
            yield break;
        }

        // 记下「这一层打完了」，第0层的门就能用 Requires Level = levelId 来解锁
        GameProgress.Ensure().MarkEventCleared(levelId);

        SceneFade.FadeTo(returnScene, 0.5f, 0.35f, 0.9f);
    }
}
