using UnityEngine;

/// <summary>可以「翻找」的碎片：打开一段文案，然后自己消失。</summary>
public class FragmentPickup : InteractableBase
{
    public string fragmentId = "frag_01";
    public string[] lines;

    public override void Interact(ShelterPlayer player)
    {
        var progress = GameProgress.Ensure();
        progress.AddFragment(fragmentId);

        if (lines != null && lines.Length > 0 && ShelterDialogue.Instance != null)
        {
            ShelterDialogue.Instance.Show(displayName, lines);
        }

        if (ShelterHUD.Instance != null)
        {
            ShelterHUD.Instance.SetFragments(progress.FragmentCount, GameProgress.FragmentTotal);
            ShelterHUD.Instance.Toast("碎片 " + progress.FragmentCount + " / " + GameProgress.FragmentTotal);
        }

        gameObject.SetActive(false);
    }
}
