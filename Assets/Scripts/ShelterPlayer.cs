using UnityEngine;

/// <summary>
/// 第0层的玩家（红色主控）。A/D 移动、空格跳，会自动上很矮的台阶。
/// 想调手感：改 Inspector 里的 Move Speed / Jump Force / Gravity Scale / Step Height。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class ShelterPlayer : MonoBehaviour
{
    [Header("手感")]
    public float moveSpeed = 5f;
    public float jumpForce = 12.5f;
    [Range(1f, 8f)] public float gravityScale = 3.5f;
    [Tooltip("能自动迈上去的台阶高度上限")]
    public float stepHeight = 0.55f;
    public float coyoteTime = 0.08f;
    public float jumpBufferTime = 0.1f;

    [Header("地面检测")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.18f;
    public LayerMask groundMask;

    [Header("出生点")]
    public Vector3 spawnPoint = new Vector3(11.5f, 1.1f, 0f);

    [Header("引用")]
    public SpriteRenderer spriteRenderer;

    /// <summary>对话打开时由 ShelterDialogue 关掉。</summary>
    public bool InputEnabled = true;

    /// <summary>自测用：非 0 时覆盖键盘水平输入（-1 左 / 1 右）。</summary>
    [System.NonSerialized] public float AutoMove;

    public bool IsGrounded { get { return grounded; } }

    /// <summary>瞬移（自测/调试用）：同时清掉速度，免得带着旧惯性飞出去。</summary>
    public void Teleport(Vector3 position)
    {
        rb.position = position;
        rb.velocity = Vector2.zero;
        transform.position = position;
    }

    Rigidbody2D rb;
    BoxCollider2D box;
    bool grounded;
    float coyote;
    float jumpBuffer;
    float horizontal;
    float nextStepTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start()
    {
        // 从事件场景回来时，回到离开的位置
        if (ShelterFlow.HasReturnPoint)
        {
            transform.position = ShelterFlow.ReturnPoint;
            ShelterFlow.ConsumeReturnPoint();
        }
        else
        {
            transform.position = spawnPoint;
        }
    }

    void Update()
    {
        if (transform.position.y < -30f)
        {
            transform.position = spawnPoint;
            rb.velocity = Vector2.zero;
        }

        if (!InputEnabled)
        {
            horizontal = 0f;
            return;
        }

        horizontal = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(AutoMove) > 0.01f) horizontal = AutoMove;
        if (Mathf.Abs(horizontal) < 0.01f) horizontal = 0f;

        if (spriteRenderer != null)
        {
            if (horizontal > 0.01f) spriteRenderer.flipX = false;
            else if (horizontal < -0.01f) spriteRenderer.flipX = true;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            jumpBuffer = jumpBufferTime;
        }
        else
        {
            jumpBuffer -= Time.unscaledDeltaTime;
        }
    }

    void FixedUpdate()
    {
        var checkPos = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.78f;
        grounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundMask) != null;

        // 脚下如果踩在斜坡（楼梯）上，就顺着坡走，而不是被重力往下拽
        float slope = 0f;
        if (grounded)
        {
            var down = Physics2D.Raycast(checkPos, Vector2.down, 0.9f, groundMask);
            if (down.collider != null && down.normal.y > 0.15f) slope = -down.normal.x / down.normal.y;
        }

        bool onSlope = grounded && Mathf.Abs(slope) > 0.25f;
        rb.gravityScale = onSlope ? 0f : gravityScale;

        if (onSlope)
        {
            rb.velocity = Mathf.Abs(horizontal) < 0.01f
                ? Vector2.zero
                : new Vector2(horizontal * moveSpeed, horizontal * moveSpeed * slope);
        }
        else
        {
            float vy = rb.velocity.y;
            if (grounded && vy > 0.5f) vy = 0f; // 刚走出斜坡时别被弹起来
            rb.velocity = new Vector2(horizontal * moveSpeed, vy);
        }

        if (grounded) coyote = coyoteTime;
        else coyote -= Time.fixedDeltaTime;

        if (jumpBuffer > 0f && coyote > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            rb.gravityScale = gravityScale;
            jumpBuffer = 0f;
            coyote = 0f;
            grounded = false;
        }
        else if (grounded && Mathf.Abs(rb.velocity.y) < 0.2f)
        {
            StepAssist(horizontal);
        }
    }

    /// <summary>撞到矮台阶时把脚抬上去——楼梯有斜坡碰撞，这个留给小台沿。</summary>
    void StepAssist(float dir)
    {
        if (Time.time < nextStepTime || Mathf.Abs(dir) < 0.1f) return;

        var bounds = box.bounds;
        var origin = new Vector2(bounds.center.x, bounds.min.y + 0.08f);
        var hit = Physics2D.Raycast(origin, Vector2.right * Mathf.Sign(dir), bounds.extents.x + 0.2f, groundMask);
        if (hit.collider == null) return;

        float climb = hit.collider.bounds.max.y - bounds.min.y;
        if (climb > 0.05f && climb <= stepHeight)
        {
            rb.position = new Vector2(rb.position.x, rb.position.y + climb + 0.02f);
            nextStepTime = Time.time + 0.12f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.color = Color.cyan;
        if (box == null) box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            var bounds = box.bounds;
            Gizmos.DrawLine(
                new Vector3(bounds.center.x, bounds.min.y + stepHeight, 0f),
                new Vector3(bounds.center.x + (bounds.extents.x + 0.22f), bounds.min.y + stepHeight, 0f));
        }
    }
}
