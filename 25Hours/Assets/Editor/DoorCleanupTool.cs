using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DoorCleanupTool
{
    const string FlagPath = "Temp/remove-doors.flag";
    const string LogPath = "Temp/remove-doors.log";

    [MenuItem("工具/25小时/删掉当前场景里的事件门（并保存）", false, 30)]
    public static void RemoveDoors()
    {
        var doors = Object.FindObjectsOfType<EventDoor>(true);
        int removed = 0;

        for (int i = 0; i < doors.Length; i++)
        {
            var door = doors[i];
            if (door == null) continue;

            var go = door.gameObject;
            // 预制体实例要删最外层的根，不然只删掉一个空壳
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            if (root != null) go = root;

            Debug.Log("[门] 删掉：" + go.name);
            Object.DestroyImmediate(go);
            removed++;
        }

        if (removed > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("[门] 完成：删掉 " + removed + " 个 EventDoor，场景：" + EditorSceneManager.GetActiveScene().path);
        System.IO.File.WriteAllText(LogPath, System.DateTime.Now + " removed=" + removed);
    }

    [InitializeOnLoadMethod]
    static void AutoRunIfRequested()
    {
        if (!System.IO.File.Exists(FlagPath)) return;
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
        System.IO.File.Delete(FlagPath);
        EditorApplication.delayCall += RemoveDoors;
    }
}