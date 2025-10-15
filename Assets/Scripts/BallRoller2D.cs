// BallRoller2D.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallRoller2D : MonoBehaviour
{
    public float torque = 12f;          // A/D để lăn
    public float maxAngularVel = 200f;  // giới hạn quay

    Rigidbody2D rb;
    float input; // -1..1 (D = -1; A = +1)

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void Update()
    {
        bool left = Input.GetKey(KeyCode.A);
        bool right = Input.GetKey(KeyCode.D);

        input = 0f;
        if (left) input += 1f;
        if (right) input -= 1f;
    }

    void FixedUpdate()
    {
        if (input != 0f)
            rb.AddTorque(input * torque, ForceMode2D.Force);

        if (Mathf.Abs(rb.angularVelocity) > maxAngularVel)
            rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * maxAngularVel;
    }
}
