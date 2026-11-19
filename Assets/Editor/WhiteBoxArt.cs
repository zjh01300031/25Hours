using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 白盒阶段只用一块白方块。所有物件靠 SpriteRenderer 的颜色区分，
/// 不产出任何美术资源；换成正式美术时，把物件上的 sprite 换掉即可。
/// 中文字体仍从系统复制（Windows 自带黑体），不进仓库。
/// </summary>
public static class WhiteBoxArt
{
    public const string ArtDir = "Assets/Art";
    public const string FontDir = "Assets/Fonts";
    public const string FontAssetPath = FontDir + "/SimHei.ttf";
    public const string BlockName = "wb_block";

    public static void GenerateAll()
    {
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets", "Fonts");
        EnsureFont();

        var block = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        var pixels = new Color32[8 * 8];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        block.SetPixels32(pixels);
        Save(block, BlockName);
    }

    /// <summary>白盒里所有 sprite 都是同一块白方块，颜色由调用方给。</summary>
    public static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + BlockName + ".png");
    }

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

    static void Save(Texture2D texture, string name)
    {
        string path = ArtDir + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }
}
