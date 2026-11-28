using UnityEngine;

/// <summary>
/// 通用交互物：按 id 从文案仓库取台词播出来。
/// 场景里随便一个方框挂上它、填 storyId，就能用来测「物品交互 + 文案」。
/// </summary>
public class StoryInteractable : InteractableBase
{
    [Tooltip("文案 id（Assets/Resources/Story/story.csv 里的 id）")]
    public string storyId;

    [Tooltip("没有文案时显示的功能反馈")]
    public string missingToast = "（这段还没写文案）";

    public override void Interact(ShelterPlayer player)
    {
        if (ShelterDialogue.IsOpen) return;

        if (Story.Has(storyId) && ShelterDialogue.Instance != null)
        {
            ShelterDialogue.Instance.Show(Story.Speaker(storyId), Story.Lines(storyId));
            return;
        }

        if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast(missingToast);
    }
}
