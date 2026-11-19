using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 白盒阶段不产出任何美术：场景里所有东西都用同一块纯白方块（一张 8×8 白贴图），
/// 靠 SpriteRenderer.color 区分。这里只负责准备这块白方块和中文字体（Windows 自带黑体，不进仓库）。
/// </summary>
public static class WhiteBoxArt
{
    public const string FontDir = "Assets/Fonts";
    public const string FontAssetPath = FontDir + "/SimHei.ttf";
    public const string SquareDir = "Assets/Whitebox";
    public const string SquareAssetPath = SquareDir + "/white.png";

    static Sprite square;

    /// <summary>纯白方块精灵，全场景共用。</summary>
    public static Sprite Square
    {
        get
        {
            if (square == null)
            {
                square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareAssetPath);
            }
            return square;
        }
    }

    public static void Prepare()
    {
        if (!AssetDatabase.IsValidFolder(FontDir)) AssetDatabase.CreateFolder("Assets", "Fonts");
        if (!AssetDatabase.IsValidFolder(SquareDir)) AssetDatabase.CreateFolder("Assets", "Whitebox");

        EnsureSquare();
        EnsureFont();
        AssetDatabase.Refresh();
        square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareAssetPath);
    }

    static void EnsureSquare()
    {
        if (File.Exists(SquareAssetPath)) return;

        // 8×8 纯白，只是「一块白色」，不是美术素材
        var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        var pixels = new Color32[8 * 8];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(SquareAssetPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(SquareAssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        var importer = AssetImporter.GetAtPath(SquareAssetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 8f;      // 8px → 1 个世界单位
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        Debug.Log("[WhiteBoxArt] 已生成白方块贴图：" + SquareAssetPath);
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
}
