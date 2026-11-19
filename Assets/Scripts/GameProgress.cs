using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 跨场景的进度：碎片、拾取物、事件完成记录、回程坐标，并且会存到本地存档里。
/// 场景里放一个 GameProgress 物体，切场景后会自动保留。
///
/// 【别人的代码怎么用】
///   GameProgress.Ensure().AddFragment("frag_07");
///   GameProgress.Ensure().AddItem("item_tape", 1);
///   GameProgress.Ensure().MarkEventCleared("event_01");
///   GameProgress.Ensure().IsEventCleared("event_01");
///   GameProgress.EventCleared += 收到通知的方法;   // 事件完成时回调
/// 不用去场景里找物体，静态方法会自己补一个。
///
/// 存档位置：%USERPROFILE%\AppData\LocalLow\Zhao\第25小时\progress.json
/// 命令行带 -selftest 时不读不写存档，保证自测每次从头开始。
/// </summary>
public class GameProgress : MonoBehaviour
{
    public static GameProgress Instance { get; private set; }

    /// <summary>第0层一共藏了多少片文案碎片。</summary>
    public const int FragmentTotal = 6;

    /// <summary>回程坐标（由 ShelterFlow 维护，这里保留是为了兼容旧的读取方）。</summary>
    public bool hasReturnPos;
    public Vector3 shelterReturnPos;

    [Serializable]
    class ItemStack
    {
        public string id;
        public int count;
    }

    [Serializable]
    class SaveData
    {
        public List<string> fragments = new List<string>();
        public List<string> events = new List<string>();
        public List<ItemStack> items = new List<ItemStack>();
    }

    readonly HashSet<string> fragments = new HashSet<string>();
    readonly HashSet<string> clearedEvents = new HashSet<string>();
    readonly Dictionary<string, int> items = new Dictionary<string, int>();

    bool saveEnabled = true;

    public int FragmentCount { get { return fragments.Count; } }

    /// <summary>收到一片碎片时触发：(碎片 id, 现在有几片, 总共有几片)</summary>
    public static event Action<string, int, int> FragmentChanged;

    /// <summary>某个事件第一次被标记完成时触发：(事件 id)</summary>
    public static event Action<string> EventCleared;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        saveEnabled = !HasCommandLine("-selftest");
        if (saveEnabled) Load();
    }

    static bool HasCommandLine(string key)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == key) return true;
        }
        return false;
    }

    // ------------------------------------------------------------------
    // 碎片
    // ------------------------------------------------------------------

    public bool HasFragment(string id)
    {
        return !string.IsNullOrEmpty(id) && fragments.Contains(id);
    }

    public bool AddFragment(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (!fragments.Add(id)) return false;

        Save();
        var handler = FragmentChanged;
        if (handler != null) handler(id, fragments.Count, FragmentTotal);
        return true;
    }

    // ------------------------------------------------------------------
    // 拾取物
    // ------------------------------------------------------------------

    public int ItemCount(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        int count;
        return items.TryGetValue(id, out count) ? count : 0;
    }

    /// <summary>加物品，返回加完以后的数量（0 表示 id 为空）。</summary>
    public int AddItem(string id, int amount = 1)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0) return 0;

        int count = ItemCount(id) + amount;
        items[id] = count;
        Save();
        return count;
    }

    public void RemoveItem(string id, int amount = 1)
    {
        if (string.IsNullOrEmpty(id)) return;
        int count = ItemCount(id) - amount;
        if (count <= 0) items.Remove(id); else items[id] = count;
        Save();
    }

    // ------------------------------------------------------------------
    // 事件
    // ------------------------------------------------------------------

    public bool IsEventCleared(string eventId)
    {
        return !string.IsNullOrEmpty(eventId) && clearedEvents.Contains(eventId);
    }

    /// <summary>标记事件完成。第一次标记时返回 true 并广播 EventCleared。</summary>
    public bool MarkEventCleared(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return false;
        if (!clearedEvents.Add(eventId)) return false;

        Save();
        var handler = EventCleared;
        if (handler != null) handler(eventId);
        return true;
    }

    public void RememberShelterPos(Vector3 pos)
    {
        shelterReturnPos = pos;
        hasReturnPos = true;
    }

    // ------------------------------------------------------------------
    // 存档
    // ------------------------------------------------------------------

    public static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, "progress.json"); }
    }

    public void Save()
    {
        if (!saveEnabled) return;

        var data = new SaveData();
        data.fragments.AddRange(fragments);
        data.events.AddRange(clearedEvents);
        foreach (var pair in items) data.items.Add(new ItemStack { id = pair.Key, count = pair.Value });

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GameProgress] 存档失败：" + e.Message);
        }
    }

    public void Load()
    {
        if (!File.Exists(SavePath)) return;

        try
        {
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null) return;

            if (data.fragments != null) foreach (var id in data.fragments) fragments.Add(id);
            if (data.events != null) foreach (var id in data.events) clearedEvents.Add(id);
            if (data.items != null)
            {
                foreach (var stack in data.items)
                {
                    if (stack != null && !string.IsNullOrEmpty(stack.id)) items[stack.id] = stack.count;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GameProgress] 读档失败：" + e.Message);
        }
    }

    /// <summary>清空进度并删档（自测、调试用）。</summary>
    public void ResetProgress()
    {
        fragments.Clear();
        clearedEvents.Clear();
        items.Clear();
        hasReturnPos = false;

        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[GameProgress] 删档失败：" + e.Message);
        }
    }

    /// <summary>没有进度物体时补一个（比如直接从事件场景开始运行）。</summary>
    public static GameProgress Ensure()
    {
        if (Instance == null)
        {
            Instance = new GameObject("GameProgress").AddComponent<GameProgress>();
        }

        return Instance;
    }
}
