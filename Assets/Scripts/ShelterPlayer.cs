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

    [Header("样子")]
    [Tooltip("会翻面的图（一般填玩家的子物体 图）。留空会自动找名字叫 图 的子物体")]
    public Transform spriteRoot;
    [Tooltip("走路时上下晃一点，纯装饰")]
    public bool bobWhileWalking = true;
    public float bobAmount = 0.035f;
    public float bobSpeed = 11f;

    /// <summary>对话打开时由 ShelterDialogue 关掉。</summary>
    public bool InputEnabled = true;

    /// <summary>自测用：非 0 时覆盖键盘水平输入（-1 左 / 1 右）。</summary>
    [System.NonSerialized] public float AutoMove;

    public bool IsGrounded { get { return grounded; } }

    /// <summary>自测/调试用：看当前速度。</summary>
    public Vector2 Velocity { get { return rb != null ? rb.velocity : Vector2.zero; } }

    /// <summary>瞬移（自测/调试用）：同时清掉速度，免得带着旧惯性飞出去。</summary>
    public void Teleport(Vector3 position)
    {
        rb.position = position;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.WakeUp();
        transform.position = position;
    }

    Rigidbody2D rb;
    BoxCollider2D box;
    bool grounded;
    float coyote;
    float jumpBuffer;
    float horizontal;
    float nextStepTime;
    float airLock;

    float facing = 1f;
    Vector3 spriteBasePos;
    float walkTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (spriteRoot == null)
        {
            spriteRoot = FindSpriteChild();
        }

        if (spriteRoot != null)
        {
            spriteBasePos = spriteRoot.localPosition;

            // 换了真图以后，根物体上那个占位方块就不用显示了
            if (spriteRenderer != null && spriteRenderer.transform == transform)
            {
                spriteRenderer.enabled = false;
            }
        }
    }

    /// <summary>找子物体里的 SpriteRenderer（玩家结构是「碰撞在根、图在子物体」）。</summary>
    Transform FindSpriteChild()
    {
        var found = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i].gameObject != gameObject)
            {
                return found[i].transform;
            }
        }
        return null;
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

        if (spriteRoot != null)
        {
            // 朝向：往左走把图翻过来
            if (horizontal > 0.01f) facing = 1f;
            else if (horizontal < -0.01f) facing = -1f;

            var scale = spriteRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * facing;
            spriteRoot.localScale = scale;

            // 走路时轻微上下晃（纯装饰，不影响碰撞）
            if (bobWhileWalking && grounded && Mathf.Abs(horizontal) > 0.01f)
            {
                walkTime += Time.deltaTime * bobSpeed;
                var pos = spriteBasePos;
                pos.y += Mathf.Sin(walkTime) * bobAmount;
                spriteRoot.localPosition = pos;
            }
            else
            {
                walkTime = 0f;
                spriteRoot.localPosition = spriteBasePos;
            }
        }
        else if (spriteRenderer != null)
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
        if (airLock > 0f) airLock -= Time.fixedDeltaTime;

        var checkPos = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.78f;

        // 用「脚底薄盒子向下 BoxCast」判地面：楼梯是 58° 的斜坡，脚下中心点到坡面的垂直距离
        // 有 0.6 左右，原来只用一个半径 0.18 的圆检测，在斜坡上会一直判成「没落地」，
        // 表现就是站在楼梯上跳不起来、还慢慢往下滑。
        var bounds = box.bounds;
        var feet = new Vector2(bounds.center.x, bounds.min.y + 0.02f);
        var cast = Physics2D.BoxCast(feet, new Vector2(bounds.size.x * 0.92f, 0.04f), 0f,
            Vector2.down, 0.14f, groundMask);

        bool touching = cast.collider != null
                        || Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundMask) != null;

        // 只在「没在往上飞」且「刚起跳的锁定时间过了」时才算踩在地上。
        grounded = touching && rb.velocity.y <= 0.01f && airLock <= 0f;

        // 站在斜坡（楼梯）上就顺着坡走，而不是被重力往下拽
        float slope = 0f;
        if (grounded && cast.collider != null && cast.normal.y > 0.15f)
        {
            slope = -cast.normal.x / cast.normal.y;
        }

        bool onSlope = grounded && Mathf.Abs(slope) > 0.25f;

        if (onSlope)
        {
            rb.gravityScale = 0f;
            rb.velocity = Mathf.Abs(horizontal) < 0.01f
                ? Vector2.zero
                : new Vector2(horizontal, horizontal * slope).normalized * moveSpeed;
        }
        else
        {
            rb.gravityScale = gravityScale;
            rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        }

        if (grounded) coyote = coyoteTime;
        else coyote -= Time.fixedDeltaTime;

        if (jumpBuffer > 0f && coyote > 0f)
        {
            rb.WakeUp();
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            rb.gravityScale = gravityScale;
            jumpBuffer = 0f;
            coyote = 0f;
            grounded = false;
            airLock = 0.12f; // 起跳后短暂忽略地面检测，免得立刻被斜坡拽回去
        }
        else if (grounded && Mathf.Abs(rb.velocity.y) < 0.2f)
        {
            StepAssist(horizontal);
        }
    }

    /// <summary>自测/调试用：不走键盘，直接请求一次跳跃。</summary>
    public void ForceJump()
    {
        rb.WakeUp();
        jumpBuffer = jumpBufferTime;
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
