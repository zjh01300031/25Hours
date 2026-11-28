using UnityEngine;

/// <summary>换房间后，新场景里的玩家要站在「到达点」旁边。</summary>
public static class RoomSpawn
{
    public static string PendingName;

    public static bool Consume(out Vector3 pos)
    {
        pos = Vector3.zero;
        if (string.IsNullOrEmpty(PendingName)) return false;

        var go = GameObject.Find(PendingName);
        PendingName = null;
        if (go == null) return false;

        pos = go.transform.position + Vector3.up * 0.75f;
        return true;
    }
}
