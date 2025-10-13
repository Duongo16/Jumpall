using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallRoller2D : MonoBehaviour
{
    public float torque = 12f;          // Độ “đạp” khi giữ A/D (có thể tăng/giảm)
    public float maxAngularVel = 200f;  // Giới hạn quay để không văng

    Rigidbody2D rb;
    float input; // -1..1 (D = -1 để lăn sang phải; A = +1 để lăn sang trái)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Đọc phím ở Update (mượt), vật lý ở FixedUpdate:
        bool left = Input.GetKey(KeyCode.A);
        bool right = Input.GetKey(KeyCode.D);

        input = 0f;
        if (left) input += 1f;   // A → mô-men dương → lăn sang trái
        if (right) input -= 1f;   // D → mô-men âm   → lăn sang phải
    }

    void FixedUpdate()
    {
        if (input != 0f)
        {
            rb.AddTorque(input * torque, ForceMode2D.Force);
        }

        // Giới hạn tốc độ quay để ổn định
        if (Mathf.Abs(rb.angularVelocity) > maxAngularVel)
        {
            rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * maxAngularVel;
        }
    }
}
