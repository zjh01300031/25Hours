using UnityEngine;

/// <summary>
/// 玩家身边的「检测圈」：找到最近的可交互物，显示提示，按 E 互动。
/// </summary>
public class ShelterInteractor : MonoBehaviour
{
    public float radius = 1.9f;
    public LayerMask interactableMask;
    public KeyCode interactKey = KeyCode.Space;

    readonly Collider2D[] hits = new Collider2D[16];
    InteractableBase current;
    string lastPrompt;

    void Update()
    {
        if (ShelterHUD.Instance == null) return;

        var found = FindNearest();

        if (found != current)
        {
            if (current != null) current.SetHighlight(false);
            current = found;
            if (current != null) current.SetHighlight(true);
        }

        bool dialogueOpen = ShelterDialogue.IsOpen || ChoiceUI.IsOpen || InventoryUI.IsOpen;
        string prompt = (current == null || dialogueOpen) ? null : current.PromptText;
        if (prompt != lastPrompt)
        {
            lastPrompt = prompt;
            ShelterHUD.Instance.SetPrompt(prompt);
        }

        if (dialogueOpen || current == null) return;
        if (Time.frameCount == ShelterDialogue.LastClosedFrame) return;
        if (!Input.GetKeyDown(interactKey)) return;

        var target = current;
        current = null;
        target.SetHighlight(false);
        target.Interact(GetComponent<ShelterPlayer>());
    }

    /// <summary>自测/调试用：不去按键，直接和最近的可交互物互动；给了 namePart 就找名字里含它的那个。</summary>
    public void ForceInteract(string namePart = null)
    {
        var self = GetComponent<ShelterPlayer>();

        if (!string.IsNullOrEmpty(namePart))
        {
            var all = FindObjectsOfType<InteractableBase>();
            for (int i = 0; i < all.Length; i++)
            {
                if (!all[i].CanInteract) continue;
                if (all[i].displayName == null || !all[i].displayName.Contains(namePart)) continue;
                all[i].SetHighlight(false);
                all[i].Interact(self);
                return;
            }
            return;
        }

        var target = FindNearest();
        if (target == null) return;
        current = null;
        target.SetHighlight(false);
        target.Interact(self);
    }

    InteractableBase FindNearest()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, radius, hits, interactableMask);
        InteractableBase best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var interactable = hits[i].GetComponentInParent<InteractableBase>();
            if (interactable == null || !interactable.CanInteract) continue;

            float distance = ((Vector2)interactable.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = interactable;
            }
        }

        return best;
    }
}
