using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>给场景里现有的棋盘入口补上「进棋盘前的文案」，不用重建整个探索区。</summary>
public static class IntroTool
{
    [MenuItem("工具/25小时/给棋盘入口补上开场文案（当前场景）", false, 8)]
    public static void FillIntro()
    {
        var gates = Object.FindObjectsOfType<BoardGate>(true);
        int count = 0;

        for (int i = 0; i < gates.Length; i++)
        {
            var gate = gates[i];
            if (gate.introSlides != null && gate.introSlides.Length > 0) continue;

            gate.introSlides = new[]
            {
                gate.gateTitle + "|" + gate.introText,
                "你把「" + gate.requiredItemName + "」按进凹槽|凹槽边缘亮起一圈细光，台面上的纹路一格一格睁开了眼。",
                "接下来|你只看得见这张棋盘。",
            };
            EditorUtility.SetDirty(gate);
            count++;
        }

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("[开场文案] 补了 " + count + " 个棋盘入口（共找到 " + gates.Length + " 个）");
    }
}
