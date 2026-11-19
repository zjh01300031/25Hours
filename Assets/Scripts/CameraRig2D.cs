using UnityEngine;

/// <summary>平滑跟随玩家，并把镜头夹在房间范围内。</summary>
public class CameraRig2D : MonoBehaviour
{
    public Transform target;
    public Vector2 offset = new Vector2(0f, 1.2f);
    public float smoothTime = 0.18f;
    public bool useBounds = true;
    public Vector2 min = new Vector2(-8.4f, -3.4f);
    public Vector2 max = new Vector2(6.4f, 2.9f);

    Vector2 velocity;

    void LateUpdate()
    {
        if (target == null) return;

        var want = (Vector2)target.position + offset;
        if (useBounds)
        {
            want.x = Mathf.Clamp(want.x, min.x, max.x);
            want.y = Mathf.Clamp(want.y, min.y, max.y);
        }

        var pos = Vector2.SmoothDamp(transform.position, want, ref velocity, smoothTime);
        transform.position = new Vector3(pos.x, pos.y, -10f);
    }
}
