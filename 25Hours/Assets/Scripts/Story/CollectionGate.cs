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

        var progress = GameProgress.Ensure();

        // 先看第一组：还没打事件一，就看第一组
        if (!progress.IsEventCleared(quest.EventOf("A")))
        {
            int totalA = quest.Total("A");
            int remainA = quest.Remaining("A");

            if (totalA <= 0)
            {
                Say("第一组的东西还没摆进场景");
                return;
            }

            if (remainA > 0)
            {
                Say(missingPrefix + remainA + " 件：" + quest.MissingNames("A"));
                return;
            }

            Say(readyText);
            if (!quest.TryEnter("A")) Say("进不去（检查 Build Settings 里的 Event_01）");
            return;
        }

        // 第一组已经打完了：看第二组
        if (!progress.IsEventCleared(quest.EventOf("B")))
        {
            int totalB = quest.Total("B");
            int remainB = quest.Remaining("B");

            if (totalB <= 0)
            {
                Say("第二组的东西还没摆进场景");
                return;
            }

            if (remainB > 0)
            {
                Say(missingPrefix + remainB + " 件：" + quest.MissingNames("B"));
                return;
            }

            Say(readyText);
            if (!quest.TryEnter("B")) Say("进不去（检查 Build Settings 里的 Event_02）");
            return;
        }

        Say("这里已经没有要收的东西了。");
    }

    void Say(string text)
    {
        if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast(text);
        Debug.Log("[收集检查点] " + text);
    }
}
