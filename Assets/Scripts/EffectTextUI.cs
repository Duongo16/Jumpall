// Assets/Scripts/UI/EffectTextUI.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class EffectTextUI : MonoBehaviour
{
    public static EffectTextUI Instance { get; private set; }
    [SerializeField] TMP_Text effectText;
    [SerializeField] float defaultShowTime = 0.5f;
    Coroutine co;

    void Reset() { effectText = GetComponent<TMP_Text>(); }
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!effectText) effectText = GetComponent<TMP_Text>();
        effectText.text = ""; effectText.gameObject.SetActive(false);
    }
    public void Flash(string msg, float seconds = -1f)
    {
        if (!effectText) return;
        if (seconds <= 0f) seconds = defaultShowTime;
        if (co != null) StopCoroutine(co);
        effectText.gameObject.SetActive(true);
        effectText.text = msg;
        co = StartCoroutine(HideAfter(seconds));
    }
    IEnumerator HideAfter(float s)
    {
        yield return new WaitForSeconds(s);
        effectText.text = ""; effectText.gameObject.SetActive(false);
        co = null;
    }
}
