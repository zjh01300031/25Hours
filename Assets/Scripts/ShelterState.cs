using UnityEngine;

/// <summary>
/// 事件完成后，第0层的视觉变化：顶灯亮、背景变暖、展柜里多一个人偶。
/// 哪个事件算"完成"看这里的 eventId，默认 event_01。
/// </summary>
public class ShelterState : MonoBehaviour
{
    [Tooltip("触发这些变化的那个事件 id")]
    public string eventId = "event_01";

    public GameObject lampGlow;
    public GameObject newDoll;
    public SpriteRenderer backWall;
    public Sprite warmWallSprite;

    void Start()
    {
        var progress = GameProgress.Instance;
        if (progress == null || !progress.IsEventCleared(eventId)) return;

        if (lampGlow != null) lampGlow.SetActive(true);
        if (newDoll != null) newDoll.SetActive(true);
        if (backWall != null && warmWallSprite != null) backWall.sprite = warmWallSprite;
        if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast("事件完成 · 展柜里多了一个人偶");
    }
}
