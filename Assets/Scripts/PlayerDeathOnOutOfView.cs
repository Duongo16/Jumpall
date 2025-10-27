using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDeathOnOutOfView : MonoBehaviour
{
    public Camera cam;                 // để trống sẽ tự lấy MainCamera
    public float deathDelay = 3f;      // rơi 3s rồi mới chết
    public bool onlyBelow = true;      // chỉ tính khi rơi xuống đáy

    bool dying;
    Rigidbody2D rb;
    SoftBodyEndlessRunner runner;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        runner = GetComponent<SoftBodyEndlessRunner>() ?? GetComponentInParent<SoftBodyEndlessRunner>();
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        if (dying || !cam) return;

        Vector3 vp = cam.WorldToViewportPoint(transform.position);
        bool outView = onlyBelow ? (vp.y < 0f) : (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f);
        if (outView) StartCoroutine(DieAfterDelay());
    }

    IEnumerator DieAfterDelay()
    {
        dying = true;
        yield return new WaitForSeconds(deathDelay);

        int meters = runner ? Mathf.FloorToInt(runner.distance) : 0;
        GameOverUI.Show(meters);

        Destroy(gameObject);
    }
}
