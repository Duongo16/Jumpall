using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SoftBodyRun))]
public class SoftBodyEndlessRunner : MonoBehaviour
{
    [Header("Run")]
    public float runAcceleration = 60f;
    [Range(0f, 1f)] public float airAccelFactor = 0.25f;

    [Header("Speed ramp")]
    public float startXVelocity = 10f;
    public float speedGrowthPerSec = 0.6f;
    public float hardCap = 0f;

    [Header("Hold Jump (variable height)")]
    public float maxHoldJumpTime = 0.35f;
    public float holdUpForce = 40f;
    public float nearGroundExtra = 0.10f;

    [Header("Distance")]
    public float distance = 0f;
    float lastX;

    [Header("Air Gravity Swap")]
    public float jumpGravity = 10f;       // khi bay, xa đất
    public float nearGroundGravity = 1f;  // khi gần đất <= 0.5 hoặc đã chạm
    public float nearGroundDist = 0.5f;   // ngưỡng "gần đất"


    [Header("Box effects")]
    public float gravityDeltaOverride = 0f;

    Rigidbody2D rb;
    SoftBodyRun sb;
    bool isHolding;
    float holdT, runTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sb = GetComponent<SoftBodyRun>();
        lastX = rb.position.x;
        rb.gravityScale = nearGroundGravity; // mặc định khi đang đứng đất
    }

    bool IsGrounded()
    {
        var hit = Physics2D.Raycast(rb.position, Vector2.down,
            sb.groundCheckDist + nearGroundExtra, sb.groundMask);
        return hit.collider != null;
    }

    // Trả về khoảng cách tới đất, -1 nếu không thấy trong tầm check
    float GroundDistance(float maxDist)
    {
        var hit = Physics2D.Raycast(rb.position, Vector2.down, maxDist, sb.groundMask);
        return hit.collider ? hit.distance : -1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded()) { isHolding = true; holdT = 0f; }
        if (Input.GetKeyUp(KeyCode.Space)) isHolding = false;
    }

    void FixedUpdate()
    {
        runTime += Time.fixedDeltaTime;

        bool grounded = IsGrounded();
        float accel = grounded ? runAcceleration : runAcceleration * airAccelFactor;

        float targetVx = startXVelocity + speedGrowthPerSec * runTime;
        if (hardCap > 0f) targetVx = Mathf.Min(targetVx, hardCap);

        var v = rb.linearVelocity;
        v.x = Mathf.MoveTowards(v.x, targetVx, accel * Time.fixedDeltaTime);
        rb.linearVelocity = v;

        // Distance: cộng dương theo dịch chuyển X
        float dx = rb.position.x - lastX;
        if (dx > 0f) distance += dx;
        lastX = rb.position.x;

        // Giữ nhảy như cũ
        if (isHolding && holdT < maxHoldJumpTime)
        {
            rb.AddForce(Vector2.up * holdUpForce, ForceMode2D.Force);
            holdT += Time.fixedDeltaTime;
        }
        else isHolding = false;

        // Gravity: xa đất => 10, gần đất (<=0.5) hoặc grounded => 1
        if (grounded)
            rb.gravityScale = Mathf.Max(0.1f, nearGroundGravity + gravityDeltaOverride);
        else
        {
            float d = GroundDistance(sb.groundCheckDist + nearGroundExtra + nearGroundDist);
            if (d >= 0f && d <= nearGroundDist)
                rb.gravityScale = Mathf.Max(0.1f, nearGroundGravity + gravityDeltaOverride);
            else
                rb.gravityScale = Mathf.Max(0.1f, jumpGravity + gravityDeltaOverride);
        }
    }
}
