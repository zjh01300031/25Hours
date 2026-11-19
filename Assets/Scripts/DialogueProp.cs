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
        if (ShelterDialogue.Instance != null) ShelterDialogue.Instance.Show(displayName, after ? linesAfterEvent : lines);
    }
}
