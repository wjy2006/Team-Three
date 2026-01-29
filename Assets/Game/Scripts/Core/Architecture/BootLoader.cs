using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    void Start()
    {
        // 游戏启动后，自动进入第一关?
        SceneManager.LoadScene("Room_Lab_PlayerHouse");
    }
}
