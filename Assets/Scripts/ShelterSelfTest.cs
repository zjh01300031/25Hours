using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// 自动验收：只有命令行带 -selftest 才会跑。
/// 流程：出生 → 跳一下 → 下楼梯到 B1 → 试事件二的门（锁着）→ 进事件一 → 完成回避难所 →
///       事件二的门解锁 → 进事件二 → 完成回避难所。
/// 坐标打到 Player.log（%USERPROFILE%\AppData\LocalLow\Zhao\第25小时\Player.log），
/// 截图存到 D:/Unity2D/Build/25Hours/selftest。平时正常启动什么都不做。
/// </summary>
public class ShelterSelfTest : MonoBehaviour
{
    const string ShotDir = "D:/Unity2D/Build/25Hours/selftest";
    static bool running;

    void Start()
    {
        if (running)
        {
            enabled = false;
            return;
        }

        string[] args = System.Environment.GetCommandLineArgs();
        bool on = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-selftest") on = true;
        }
        if (!on)
        {
            enabled = false;
            return;
        }
        running = true;
        Directory.CreateDirectory(ShotDir);
        // 切场景时自己不能跟着被销毁，不然协程会断（DontDestroyOnLoad 只能对根物体用）
        if (transform.parent != null) transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        var player = FindObjectOfType<ShelterPlayer>();
        var interactor = FindObjectOfType<ShelterInteractor>();

        yield return new WaitForSeconds(1.2f);
        Log("出生", player);
        yield return Shot("01_spawn");

        // 平地跳一下，确认没被地面判定卡住
        {
            float startY = player.transform.position.y;
            player.ForceJump();
            float peak = startY;
            for (int i = 0; i < 70; i++)
            {
                yield return new WaitForFixedUpdate();
                if (player.transform.position.y > peak) peak = player.transform.position.y;
            }
            Debug.Log(string.Format("[自测] 平地跳跃：起跳 y={0:F2}，最高 y={1:F2}（升 {2:F2}），落地 grounded={3}",
                startY, peak, peak - startY, player.IsGrounded));
        }

        // 楼梯斜坡上跳一下
        player.Teleport(new Vector3(-8.5f, -3.2f, 0f));
        yield return new WaitForSeconds(0.9f);
        {
            float startY = player.transform.position.y;
            Debug.Log(string.Format("[自测] 斜坡起跳前：y={0:F2} grounded={1} vy={2:F2}",
                startY, player.IsGrounded, player.Velocity.y));
            player.ForceJump();
            float peak = startY;
            for (int i = 0; i < 80; i++)
            {
                yield return new WaitForFixedUpdate();
                if (player.transform.position.y > peak) peak = player.transform.position.y;
            }
            Debug.Log(string.Format("[自测] 斜坡跳跃：最高 y={0:F2}（升 {1:F2}），结束 grounded={2}",
                peak, peak - startY, player.IsGrounded));
        }

        // 走楼梯下去
        player.Teleport(new Vector3(-4.5f, 1f, 0f));
        yield return new WaitForSeconds(0.6f);
        Log("向左下楼梯", player);
        player.AutoMove = -1f;
        yield return new WaitForSeconds(1.3f);
        yield return Shot("02_stairs");
        yield return new WaitForSeconds(2.2f);
        player.AutoMove = 0f;
        yield return new WaitForSeconds(0.4f);
        Log("到达 B1", player);
        yield return Shot("03_floor-b1");

        // 事件二的门：现在锁着
        player.Teleport(new Vector3(-19.8f, -4.6f, 0f));
        yield return new WaitForSeconds(1.2f);
        Log("事件二门前", player);
        yield return Shot("04_door-02-locked");
        if (interactor != null) interactor.ForceInteract("门·事件二");
        yield return new WaitForSeconds(0.8f);
        yield return Shot("05_door-02-locked-interact");

        // 事件一：进去、完成
        player.Teleport(new Vector3(13.8f, 1f, 0f));
        yield return new WaitForSeconds(1.2f);
        Log("事件一门前", player);
        yield return Shot("06_door-01");

        if (interactor != null) interactor.ForceInteract("门·事件一");
        yield return new WaitForSeconds(2.0f);
        yield return Shot("07_event-01-room");

        var room = FindObjectOfType<EventRoomBase>();
        if (room != null)
        {
            Debug.Log("[自测] 事件一 eventId=" + room.EventId + "，调用 Complete()");
            room.Complete();
        }
        else
        {
            Debug.LogWarning("[自测] 事件场景里没找到 EventRoomBase");
        }
        yield return new WaitForSeconds(2.4f);

        var back = FindObjectOfType<ShelterPlayer>();
        var backInteractor = FindObjectOfType<ShelterInteractor>();
        Log("从事件一回到避难所", back);
        yield return Shot("08_back-from-event-01");

        // 事件二的门：应该解锁了
        if (back != null) back.Teleport(new Vector3(-19.8f, -4.6f, 0f));
        yield return new WaitForSeconds(1.2f);
        yield return Shot("09_door-02-unlocked");

        if (backInteractor != null) backInteractor.ForceInteract("门·事件二");
        yield return new WaitForSeconds(2.0f);
        yield return Shot("10_event-02-room");

        var room2 = FindObjectOfType<EventRoomBase>();
        if (room2 != null)
        {
            Debug.Log("[自测] 事件二 eventId=" + room2.EventId + "，调用 Complete()");
            room2.Complete();
        }
        yield return new WaitForSeconds(2.4f);

        var last = FindObjectOfType<ShelterPlayer>();
        Log("两个事件都完成，回到避难所", last);
        yield return Shot("11_done");

        yield return new WaitForSeconds(0.4f);
        Application.Quit();
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        Destroy(tex);
        string path = Path.Combine(ShotDir, name + ".png");
        File.WriteAllBytes(path, bytes);
        Debug.Log("[自测] 截图 -> " + path);
        yield return new WaitForSeconds(0.3f);
    }

    void Log(string tag, ShelterPlayer p)
    {
        if (p == null)
        {
            Debug.Log("[自测] " + tag + " 玩家不存在");
            return;
        }
        Debug.Log(string.Format("[自测] {0}  position=({1:F2}, {2:F2})  grounded={3}",
            tag, p.transform.position.x, p.transform.position.y, p.IsGrounded));
    }
}
