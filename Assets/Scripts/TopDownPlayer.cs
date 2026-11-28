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

    [Tooltip("允许上下走（W/S）。关掉就只有 A/D 左右走")]
    public bool allowVertical = false;

    [Tooltip("关掉就站住不动（弹面板、转场时用）")]
    public bool inputEnabled = true;

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

    Vector2 ReadInput()
    {
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (allowVertical)
        {
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;
        }
        return new Vector2(x, y);
    }

    void Update()
    {
        input = ReadInput();
        if (input.sqrMagnitude > 1f) input.Normalize();

        if (Mathf.Abs(input.x) > 0.01f) facing = input.x > 0f ? 1f : -1f;

        UpdateDiag();

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
        rb.velocity = input * speed;
    }

    // ------- 排查用：每秒把状态写进 D:\Unity2D\Build\topdown_debug.txt -------
    float diagTimer;
    static bool diagFailed;

    void UpdateDiag()
    {
        if (diagFailed) return;
        diagTimer += Time.unscaledDeltaTime;
        if (diagTimer < 1f) return;
        diagTimer = 0f;

        try
        {
            string path = "D:/Unity2D/Build/topdown_debug.txt";
            System.IO.Directory.CreateDirectory("D:/Unity2D/Build");
            string line = string.Format(
                "{0:HH:mm:ss}  pos=({1:0.00},{2:0.00})  input=({3:0},{4:0})  vel=({5:0.00},{6:0.00})  speed={7}  body={8}  simulated={9}  enabled={10}  ts={11:0.00}  focused={12}\n",
                System.DateTime.Now, transform.position.x, transform.position.y,
                input.x, input.y, rb.velocity.x, rb.velocity.y, speed,
                rb.bodyType, rb.simulated, enabled, Time.timeScale, Application.isFocused);
            System.IO.File.AppendAllText(path, line);
        }
        catch { diagFailed = true; }
    }
}