using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 白盒阶段的占位图：全部用代码画成 PNG（纯色块 + 一点点边线），不需要外部素材。
/// 想换美术：直接把 Assets/Art 里的同名 png 覆盖成你自己的图即可，场景不用改。
/// </summary>
public static class WhiteBoxArt
{
    public const string ArtDir = "Assets/Art";
    public const string FontDir = "Assets/Fonts";
    public const string FontAssetPath = FontDir + "/SimHei.ttf";

    public static void GenerateAll()
    {
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets", "Fonts");
        EnsureFont();

        // 建筑
        var light = NewTex(16, 16);
        Fill(light, new Color32(198, 201, 208, 255));
        Edge(light, new Color32(226, 229, 235, 255), new Color32(150, 153, 160, 255));
        Save(light, "block_light");

        var mid = NewTex(16, 16);
        Fill(mid, new Color32(148, 152, 162, 255));
        Edge(mid, new Color32(174, 178, 188, 255), new Color32(108, 112, 122, 255));
        Save(mid, "block_mid");

        var dark = NewTex(16, 16);
        Fill(dark, new Color32(78, 82, 92, 255));
        Edge(dark, new Color32(102, 106, 116, 255), new Color32(52, 55, 63, 255));
        Save(dark, "block_dark");

        // 楼梯的实心部分：比 block_dark 再沉一点，免得大三角形抢画面
        var stairBlock = NewTex(16, 16);
        Fill(stairBlock, new Color32(60, 64, 72, 255));
        Edge(stairBlock, new Color32(84, 88, 96, 255), new Color32(42, 45, 51, 255));
        Save(stairBlock, "block_stair");

        var black = NewTex(16, 16);
        Fill(black, new Color32(18, 20, 24, 255));
        Save(black, "block_black");

        var wall = NewTex(16, 16);
        Fill(wall, new Color32(43, 47, 55, 255));
        Box(wall, 0, 8, 16, 1, new Color32(38, 42, 49, 255));
        Save(wall, "back_wall_1f");

        var wallWarm = NewTex(16, 16);
        Fill(wallWarm, new Color32(58, 51, 43, 255));
        Box(wallWarm, 0, 8, 16, 1, new Color32(51, 45, 38, 255));
        Save(wallWarm, "back_wall_1f_warm");

        var wallB1 = NewTex(16, 16);
        Fill(wallB1, new Color32(46, 50, 58, 255));
        Box(wallB1, 0, 8, 16, 1, new Color32(40, 44, 51, 255));
        Save(wallB1, "back_wall_b1");

        var wallStair = NewTex(16, 16);
        Fill(wallStair, new Color32(30, 33, 40, 255));
        Box(wallStair, 0, 8, 16, 1, new Color32(26, 29, 35, 255));
        Save(wallStair, "back_wall_stair");

        // 房间以外的「泥土/结构」：深但不是纯黑，免得镜头拍到外面像个黑洞
        var earth = NewTex(32, 32);
        Fill(earth, new Color32(16, 18, 23, 255));
        Box(earth, 0, 6, 32, 1, new Color32(20, 22, 28, 255));
        Box(earth, 0, 17, 32, 1, new Color32(20, 22, 28, 255));
        Box(earth, 0, 26, 32, 1, new Color32(20, 22, 28, 255));
        Save(earth, "back_wall_earth");

        // 角色
        var player = NewTex(16, 24);
        Box(player, 4, 0, 3, 5, new Color32(118, 34, 34, 255));
        Box(player, 9, 0, 3, 5, new Color32(118, 34, 34, 255));
        Box(player, 4, 4, 8, 11, new Color32(196, 56, 56, 255));
        Box(player, 3, 6, 1, 6, new Color32(170, 44, 44, 255));
        Box(player, 12, 6, 1, 6, new Color32(170, 44, 44, 255));
        Box(player, 3, 14, 10, 9, new Color32(214, 66, 66, 255));
        Box(player, 5, 18, 2, 2, new Color32(248, 244, 236, 255));
        Box(player, 9, 18, 2, 2, new Color32(248, 244, 236, 255));
        Box(player, 4, 13, 8, 1, new Color32(238, 208, 126, 255));
        Save(player, "player_red");

        var npc = NewTex(16, 24);
        Box(npc, 5, 0, 2, 5, new Color32(62, 104, 166, 255));
        Box(npc, 9, 0, 2, 5, new Color32(62, 104, 166, 255));
        Box(npc, 4, 4, 8, 12, new Color32(86, 138, 206, 255));
        Box(npc, 4, 15, 8, 8, new Color32(122, 174, 232, 255));
        Box(npc, 6, 18, 2, 2, new Color32(248, 244, 236, 255));
        Box(npc, 9, 18, 2, 2, new Color32(248, 244, 236, 255));
        Box(npc, 7, 5, 1, 9, new Color32(64, 106, 164, 255));
        Save(npc, "npc_doll");

        // 道具
        var item = NewTex(16, 16);
        Box(item, 2, 2, 12, 12, new Color32(236, 184, 70, 255));
        Box(item, 1, 4, 14, 8, new Color32(236, 184, 70, 255));
        Box(item, 4, 1, 8, 14, new Color32(236, 184, 70, 255));
        Box(item, 5, 6, 6, 5, new Color32(255, 226, 152, 255));
        Box(item, 7, 3, 2, 3, new Color32(150, 104, 36, 255));
        Save(item, "item_glow");

        var crate = NewTex(16, 16);
        Fill(crate, new Color32(134, 120, 98, 255));
        for (int i = 2; i < 14; i++)
        {
            Box(crate, i, i, 1, 1, new Color32(108, 96, 78, 255));
            Box(crate, i, 15 - i, 1, 1, new Color32(108, 96, 78, 255));
        }
        Box(crate, 0, 0, 16, 2, new Color32(102, 90, 72, 255));
        Box(crate, 0, 14, 16, 2, new Color32(102, 90, 72, 255));
        Box(crate, 0, 0, 2, 16, new Color32(102, 90, 72, 255));
        Box(crate, 14, 0, 2, 16, new Color32(102, 90, 72, 255));
        Save(crate, "crate");

        var cabinet = NewTex(16, 24);
        Fill(cabinet, new Color32(120, 108, 90, 255));
        Box(cabinet, 0, 0, 16, 2, new Color32(92, 82, 66, 255));
        Box(cabinet, 0, 22, 16, 2, new Color32(92, 82, 66, 255));
        Box(cabinet, 0, 0, 2, 24, new Color32(92, 82, 66, 255));
        Box(cabinet, 14, 0, 2, 24, new Color32(92, 82, 66, 255));
        Box(cabinet, 7, 2, 1, 20, new Color32(96, 86, 70, 255));
        Box(cabinet, 4, 12, 1, 3, new Color32(70, 62, 50, 255));
        Box(cabinet, 11, 12, 1, 3, new Color32(70, 62, 50, 255));
        Save(cabinet, "cabinet");

        var display = NewTex(24, 32);
        Fill(display, new Color32(62, 70, 82, 255));
        Box(display, 2, 6, 20, 22, new Color32(132, 178, 198, 70));
        Box(display, 3, 20, 3, 6, new Color32(230, 244, 250, 70));
        Box(display, 0, 0, 24, 6, new Color32(84, 92, 106, 255));
        Box(display, 0, 27, 24, 5, new Color32(84, 92, 106, 255));
        Box(display, 0, 0, 2, 32, new Color32(46, 52, 62, 255));
        Box(display, 22, 0, 2, 32, new Color32(46, 52, 62, 255));
        Save(display, "display_case");

        var door = NewTex(24, 32);
        Fill(door, new Color32(42, 46, 54, 255));
        Box(door, 0, 0, 24, 2, new Color32(28, 31, 37, 255));
        Box(door, 0, 30, 24, 2, new Color32(28, 31, 37, 255));
        Box(door, 0, 0, 2, 32, new Color32(28, 31, 37, 255));
        Box(door, 22, 0, 2, 32, new Color32(28, 31, 37, 255));
        Box(door, 11, 2, 2, 28, new Color32(54, 58, 66, 255));
        Box(door, 9, 15, 6, 5, new Color32(170, 70, 70, 255));
        Save(door, "door_locked");

        // 打开的事件门：门后是暖光
        var doorEvent = NewTex(24, 32);
        Fill(doorEvent, new Color32(42, 45, 52, 255));
        Box(doorEvent, 4, 4, 16, 24, new Color32(104, 90, 68, 255));
        Box(doorEvent, 11, 5, 3, 22, new Color32(238, 208, 148, 255));
        Box(doorEvent, 4, 4, 16, 3, new Color32(180, 152, 106, 255));
        Box(doorEvent, 0, 0, 24, 4, new Color32(78, 82, 90, 255));
        Box(doorEvent, 0, 28, 24, 4, new Color32(78, 82, 90, 255));
        Box(doorEvent, 0, 0, 4, 32, new Color32(78, 82, 90, 255));
        Box(doorEvent, 20, 0, 4, 32, new Color32(78, 82, 90, 255));
        Save(doorEvent, "door_event");

        var lampOff = NewTex(16, 8);
        Box(lampOff, 1, 4, 14, 3, new Color32(92, 96, 106, 255));
        Box(lampOff, 5, 0, 6, 4, new Color32(150, 152, 158, 255));
        Save(lampOff, "lamp_off");

        var lampOn = NewTex(16, 8);
        Box(lampOn, 1, 4, 14, 3, new Color32(120, 118, 106, 255));
        Box(lampOn, 5, 0, 6, 4, new Color32(255, 214, 128, 255));
        Save(lampOn, "lamp_on");

        // 光晕 / UI
        var glow = NewTex(16, 16);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                float dx = (x - 7.5f) / 8f;
                float dy = (y - 7.5f) / 8f;
                float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                float a = d * d;
                glow.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        Save(glow, "glow");

        // 挂钟：指针停在「第 25 小时」（1 点位置）
        var clock = NewTex(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = (x - 15.5f) / 12f;
                float dy = (y - 15.5f) / 12f;
                float d = dx * dx + dy * dy;
                if (d <= 1f) clock.SetPixel(x, y, new Color32(92, 97, 108, 255));
                else if (d <= 1.28f) clock.SetPixel(x, y, new Color32(158, 163, 174, 255));
            }
        }
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            int tx = Mathf.RoundToInt(15.5f + Mathf.Cos(a) * 9.5f);
            int ty = Mathf.RoundToInt(15.5f + Mathf.Sin(a) * 9.5f);
            if (i % 3 == 0) Box(clock, tx - 1, ty - 1, 3, 3, new Color32(226, 230, 238, 255));
            else Box(clock, tx, ty, 2, 2, new Color32(186, 191, 202, 255));
        }
        // 时针指向 1 点（第 25 小时），分针指向 12
        Line(clock, 16, 16, 21, 21, new Color32(40, 43, 50, 255));
        Line(clock, 16, 16, 16, 6, new Color32(40, 43, 50, 255));
        Line(clock, 16, 16, 17, 20, new Color32(40, 43, 50, 255));
        // 一道裂纹
        Line(clock, 23, 25, 27, 20, new Color32(60, 64, 72, 255));
        Box(clock, 15, 15, 3, 3, new Color32(30, 32, 38, 255));
        Save(clock, "clock_face");

        var white = NewTex(8, 8);
        Fill(white, new Color32(255, 255, 255, 255));
        Save(white, "ui_white");

        var panel = NewTex(32, 32);
        Fill(panel, new Color32(12, 15, 20, 238));
        Box(panel, 0, 0, 32, 2, new Color32(66, 73, 88, 255));
        Box(panel, 0, 30, 32, 2, new Color32(66, 73, 88, 255));
        Box(panel, 0, 0, 2, 32, new Color32(66, 73, 88, 255));
        Box(panel, 30, 0, 2, 32, new Color32(66, 73, 88, 255));
        Save(panel, "ui_panel", 16, new Vector4(10f, 10f, 10f, 10f));

        AssetDatabase.Refresh();
        Debug.Log("[WhiteBoxArt] 占位图生成完毕（" + ArtDir + "）");
    }

    // ---------------- 工具 ----------------

    static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    static void EnsureFont()
    {
        if (AssetDatabase.LoadAssetAtPath<Font>(FontAssetPath) != null) return;

        const string systemFont = @"C:\Windows\Fonts\simhei.ttf";
        if (!File.Exists(systemFont))
        {
            Debug.LogError("[WhiteBoxArt] 没找到系统黑体：" + systemFont + "，中文会显示成方块。请把一个中文字体放到 " + FontAssetPath);
            return;
        }

        File.Copy(systemFont, FontAssetPath, true);
        AssetDatabase.ImportAsset(FontAssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[WhiteBoxArt] 已导入中文字体：" + FontAssetPath);
    }

    public static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + name + ".png");
    }

    static Texture2D NewTex(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        var clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        texture.SetPixels32(pixels);
        return texture;
    }

    static void Fill(Texture2D texture, Color32 color)
    {
        Box(texture, 0, 0, texture.width, texture.height, color);
    }

    static void Box(Texture2D texture, int x, int y, int width, int height, Color32 color)
    {
        for (int i = x; i < x + width; i++)
        {
            for (int j = y; j < y + height; j++)
            {
                if (i >= 0 && j >= 0 && i < texture.width && j < texture.height)
                {
                    texture.SetPixel(i, j, color);
                }
            }
        }
    }

    static void Line(Texture2D texture, int x0, int y0, int x1, int y1, Color32 color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            texture.SetPixel(x0, y0, color);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    static void Edge(Texture2D texture, Color32 top, Color32 bottom)    {
        Box(texture, 0, texture.height - 2, texture.width, 2, top);
        Box(texture, 0, 0, texture.width, 1, bottom);
    }

    static void Save(Texture2D texture, string name, int pixelsPerUnit = 16, Vector4 border = default, bool fullRect = true)
    {
        string path = ArtDir + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = fullRect ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
        settings.spriteBorder = border;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }
}
