using UnityEngine;

/// <summary>只说话的物体：展柜、人偶、2区门之类。事件完成后可以换一套台词。</summary>
public class DialogueProp : InteractableBase
{
    [Tooltip("决定「事件后台词」看哪个事件的完成状态")]
    public string watchEventId = "event_01";

    public string[] lines;
    [Tooltip("事件完成之后的台词（留空就用原来的）")]
    public string[] linesAfterEvent;

    public override void Interact(ShelterPlayer player)
    {
        var progress = GameProgress.Instance;
        bool after = progress != null && progress.IsEventCleared(watchEventId)
                     && linesAfterEvent != null && linesAfterEvent.Length > 0;

        string[] use = after ? linesAfterEvent : lines;
        if (use != null && use.Length > 0 && ShelterDialogue.Instance != null)
        {
            ShelterDialogue.Instance.Show(displayName, use);
            return;
        }

        // 剧情文字之后再补：现在只给一个中性的占位反馈
        if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast("（这段文字之后再加）");
    }
}
