using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;


public class MainMenu : MonoBehaviour
{

    public Button playButton;
    public Button exitButton;

    public GameObject endlessBtn;
    public GameObject puzzleBtn;

    public RectTransform endlessBtnRect;
    public RectTransform puzzleBtnRect;

    void Start()
    {
        endlessBtn.SetActive(false);
        puzzleBtn.SetActive(false);

        playButton.onClick.AddListener(OnPlayClicked);
        exitButton.onClick.AddListener(OnExitClicked);
    }

    void OnPlayClicked()
    {
        playButton.gameObject.SetActive(false); // ẩn nút Play
        exitButton.gameObject.SetActive(false);
        endlessBtn.SetActive(true); // hiện Endless
        puzzleBtn.SetActive(true);  // hiện Puzzle

        // đặt vị trí ban đầu thấp hơn một chút 
        endlessBtnRect.anchoredPosition = new Vector2(-377, -200);
        puzzleBtnRect.anchoredPosition = new Vector2(377, -200);

        // hiệu ứng chạy từ dưới lên
        endlessBtnRect.DOAnchorPosY(0, 1f).SetEase(Ease.OutBack);
        puzzleBtnRect.DOAnchorPosY(0, 1f).SetEase(Ease.OutBack).SetDelay(0.1f);
    }

    void OnExitClicked()
    {
        Application.Quit(); //thoát game thật khi build
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; //dừng khi đang chạy trong Editor
#endif
    }

    //public void PlayGamePuzzle()
    //{
    //    SceneManager.LoadSceneAsync(1);
    //}

    public void PlayGameEndless()
    {
        SceneManager.LoadSceneAsync("EndlessMap");
    }
}
