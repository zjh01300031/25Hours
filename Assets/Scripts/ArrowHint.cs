using UnityEngine;

/// <summary>指着通道的箭头：事件没通关不显示，出现后上下轻微浮动。</summary>
public class ArrowHint : MonoBehaviour
{
    [Tooltip("要先把哪个事件打完才显示")]
    public string eventId = "event_02";

    public float bobAmount = 0.18f;
    public float bobSpeed = 3.2f;

    SpriteRenderer sr;
    Vector3 basePos;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Marker.White;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(0.9f, 0.3f);
            sr.color = new Color(1f, 0.9f, 0.5f, 0.95f);
            sr.sortingOrder = 6;
        }

        basePos = transform.position;
        gameObject.SetActive(GameProgress.Ensure().IsEventCleared(eventId));
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        transform.position = basePos + Vector3.right * Mathf.Sin(Time.time * bobSpeed) * bobAmount;
    }
}
