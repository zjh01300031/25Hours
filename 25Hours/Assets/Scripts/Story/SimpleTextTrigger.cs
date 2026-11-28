using UnityEngine;

/// <summary>
/// 最简单的「按 E 出文字」。
/// 用法：把脚本挂到任何物体上，物体放 Interactable 层、加一个 Is Trigger 的碰撞体，
/// 然后在 Inspector 的 Lines 里一行写一句，游戏里对着它按 E 就会弹对话框。
/// 不需要 CSV、不需要改代码，所以这个东西可以到处复制复用。
/// </summary>
public class SimpleTextTrigger : InteractableBase
{
    [Tooltip("对话框左上角显示的名字，例如：旁白 / 纸箱 / 门")]
    public string speaker = "旁白";

    [Tooltip("一行一句，填几行就显示几句；留空则只弹一句提示")]
    [TextArea(2, 8)]
    public string[] lines = new string[0];

    public override void Interact(ShelterPlayer player)
    {
        // 没填字就当占位，给个提示，不要报错
        if (lines == null || lines.Length == 0)
        {
            if (ShelterHUD.Instance != null)
            {
                ShelterHUD.Instance.Toast("（还没填文字）");
            }
            return;
        }

        // 有对话框就交给对话框；没有就退化成控制台打印，方便单独测逻辑
        if (ShelterDialogue.Instance != null)
        {
            ShelterDialogue.Instance.Show(speaker, lines);
        }
        else
        {
            Debug.Log(speaker + "：" + string.Join(" / ", lines));
        }
    }
}
