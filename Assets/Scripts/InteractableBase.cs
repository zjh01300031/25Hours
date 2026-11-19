using UnityEngine;

/// <summary>
/// 所有「按 E 能互动」的东西都继承它。
/// 物体放在 Interactable 层，挂一个 Trigger 碰撞体给玩家的检测圈找。
/// </summary>
public abstract class InteractableBase : MonoBehaviour
{
    [Tooltip("提示里显示的名字，例如：纸箱")]
    public string displayName = "东西";

    [Tooltip("提示里的动词，例如：查看 / 翻找 / 对话 / 进入")]
    public string verb = "查看";

    [Tooltip("靠近时亮起的那个子物体（由生成器自动创建）")]
    public GameObject highlight;

    public virtual bool CanInteract => true;

    public virtual string PromptText => verb + " " + displayName;

    public abstract void Interact(ShelterPlayer player);

    public void SetHighlight(bool on)
    {
        if (highlight != null && highlight.activeSelf != on)
        {
            highlight.SetActive(on);
        }
    }
}
