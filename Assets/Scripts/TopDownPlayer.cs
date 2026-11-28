using UnityEngine;

/// <summary>
/// 事件层（探索区）用的俯视角色：WASD / 方向键八方向走动，不跳、不受重力。
/// 照着美术给的俯视立绘换图：把 Sprite 拖到图子物体的 Sprite Renderer 上。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class TopDownPlayer : MonoBehaviour
{
    public float speed = 4.5f;
    [Tooltip("走路时轻微上下，想完全不动就关掉")]
    public bool bob = true;
    public float bobAmount = 0.02f;
    public float bobSpeed = 10f;

    public SpriteRenderer spriteRenderer;

    Rigidbody2D rb;
    Vector2 input;
    float facing = 1f;
    Vector3 basePos;
    float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) basePos = spriteRenderer.transform.localPosition;
    }

    static Vector2 ReadInput()
    {
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
        return new Vector2(x, y);
    }

    void Update()
    {
        input = ReadInput();
        if (input.sqrMagnitude > 1f) input.Normalize();

        if (Mathf.Abs(input.x) > 0.01f) facing = input.x > 0f ? 1f : -1f;

        if (spriteRenderer != null && bob)
        {
            if (input.sqrMagnitude > 0.01f)
            {
                timer += Time.deltaTime * bobSpeed;
                spriteRenderer.transform.localPosition = basePos + Vector3.up * (Mathf.Sin(timer) * bobAmount);
            }
            else
            {
                timer = 0f;
                spriteRenderer.transform.localPosition = basePos;
            }

            var s = spriteRenderer.transform.localScale;
            s.x = Mathf.Abs(s.x) * facing;
            spriteRenderer.transform.localScale = s;
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + input * speed * Time.fixedDeltaTime);
    }
}