using UnityEngine;

/// <summary>
/// 事件门：第0层里通向某个事件场景的门（事件一、事件二）。
///
/// - eventId：要进哪个事件（和 EventCatalog 登记表里的 id 一致）。
/// - requiresEventId：前置事件；填了就变成"锁着的门"，前置事件完成后自动开。
/// - 门的样子：lockedSprite / unlockedSprite 会自动切换，statusGlow 的颜色也跟着换。
/// </summary>
public class EventDoor : InteractableBase
{
    [Tooltip("要进入的事件 id（要和 EventCatalog 里登记的一致）")]
    public string eventId = "event_01";

    [Tooltip("要先完成的前置事件 id；留空表示一开始就能开")]
    public string requiresEventId;

    [Tooltip("开门前说的话（留空就直接进）")]
    public string[] enterLines;

    [Tooltip("门还锁着的时候说的话（留空就只给 toast 提示）")]
    public string[] lockedLines;

    [Tooltip("这个事件已经完成过以后说的话")]
    public string[] clearedLines;

    [Tooltip("登记表里没有 / 场景没进 Build Settings 时说的话")]
    public string[] missingLines;

    [Header("门的样子")]
    [Tooltip("真正显示门的那张图的 SpriteRenderer（留空会自动在子物体里找「图」）")]
    public SpriteRenderer doorRenderer;
    public Sprite lockedSprite;
    public Sprite unlockedSprite;

    [Tooltip("门口那团光（没有就不换色）")]
    public SpriteRenderer statusGlow;
    public Color lockedGlow = new Color(1f, 0.32f, 0.3f, 0.18f);
    public Color unlockedGlow = new Color(1f, 0.86f, 0.56f, 0.24f);

    string pendingScene;
    bool wasLocked = true;

    /// <summary>前置事件没完成 = 锁着。</summary>
    public bool IsLocked
    {
        get
        {
            if (string.IsNullOrEmpty(requiresEventId)) return false;
            return !GameProgress.Ensure().IsEventCleared(requiresEventId);
        }
    }

    public override string PromptText
    {
        get { return IsLocked ? "查看 " + displayName : "进入 " + displayName; }
    }

    void Start()
    {
        if (doorRenderer == null) doorRenderer = GetComponentInChildren<SpriteRenderer>();
        RefreshLook();
    }

    void Update()
    {
        if (pendingScene != null && !ShelterDialogue.IsOpen)
        {
            string scene = pendingScene;
            pendingScene = null;
            ShelterFlow.BeginEvent(eventId, scene);
            return;
        }

        // 门在同一场景里被解锁（做完前置事件回来）时，把外观换掉
        if (IsLocked != wasLocked) RefreshLook();
    }

    void RefreshLook()
    {
        bool locked = IsLocked;
        wasLocked = locked;

        if (doorRenderer == null) doorRenderer = GetComponentInChildren<SpriteRenderer>();
        if (doorRenderer != null)
        {
            Sprite want = locked ? lockedSprite : unlockedSprite;
            if (want != null && doorRenderer.sprite != want) doorRenderer.sprite = want;
        }

        if (statusGlow != null) statusGlow.color = locked ? lockedGlow : unlockedGlow;
    }

    public override void Interact(ShelterPlayer player)
    {
        if (IsLocked)
        {
            ShowLines(lockedLines);
            if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast("门锁着 · 要先完成前置事件");
            return;
        }

        if (GameProgress.Ensure().IsEventCleared(eventId))
        {
            ShowLines(clearedLines);
            return;
        }

        string scene = ResolveScene();
        if (string.IsNullOrEmpty(scene))
        {
            ShowLines(missingLines);
            if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast("（这扇门后面还没接事件）");
            return;
        }

        ShelterFlow.RememberShelterPosition(player != null ? player.transform.position : transform.position);

        if (enterLines != null && enterLines.Length > 0)
        {
            ShowLines(enterLines);
            pendingScene = scene;
        }
        else
        {
            ShelterFlow.BeginEvent(eventId, scene);
        }
    }

    string ResolveScene()
    {
        var catalog = EventCatalog.Instance;
        EventInfo info;
        if (catalog != null && catalog.TryGet(eventId, out info) && info != null && !string.IsNullOrEmpty(info.sceneName))
        {
            return info.sceneName;
        }

        return null;
    }

    void ShowLines(string[] lines)
    {
        if (lines == null || lines.Length == 0 || ShelterDialogue.Instance == null) return;
        ShelterDialogue.Instance.Show(displayName, lines);
    }
}
