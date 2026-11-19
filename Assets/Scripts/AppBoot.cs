using System;
using UnityEngine;

/// <summary>
/// 启动时把帧率/物理步长统一一下：
/// - 物理步长从默认 0.02（50Hz）改成 1/60，和 60 帧对齐，避免走/跳时出现细微顿感
/// - 垂直同步跟随显示器，不额外限帧
/// 命令行带 -fps 时每秒往日志里打一行帧率（含最差一帧），方便现场确认卡不卡。
/// </summary>
public class AppBoot : MonoBehaviour
{
    bool logFps;
    float timer;
    int frames;
    float worst;

    void Awake()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;
        Time.fixedDeltaTime = 1f / 60f;

        logFps = Array.IndexOf(Environment.GetCommandLineArgs(), "-fps") >= 0;
    }

    void Update()
    {
        if (!logFps) return;

        frames++;
        float dt = Time.unscaledDeltaTime;
        if (dt > worst) worst = dt;

        timer += dt;
        if (timer >= 1f)
        {
            Debug.Log(string.Format("[FPS] {0:0.0} fps，最差一帧 {1:0.0} ms",
                frames / timer, worst * 1000f));
            frames = 0;
            timer = 0f;
            worst = 0f;
        }
    }
}
