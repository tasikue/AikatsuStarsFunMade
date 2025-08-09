using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public void StartLive()
    {
        Debug.Log("ライブ開始！");
        // 本来は別シーンのライブ画面へ遷移
        // SceneManager.LoadScene("LiveScene");
    }
}
