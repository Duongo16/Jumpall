using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishPoint : MonoBehaviour
{
    private bool canFinish = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            canFinish = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            canFinish = false;
    }

    void UnlockNextLevel()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int unlocked = PlayerPrefs.GetInt("UnlockedLevel", 1);

        // nếu level kế tiếp chưa mở, thì mở nó
        if (unlocked <= currentIndex)
        {
            PlayerPrefs.SetInt("UnlockedLevel", currentIndex + 1);
            PlayerPrefs.Save();
        }
    }

    void Update()
    {
        if (canFinish && Input.GetKeyDown(KeyCode.F))
        {
            UnlockNextLevel();

            // chuyển sang scene kế tiếp
            int nextScene = SceneManager.GetActiveScene().buildIndex + 1;
            SceneManager.LoadScene(nextScene);
        }
    }
}
