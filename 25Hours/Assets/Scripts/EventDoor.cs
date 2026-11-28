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
    [Tooltip("交互后进入哪一层：填场景名（必须已经加进 Build Settings），例如 Level_01")]
    public string nextLevelScene;

    [Tooltip("可选：要先把哪一层打完才打开。留空 = 一开始就能进")]
    public string requiresLevel;

    [Tooltip("旧的事件 id，只作为 nextLevelScene 没填时的兜底（查事件登记表）")]
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

    [Header("文案 id（优先于上面的数组，内容在 Resources/Story/story.csv）")]
    [Tooltip("进门前播的文案 id")]
    public string enterStoryId;
    [Tooltip("门锁着时播的文案 id")]
    public string lockedStoryId;
    [Tooltip("事件做完以后再碰门播的文案 id")]
    public string clearedStoryId;

    [Header("门的样子")]
    [Tooltip("真正显示门的那张图的 SpriteRenderer（留空会自动在子物体里找「图」）")]
    public SpriteRenderer doorRenderer;
    public Sprite lockedSprite;
    public Sprite unlockedSprite;

    [Tooltip("门口那团光（没有就不换色）")]
    public SpriteRenderer statusGlow;
    public Color lockedGlow = new Color(1f, 0.32f, 0.3f, 0.18f);
    public Color unlockedGlow = new Color(1f, 0.86f, 0.56f, 0.24f);

    bool wasLocked = true;

    /// <summary>前置事件没完成 = 锁着。</summary>
    public bool IsLocked
    {
        get
        {
            string need = !string.IsNullOrEmpty(requiresLevel) ? requiresLevel : requiresEventId;
            if (string.IsNullOrEmpty(need)) return false;
            return !GameProgress.Ensure().IsEventCleared(need);
        }
    }

    public override string PromptText
    {
        get { return IsLocked ? "查看 " + displayName : "进入下一层"; }
    }

    void Start()
    {
        if (doorRenderer == null) doorRenderer = GetComponentInChildren<SpriteRenderer>();
        RefreshLook();
    }

    void Update()
    {
        // 前置关卡打完后，把门的样子换成「已打开」
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
            PlayText(lockedStoryId, lockedLines);
            if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast("这扇门还锁着");
            return;
        }

        string scene = ResolveScene();
        if (string.IsNullOrEmpty(scene))
        {
            if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast("还没设置下一层（门的 Next Level Scene 填场景名）");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast("场景 " + scene + " 不在 Build Settings 里");
            return;
        }

        if (ShelterHUD.Instance != null) ShelterHUD.Instance.Toast("进入 " + scene);

        // 进下一层前，把本层标记成已完成（供下一扇门的 requiresLevel 判断）
        GameProgress.Ensure().MarkEventCleared(eventId);

        SceneFade.FadeTo(scene, 0.5f, 0.35f, 0.9f);
    }

    string ResolveScene()
    {
        if (!string.IsNullOrEmpty(nextLevelScene)) return nextLevelScene;

        var catalog = EventCatalog.Instance;
        EventInfo info;
        if (catalog != null && catalog.TryGet(eventId, out info) && info != null && !string.IsNullOrEmpty(info.sceneName))
        {
            return info.sceneName;
        }

        return null;
    }

    void PlayText(string storyId, string[] lines)
    {
        if (!string.IsNullOrEmpty(storyId) && Story.Has(storyId) && ShelterDialogue.Instance != null)
        {
            ShelterDialogue.Instance.Show(Story.Speaker(storyId), Story.Lines(storyId));
            return;
        }

        ShowLines(lines);
    }

    void ShowLines(string[] lines)
    {
        if (lines == null || lines.Length == 0 || ShelterDialogue.Instance == null) return;
        ShelterDialogue.Instance.Show(displayName, lines);
    }
}
