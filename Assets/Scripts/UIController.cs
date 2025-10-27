using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] SoftBodyEndlessRunner player;   // kéo thả từ Player
    [SerializeField] TMP_Text distanceText;

    void Awake()
    {
        // fallback nếu quên kéo thả
       
        if (!player) Debug.LogError("[UI] Missing SoftBodyEndlessRunner reference.");
        if (!distanceText) Debug.LogError("[UI] Missing Text reference (UnityEngine.UI.Text).");
    }

    void Update()
    {
        if (!player || !distanceText) return;
        distanceText.text = Mathf.FloorToInt(player.distance) + "0 m";
    }
}
