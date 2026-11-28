using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 不产出任何美术：所有 2D 物体都用 Unity 自带的默认方块精灵（2D Object → Sprites → Square 用的那张），
/// 这里只负责准备中文字体（Windows 自带黑体，不进仓库）。
/// </summary>
public static class WhiteBoxArt
{
    public const string FontDir = "Assets/Fonts";
    public const string FontAssetPath = FontDir + "/SimHei.ttf";

    /// <summary>Unity 自带的默认方块精灵。</summary>
    public static Sprite Square
    {
        get { return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); }
    }

    public static void Prepare()
    {
        if (!AssetDatabase.IsValidFolder(FontDir)) AssetDatabase.CreateFolder("Assets", "Fonts");
        EnsureFont();
        AssetDatabase.Refresh();
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
