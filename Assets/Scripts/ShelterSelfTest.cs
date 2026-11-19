using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// 自动验收：只有在命令行带 -selftest 启动时才会跑。
/// 流程：出生 → 走下楼梯到 B1 → 看回声匣（只对话）→ 试事件二的门（锁着）→ 进事件一（事件场景）→
///       完成返回 → 事件二的门解锁 → 进事件二 → 完成返回 → 回 1F 看展柜里的两个人偶。
/// 每步把坐标打到 Player.log（路径：%USERPROFILE%\AppData\LocalLow\Zhao\第25小时\Player.log），
/// 截图存到 D:/Unity2D/Build/25Hours/selftest。平时正常启动这个脚本什么都不做。
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

        yield return new WaitForSeconds(1.5f);
        Log("出生", player);
        yield return Shot("01_出生");

        Log("向左下楼梯", player);
        player.AutoMove = -1f;
        yield return new WaitForSeconds(1.6f);
        yield return Shot("02_下楼梯中");
        yield return new WaitForSeconds(1.8f);
        yield return Shot("03_到达B1");
        Log("B1", player);
        player.AutoMove = 0f;
        yield return new WaitForSeconds(0.5f);

        Log("向左走到B1尽头", player);
        player.AutoMove = -1f;
        yield return new WaitForSeconds(1.0f);
        player.AutoMove = 0f;
        yield return Shot("04_B1中段");
        Log("B1中段", player);

        // ---- 回声匣：只对话，不进事件 ----
        Log("传送到回声匣旁", player);
        player.Teleport(new Vector3(11.3f, 0.8f, 0f));
        yield return new WaitForSeconds(1.4f);
        yield return Shot("05_回声匣旁");

        if (interactor != null) interactor.ForceInteract("回声匣");
        yield return new WaitForSeconds(1.0f);
        yield return Shot("06_回声匣只是对话");
        if (ShelterDialogue.Instance != null) ShelterDialogue.Instance.Close();
        yield return new WaitForSeconds(0.6f);

        // ---- 事件二的门：现在应该锁着 ----
        Log("传送到事件二门前", player);
        player.Teleport(new Vector3(-2.6f, -7.2f, 0f));
        yield return new WaitForSeconds(1.4f);
        yield return Shot("07_事件二的门锁着");

        if (interactor != null) interactor.ForceInteract("事件二");
        yield return new WaitForSeconds(1.0f);
        yield return Shot("08_锁着的门不能进");
        if (ShelterDialogue.Instance != null) ShelterDialogue.Instance.Close();
        yield return new WaitForSeconds(0.6f);

        // ---- 事件一：进去、完成 ----
        Log("传送到事件一门前", player);
        player.Teleport(new Vector3(14.6f, 0.8f, 0f));
        yield return new WaitForSeconds(1.2f);
        yield return Shot("09_事件一门口");

        if (interactor != null) interactor.ForceInteract("事件一");
        yield return new WaitForSeconds(1.0f);
        if (ShelterDialogue.Instance != null) ShelterDialogue.Instance.Close();
        yield return new WaitForSeconds(2.0f);
        yield return Shot("10_事件一场景");

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
        yield return new WaitForSeconds(2.2f);

        var back = FindObjectOfType<ShelterPlayer>();
        var backInteractor = FindObjectOfType<ShelterInteractor>();
        Log("从事件一回到避难所", back);
        yield return Shot("11_完成事件一回到避难所");

        // ---- 事件二的门：应该已经解锁 ----
        if (back != null) back.Teleport(new Vector3(-2.6f, -7.2f, 0f));
        yield return new WaitForSeconds(1.4f);
        yield return Shot("12_事件二的门解锁了");

        if (backInteractor != null) backInteractor.ForceInteract("事件二");
        yield return new WaitForSeconds(1.0f);
        if (ShelterDialogue.Instance != null) ShelterDialogue.Instance.Close();
        yield return new WaitForSeconds(2.0f);
        yield return Shot("13_事件二场景");

        var room2 = FindObjectOfType<EventRoomBase>();
        if (room2 != null)
        {
            Debug.Log("[自测] 事件二 eventId=" + room2.EventId + "，调用 Complete()");
            room2.Complete();
        }
        yield return new WaitForSeconds(2.2f);

        var last = FindObjectOfType<ShelterPlayer>();
        Log("从事件二回到避难所", last);
        yield return Shot("14_两个事件都完成");

        if (last != null) last.Teleport(new Vector3(6.2f, 0.8f, 0f));
        yield return new WaitForSeconds(1.4f);
        yield return Shot("15_展柜两个人偶");

        yield return new WaitForSeconds(0.5f);
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
        yield return new WaitForSeconds(0.4f);
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
