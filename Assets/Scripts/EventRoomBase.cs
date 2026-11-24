using UnityEngine;

/// <summary>
/// 事件场景的接入基类。事件负责人写一个子类挂在场景里的一个物体上：
///
///   public class 我的事件 : EventRoomBase
///   {
///       protected override void Start()
///       {
///           base.Start();          // ← 别忘了这一句
///           // 在这里初始化棋盘 / 读玩家进度
///       }
///
///       void 解谜成功()
///       {
///           Complete();            // 记录事件完成 + 回第0层
///       }
///   }
///
/// 中途退出（玩家按 Esc 之类）就调 Leave()，不记录完成。
/// 事件场景必须加进 Build Settings。
/// </summary>
public abstract class EventRoomBase : MonoBehaviour
{
    [Tooltip("事件 id，要和第0层 EventCatalog 里登记的一致")]
    [SerializeField] protected string eventId = "event_01";

    public string EventId { get { return eventId; } }

    /// <summary>给场景生成器用（编辑器里也能手动改 Inspector）。</summary>
    public void SetEventId(string id)
    {
        eventId = id;
    }

    [Header("测试时看得见的占位")]
    [Tooltip("事件点的占位颜色。没挂任何 SpriteRenderer 时，运行时会自动长出一个方块，Scene 视图里也有线框。")]
    public Color placeholderColor = new Color(1f, 0.62f, 0.28f, 0.9f);

    protected virtual void Awake()
    {
        EnsurePlaceholder();
    }

    /// <summary>
    /// 事件点常常是个空物体，测试时根本看不到。
    /// 这里给它补一个运行时方块（没贴图也看得见），Scene 视图另有线框。
    /// </summary>
    protected void EnsurePlaceholder()
    {
        Marker.EnsureBox(transform, placeholderColor, 1.2f);
    }

    void OnDrawGizmos()
    {
        Marker.Wire(transform, placeholderColor, 1.2f);
    }

    protected virtual void Start()
    {
        // 直接从编辑器打开这个场景单跑时，用当前事件补一下
        if (string.IsNullOrEmpty(eventId) && !string.IsNullOrEmpty(ShelterFlow.CurrentEventId))
        {
            eventId = ShelterFlow.CurrentEventId;
        }

        OnEnter();
    }

    /// <summary>场景加载后调用：初始化棋盘、读 GameProgress。</summary>
    public virtual void OnEnter() { }

    /// <summary>
    /// 事件完成：先记录完成状态。
    /// 场景里如果有 LevelFlow（一层有多个事件的情况），交给它判断「是不是全都做完了」；
    /// 没有 LevelFlow 就直接回第0层。
    /// </summary>
    public void Complete()
    {
        GameProgress.Ensure().MarkEventCleared(eventId);

        if (LevelFlow.Instance != null)
        {
            LevelFlow.Instance.Refresh();
            return;
        }

        ShelterFlow.ReturnToShelter();
    }

    /// <summary>中途离开：不记录完成。</summary>
    public void Leave()
    {
        ShelterFlow.ReturnToShelter();
    }
}
