using UnityEngine;

/// <summary>
/// 可拾取物件：交互后记进 GameProgress，并播一段文案（storyId 留空就只弹提示）。
/// 拾取过的东西不会再出现（场景重进也不会）。
/// </summary>
public class PickupItem : InteractableBase
{
    [Tooltip("物品 id，用来存进度")]
    public string itemId = "item_01";

    [Tooltip("拾取时播的文案 id（留空就只给 toast）")]
    public string storyId;

    [Tooltip("拾取数量")]
    public int amount = 1;

    public override string PromptText
    {
        get { return "拾取 " + displayName; }
    }

    void Start()
    {
        // 上一轮已经捡过就别再出现
        if (GameProgress.Ensure().ItemCount(itemId) > 0)
        {
            gameObject.SetActive(false);
            return;
        }

        var glow = transform.Find("光");
        if (glow != null) glow.gameObject.SetActive(true);
    }

    public override void Interact(ShelterPlayer player)
    {
        if (ShelterDialogue.IsOpen) return;

        int total = GameProgress.Ensure().AddItem(itemId, amount);
        if (total == 0) return;

        if (Story.Has(storyId) && ShelterDialogue.Instance != null)
        {
            ShelterDialogue.Instance.Show(Story.Speaker(storyId), Story.Lines(storyId));
        }
        else if (ShelterHUD.Instance != null)
        {
            ShelterHUD.Instance.Toast("拾取 " + displayName + "（" + total + "）");
        }

        gameObject.SetActive(false);
    }
}
