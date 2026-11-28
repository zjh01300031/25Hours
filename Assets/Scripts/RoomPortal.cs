using UnityEngine;

/// <summary>
/// 房间之间的通道：玩家走进来 → 黑屏 2 秒 → 另一个房间渐显。
/// 目标场景里放一个同名的「到达点」，玩家会站在它附近出现。
/// </summary>
public class RoomPortal : MonoBehaviour
{
    [Header("去哪")]
    public string targetScene = "Shelter_2F";

    [Header("到了之后站在哪个物体旁边（目标场景里的物体名）")]
    public string spawnObjectName = "入口_一层";

    [Header("触发")]
    public float triggerRadius = 1.2f;
    public bool needFacingRight = false;

    [Header("转场时间（秒）")]
    public float fadeOut = 0.7f;
    public float blackHold = 2.0f;
    public float fadeIn = 1.0f;

    [Header("看起来的样子")]
    public Color color = new Color(1f, 0.92f, 0.6f, 0.85f);
    public Vector2 size = new Vector2(0.5f, 2.4f);

    bool busy;
    float armedAt;

    void Start()
    {
        if (GetComponent<SpriteRenderer>() == null)
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Marker.White;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = color;
            sr.sortingOrder = 4;
        }

        armedAt = Time.time + 0.6f;   // 刚进场别立刻又被弹回去
    }

    void Update()
    {
        if (busy || InputGate.Blocked) return;
        if (Time.time < armedAt) return;

        Transform actor = null;
        var top = FindObjectOfType<TopDownPlayer>();
        if (top != null) actor = top.transform;
        else
        {
            var p = FindObjectOfType<ShelterPlayer>();
            if (p != null) actor = p.transform;
        }
        if (actor == null) return;

        if (Vector2.Distance(actor.position, transform.position) > triggerRadius) return;

        busy = true;
        RoomSpawn.PendingName = spawnObjectName;
        SceneFade.FadeTo(targetScene, fadeOut, blackHold, fadeIn);
    }
}
