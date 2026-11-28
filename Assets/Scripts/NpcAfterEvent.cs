using UnityEngine;

/// <summary>
/// 事件没通关之前，这个 NPC 不出现（白毛 NPC 用）。
/// 注意：只关掉「看得见、碰得到」的部分，不关整个物体本身 ——
/// 否则脚本自己也停了，通关后没人把它打开。
/// </summary>
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
            Apply(ready);
            return;
        }

        if (ready == shown) return;

        shown = ready;
        Apply(ready);

        if (ready && !string.IsNullOrEmpty(appearHint)) CenterHint.Show(appearHint);
    }

    void Apply(bool visible)
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = visible;

        foreach (var col in GetComponentsInChildren<Collider2D>(true))
            col.enabled = visible;

        foreach (var choice in GetComponentsInChildren<ChoiceNpc>(true))
            choice.enabled = visible;

        foreach (var text in GetComponentsInChildren<SimpleTextTrigger>(true))
            text.enabled = visible;
    }
}
