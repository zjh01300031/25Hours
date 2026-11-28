using UnityEngine;

/// <summary>
/// 收集进度：第一组捡完 → 进事件一；事件一完成、第二组捡完 → 进事件二。
/// 每层场景放一个就行。
/// </summary>
public class CollectionQuest : MonoBehaviour
{
    public static CollectionQuest Instance { get; private set; }

    [Header("第一组：凑齐就进事件一")]
    public string groupA = "A";
    public string eventA = "event_01";
    public string sceneA = "Event_01";

    [Header("第二组：事件一完成后才出现")]
    public string groupB = "B";
    public string eventB = "event_02";
    public string sceneB = "Event_02";

    [Header("自动进入（关掉后必须去「收集检查点」交互才会进）")]
    public bool autoEnter = false;

    bool entering;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>每次捡起东西后调用：看看这组是不是空了，空了就进事件。</summary>
    /// <summary>这一组凑够之后要进哪个事件（A → eventA，B → eventB）。</summary>
    public string EventOf(string group)
    {
        if (string.IsNullOrEmpty(group)) return eventA;
        return group.Trim().ToUpperInvariant() == groupB.Trim().ToUpperInvariant() ? eventB : eventA;
    }

    public void Refresh()
    {
        if (entering) return;

        var progress = GameProgress.Ensure();
        int remainA = 0;
        int remainB = 0;
        int totalA = 0;
        int totalB = 0;

        var all = FindObjectsOfType<Collectible>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var c = all[i];
            if (c == null) continue;
            if (c.group == groupA) totalA++;
            if (c.group == groupB) totalB++;
            if (!c.gameObject.activeSelf) continue;

            if (c.group == groupA) remainA++;
            else if (c.group == groupB) remainB++;
        }

        if (!progress.IsEventCleared(eventA))
        {
            if (autoEnter && totalA > 0 && remainA <= 0) Enter(eventA, sceneA);
            return;
        }

        if (autoEnter && !progress.IsEventCleared(eventB) && totalB > 0 && remainB <= 0)
        {
            Enter(eventB, sceneB);
        }
    }

    /// <summary>某组还剩几件在场（没捡的）。</summary>
    public int Remaining(string group)
    {
        int n = 0;
        var all = FindObjectsOfType<Collectible>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var c = all[i];
            if (c == null || c.group != group) continue;
            if (c.gameObject.activeSelf) n++;
        }
        return n;
    }

    /// <summary>某组一共有几件（摆了几个就是几个）。</summary>
    public int Total(string group)
    {
        int n = 0;
        var all = FindObjectsOfType<Collectible>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var c = all[i];
            if (c != null && c.group == group) n++;
        }
        return n;
    }

    /// <summary>某组还差哪些东西的名字，用来提醒玩家。</summary>
    public string MissingNames(string group)
    {
        var sb = new System.Text.StringBuilder();
        var all = FindObjectsOfType<Collectible>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var c = all[i];
            if (c == null || c.group != group) continue;
            if (!c.gameObject.activeSelf) continue;
            if (sb.Length > 0) sb.Append("、");
            sb.Append(string.IsNullOrEmpty(c.displayName) ? c.itemId : c.displayName);
        }
        return sb.ToString();
    }

    /// <summary>外部（收集检查点）调用：这一组收齐了就直接进对应事件。</summary>
    public bool TryEnter(string group)
    {
        var progress = GameProgress.Ensure();
        if (!progress.IsEventCleared(eventA))
        {
            if (Remaining(groupA) <= 0) { Enter(eventA, sceneA); return true; }
            return false;
        }

        if (!progress.IsEventCleared(eventB))
        {
            if (Remaining(groupB) <= 0) { Enter(eventB, sceneB); return true; }
            return false;
        }

        return false;
    }

    void Enter(string eventId, string sceneName)
    {
        entering = true;

        if (ShelterHUD.Instance != null)
        {
            ShelterHUD.Instance.Toast("东西收齐了…");
        }

        if (!ShelterFlow.BeginEvent(eventId, sceneName) && ShelterHUD.Instance != null)
        {
            ShelterHUD.Instance.Toast("场景 " + sceneName + " 不在 Build Settings 里");
            entering = false;
        }
    }
}
