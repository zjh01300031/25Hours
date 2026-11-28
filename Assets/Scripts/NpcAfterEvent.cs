using UnityEngine;

/// <summary>事件没通关之前，这个物体不出现（白毛 NPC 用）。</summary>
public class NpcAfterEvent : MonoBehaviour
{
    [Tooltip("要先把哪个事件打完它才出现")]
    public string eventId = "event_02";

    [Tooltip("出现时在屏幕中间提示一句（留空不提示）")]
    public string appearHint = "";

    bool inited;
    bool shown;

    void Update()
    {
        bool ready = GameProgress.Ensure().IsEventCleared(eventId);

        if (!inited)
        {
            inited = true;
            shown = ready;
            gameObject.SetActive(ready);
            return;
        }

        if (ready == shown) return;

        shown = ready;
        gameObject.SetActive(ready);
        if (ready && !string.IsNullOrEmpty(appearHint)) CenterHint.Show(appearHint);
    }
}
