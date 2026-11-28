using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 小调整：把交互提示（toast）从屏幕上方挪到下方，然后重生成主菜单。
/// 菜单：工具 / 25小时 / 提示挪到下面 + 重生成主菜单
/// </summary>
public static class UiTweakTool
{
    const string FlagPath = "Temp/ui-tweak.flag";
    const string LogPath = "Temp/ui-tweak.log";
    const string ShelterScene = "Assets/Scenes/Shelter_L0.unity";

    [MenuItem("工具/25小时/提示挪到下面 + 重生成主菜单", false, 4)]
    public static void Run()
    {
        MoveToastToBottom();
        MenuBuilder.Build();
        Debug.Log("[UI] 交互提示已挪到下方，主菜单已重生成");
        WriteLog("[UI] 完成 " + System.DateTime.Now.ToString("HH:mm:ss"));
    }

    static void MoveToastToBottom()
    {
        if (!File.Exists(ShelterScene)) return;

        var scene = EditorSceneManager.OpenScene(ShelterScene, OpenSceneMode.Single);
        int moved = 0;

        foreach (var rt in Object.FindObjectsOfType<RectTransform>(true))
        {
            if (rt.name != "toast") continue;

            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 196f);      // 提示条上方
            rt.sizeDelta = new Vector2(1200f, 44f);
            moved++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        WriteLog("[UI] toast 调整：" + moved + " 个");
    }

    [InitializeOnLoadMethod]
    static void AutoRunIfRequested()
    {
        if (!File.Exists(FlagPath)) return;
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

        try { File.Delete(FlagPath); } catch { }
        EditorApplication.delayCall += () =>
        {
            try { Run(); }
            catch (System.Exception e)
            {
                Debug.LogError("[UI] 失败：" + e);
                WriteLog("[UI] 失败：" + e);
            }
        };
    }

    static void WriteLog(string line)
    {
        try { File.AppendAllText(LogPath, line + "\n"); } catch { }
    }
}
