using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform followTarget;
    public float xOffset = 20f;   // giữ player sát mép trái bằng cách đặt âm/duong tùy layout
    public float yOffset = 1f;

    [Header("Smoothing")]
    public float ySmooth = 8f;    // 0 = khóa cứng Y; 6–12 là mượt
    public bool lockX = true;     // khóa cứng X để không “kéo lùi”
    public float xSmooth = 12f;   // dùng khi lockX = false
    public float lookAheadTime = 0.12f;

    Rigidbody2D targetRb;
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (followTarget) targetRb = followTarget.GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (!followTarget) return;

        Vector3 pos = transform.position;

        // --- X: khóa cứng để không tụt lại khi nhảy ---
        float targetX = followTarget.position.x + xOffset;

        if (lockX)
        {
            pos.x = targetX;
        }
        else
        {
            float lead = (targetRb ? targetRb.linearVelocity.x : 0f) * lookAheadTime;
            float ax = 1f - Mathf.Exp(-xSmooth * Time.deltaTime);
            pos.x = Mathf.Lerp(pos.x, targetX + lead, ax);
        }

        // --- Y: làm mượt để hết giật ---
        float targetY = followTarget.position.y + yOffset;
        float ay = 1f - Mathf.Exp(-ySmooth * Time.deltaTime);
        pos.y = Mathf.Lerp(pos.y, targetY, ay);

        pos.z = -10f;
        transform.position = pos;
    }
}
