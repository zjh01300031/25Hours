using UnityEngine;

/// <summary>
/// 音量设置。现在工程里还没有音频，先把数值存起来，
/// 以后加 BGM / 音效时读这里的两个值就行。
/// </summary>
public static class GameAudio
{
    const string MusicKey = "vol.music";
    const string SfxKey = "vol.sfx";

    public const float DefaultVolume = 0.8f;

    public static float MusicVolume { get; private set; } = DefaultVolume;
    public static float SfxVolume { get; private set; } = DefaultVolume;

    /// <summary>从 PlayerPrefs 读回上次的音量，进主菜单时调一次。</summary>
    public static void Load()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicKey, DefaultVolume);
        SfxVolume = PlayerPrefs.GetFloat(SfxKey, DefaultVolume);
    }

    public static void SetMusic(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        PlayerPrefs.Save();
    }

    public static void SetSfx(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        PlayerPrefs.Save();
    }

    /// <summary>以后有音频源时，把音量套上去（没有就什么都不做）。</summary>
    public static void ApplyTo(AudioSource music, AudioSource sfx)
    {
        if (music != null) music.volume = MusicVolume;
        if (sfx != null) sfx.volume = SfxVolume;
    }
}
