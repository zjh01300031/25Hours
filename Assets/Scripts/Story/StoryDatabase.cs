using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 文案仓库：所有台词都放在 Assets/Resources/Story/story.csv 里，按 id 取。
///
/// CSV 三列：id,speaker,text
///   - 同一个 id 写多行 = 一段对话的多句，按文件顺序播放
///   - 用双引号包住可以写逗号和换行
///   - 改完不用重新打包，编辑器里 Play 就能读到（外部改 csv 再进游戏也行）
///
/// 【别人的代码怎么用】
///   if (Story.Has("door01.enter")) Dialogue.Show(Story.Speaker("door01.enter"), Story.Lines("door01.enter"));
///   或者直接挂 StoryInteractable，填一个 storyId 就行。
/// </summary>
public static class Story
{
    public const string ResourcePath = "Story/story";

    public class Line
    {
        public string speaker;
        public string text;
    }

    static Dictionary<string, List<Line>> table;

    public static int Count
    {
        get { EnsureLoaded(); return table.Count; }
    }

    public static void Reload()
    {
        table = new Dictionary<string, List<Line>>();

        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogWarning("[Story] 找不到 Resources/" + ResourcePath + ".csv，文案都会走占位提示");
            return;
        }

        Parse(asset.text);
        Debug.Log("[Story] 已载入 " + table.Count + " 段文案");
    }

    public static bool Has(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        EnsureLoaded();
        return table.ContainsKey(id);
    }

    /// <summary>取一段对话的所有句子；没有就返回 null。</summary>
    public static string[] Lines(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EnsureLoaded();

        List<Line> list;
        if (!table.TryGetValue(id, out list) || list == null || list.Count == 0) return null;

        var result = new string[list.Count];
        for (int i = 0; i < list.Count; i++) result[i] = list[i].text;
        return result;
    }

    /// <summary>这段对话的说话人（同一段用第一行的 speaker）。</summary>
    public static string Speaker(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EnsureLoaded();

        List<Line> list;
        if (table.TryGetValue(id, out list) && list.Count > 0) return list[0].speaker;
        return null;
    }

    static void EnsureLoaded()
    {
        if (table == null) Reload();
    }

    // ------------------------------------------------------------------
    // CSV
    // ------------------------------------------------------------------

    static void Parse(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return;

        var rows = SplitRows(csv);
        bool first = true;

        for (int r = 0; r < rows.Count; r++)
        {
            var fields = SplitCsvRow(rows[r]);
            if (fields.Count == 0) continue;

            string id = fields[0].Trim();
            if (first)
            {
                first = false;
                if (id == "id") continue;   // 表头
            }
            if (string.IsNullOrEmpty(id)) continue;

            string speaker = fields.Count > 1 ? fields[1].Trim() : "";
            string text = fields.Count > 2 ? fields[2] : "";
            text = text.Replace("\\n", "\n").Trim();

            // 只写了 id 还没写台词的，当作「这段还没写」，走功能反馈
            if (string.IsNullOrEmpty(text)) continue;

            List<Line> list;
            if (!table.TryGetValue(id, out list))
            {
                list = new List<Line>();
                table[id] = list;
            }
            list.Add(new Line { speaker = speaker, text = text });
        }
    }

    /// <summary>按行切，支持引号里的换行。</summary>
    static List<string> SplitRows(string csv)
    {
        var rows = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                current.Append(c);
                continue;
            }

            if (!inQuotes && (c == '\n' || c == '\r'))
            {
                if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++;
                rows.Add(current.ToString());
                current.Length = 0;
                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0) rows.Add(current.ToString());
        return rows;
    }

    static List<string> SplitCsvRow(string row)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < row.Length; i++)
        {
            char c = row[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < row.Length && row[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (!inQuotes && c == ',')
            {
                fields.Add(current.ToString());
                current.Length = 0;
                continue;
            }

            current.Append(c);
        }

        fields.Add(current.ToString());
        return fields;
    }
}
