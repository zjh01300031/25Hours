using UnityEngine;

/// <summary>
/// 场景里能捡起来的东西：按交互键拿进背包，然后自己消失。
/// 分组（group）决定它属于「事件一之前」还是「事件一之后」。
/// </summary>
public class Collectible : InteractableBase
{
    [Tooltip("背包里的物品 id，例如 key_01（同一个 id 捡两次会累加数量）")]
    public string itemId = "item_01";

    [Tooltip("分组：A = 第一组（凑够就进事件一）；B = 第二组（事件一之后才出现）")]
    public string group = "A";

    [Tooltip("非空时：完成这个事件之后才出现在场景里（填 event_01）。留空 = 一开始就在")]
    public string requiresEventId = "";

    [Tooltip("第一次捡到这个物品时说的话，可以留空")]
    [TextArea(1, 4)]
    public string[] lines = new string[0];

    void Start()
    {
        // 已经捡过的（背包里有）就别再出现了
        if (GameProgress.Ensure().ItemCount(itemId) > 0)
        {
            gameObject.SetActive(false);
            return;
        }

        if (!string.IsNullOrEmpty(requiresEventId)
            && !GameProgress.Ensure().IsEventCleared(requiresEventId))
        {
            gameObject.SetActive(false);
        }
    }

    public override void Interact(ShelterPlayer player)
    {
        var progress = GameProgress.Ensure();
        int before = progress.ItemCount(itemId);
        progress.AddItem(itemId, 1, displayName);

        gameObject.SetActive(false);

        bool firstTime = before == 0;
        if (firstTime && lines != null && lines.Length > 0 && ShelterDialogue.Instance != null)
        {
            ShelterDialogue.Instance.Show(displayName, lines);
        }
        else if (ShelterHUD.Instance != null)
        {
            ShelterHUD.Instance.Toast("放进背包：" + displayName + "（" + progress.ItemCount(itemId) + "）");
        }

        if (CollectionQuest.Instance != null) CollectionQuest.Instance.Refresh();
    }
}
