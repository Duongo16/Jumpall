using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ContactJumpBooster2D : MonoBehaviour
{
    public KeyCode key = KeyCode.Space;
    public float impulsePerContact = 4f;   // tăng nếu muốn bật mạnh hơn
    public float pressWindow = 0.08f;      // thời gian hợp lệ cho 1 lần nhấn
    public float maxBoostSpeed = 20f;      // giới hạn tốc độ sau boost

    Rigidbody2D rb;
    float pressTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        if (Input.GetKeyDown(key))
            pressTimer = pressWindow;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            pressTimer = pressWindow;
        }
#endif
    }

    void FixedUpdate()
    {
        if (pressTimer > 0f)
            pressTimer -= Time.fixedDeltaTime;
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (pressTimer <= 0f) return;

        int n = col.contactCount;
        for (int i = 0; i < n; i++)
        {
            var cp = col.GetContact(i);

            // Ưu tiên mặt đỡ (normal hướng lên)
            if (cp.normal.y > 0.1f)
            {
                // Đẩy mình theo normal
                rb.AddForce(cp.normal * impulsePerContact, ForceMode2D.Impulse);

                // Nếu đối tượng kia có rigidbody thì đẩy ngược lại
                if (col.rigidbody)
                    col.rigidbody.AddForce(-cp.normal * impulsePerContact, ForceMode2D.Impulse);
            }
        }

        // Clamp tốc độ để không phóng quá đà
        var v = rb.linearVelocity;
        float spd = v.magnitude;
        if (spd > maxBoostSpeed) rb.linearVelocity = v * (maxBoostSpeed / spd);

        pressTimer = 0f; // 1 lần nhấn chỉ kích một lần
    }
}
