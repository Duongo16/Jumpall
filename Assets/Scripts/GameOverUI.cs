using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panel;       // Panel chứa Text + 2 nút
    public TMP_Text distanceText;  // “123 m”
    public Button retryButton;
    public Button menuButton;
    public string menuSceneName = "MainMenu";
    public bool pauseOnShow = true;

    static GameOverUI _inst;

    void Awake()
    {
        _inst = this;
        if (panel) panel.SetActive(false);
        if (retryButton) retryButton.onClick.AddListener(OnRetry);
        if (menuButton) menuButton.onClick.AddListener(OnMenu);
    }

    public static void Show(int meters)
    {
        if (_inst == null) return;
        if (_inst.pauseOnShow) Time.timeScale = 0f;

        if (_inst.panel) _inst.panel.SetActive(true);
        if (_inst.distanceText) _inst.distanceText.text = $"<size=90>Game Over!</size>\n\n<size=60>You ran: {meters}0m</size>";
    }

    void OnRetry()
    {
        if (pauseOnShow) Time.timeScale = 1f;
        Scene scn = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scn.name);
    }

    void OnMenu()
    {
        if (pauseOnShow) Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
    }
}
