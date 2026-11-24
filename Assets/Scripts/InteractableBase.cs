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

    protected virtual void Awake()
    {
        // 一个物体上如果挂了两个「可交互」脚本，按 E 只会跑到其中一个，
        // 表现出来就是「我改了怎么还是老逻辑」。这里直接报警。
        var all = GetComponents<InteractableBase>();
        if (all != null && all.Length > 1)
        {
            Debug.LogWarning("[交互] " + name + " 上挂了 " + all.Length + " 个交互脚本（"
                + string.Join(" / ", System.Array.ConvertAll(all, c => c.GetType().Name))
                + "），按 E 只会触发其中一个。请删掉不需要的组件。", this);
        }
    }

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
