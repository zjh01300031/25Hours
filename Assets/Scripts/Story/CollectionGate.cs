using UnityEngine;

/// <summary>
/// 第 0 层的「收集检查点」：玩家过来按 E / 空格，检查这一组东西是否收齐。
/// 收齐了 → 进事件一（或事件二）；没齐 → 告诉他谁还没捡到。
/// </summary>
public class CollectionGate : InteractableBase
{
    [Header("查哪一组")]
    public string group = "A";

    [Header("齐了以后去哪")]
    public string eventId = "event_01";
    public string sceneName = "Event_01";

    [Header("文案")]
    public string readyText = "东西齐了。";
    public string missingPrefix = "还差 ";

    public override string PromptText
    {
        get { return "检查 " + displayName; }
    }

    public override void Interact(ShelterPlayer player)
    {
        var quest = CollectionQuest.Instance;
        if (quest == null)
        {
            Say("这一层没有收集进度");
            return;
        }

        int remain = quest.Remaining(group);
        if (remain > 0)
        {
            Say(missingPrefix + remain + " 件：" + quest.MissingNames(group));
            return;
        }

        if (quest.Total(group) <= 0)
        {
            Say("这一组还没摆东西");
            return;
        }

        Say(readyText);
        if (!quest.TryEnter(group))
        {
            Say("入口打不开（检查 Build Settings 与事件 id）");
        }
    }

    void Say(string text)
    {
        if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast(text);
        Debug.Log("[收集检查点] " + text);
    }
}
