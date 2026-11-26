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
            if (totalA > 0 && remainA <= 0) Enter(eventA, sceneA);
            return;
        }

        if (!progress.IsEventCleared(eventB) && totalB > 0 && remainB <= 0)
        {
            Enter(eventB, sceneB);
        }
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
