// SoftBodyBuilder2D.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SoftBodyBuilder2D : MonoBehaviour
{
    [Header("Point prefab & shape")]
    public GameObject pointPrefab;
    [Range(3, 64)] public int pointCount = 20;
    public float radius = 0.9f;

    [Header("Rim springs (neighbors)")]
    public bool linkPrevAndNext = false;
    public float neighborFrequency = 8f;
    [Range(0f, 1f)] public float neighborDamping = 0.7f;
    public bool enableNeighborCollision = false;

    [Header("Center springs (to center)")]
    public float centerFrequency = 10f;
    [Range(0f, 1f)] public float centerDamping = 0.8f;
    public bool enableCenterCollision = false;

    [Header("Hold Space = INFLATE")]
    public KeyCode inflateKey = KeyCode.Space;
    [Range(1.05f, 3f)] public float holdInflateMul = 3f;
    public float inflateLerpSpeed = 16f;
    public float outwardPressure = 200f;
    public float upwardPush = 12f;
    public float groundedBoost = 1.6f;
    public float maxCenterSpeed = 20f;   // 0 = infinite

    [Header("Jump impulse on press")]
    public float jumpImpulse = 10f;
    public float perPointImpulse = 1.2f;
    public float pointProbeRadius = 0.08f;

    [Header("Move (A/D)")]
    public float moveForce = 14f;

    [Header("Grounding")]
    public LayerMask groundMask;
    public float groundCheckDist = 0.19f;

    // ==== Anti-splat (shape memory) ====
    [Header("Anti-Splat / Shape Memory")]
    public bool enableShapeMemory = true;
    public float shapeStiffness = 140f;      // k
    public float shapeDamping = 6f;          // c
    public float shapeForceClamp = 500f;
    public bool enableAntiShear = true;
    public float antiShearDamping = 3.5f;
    public float groundedShapeBoost = 1.4f;

    [Header("Jump boost (grounded points + compression)")]
    public float jumpPerGroundedPoint = 0.45f;
    public float compressionBoostK = 50f;
    public float compressionBoostCap = 12f;

    Rigidbody2D centerRb;
    readonly List<Rigidbody2D> pointRBs = new();
    readonly List<SpringJoint2D> rimSprings = new();
    readonly List<SpringJoint2D> centerSprings = new();

    float baseRimDistance;
    readonly List<float> baseCenterDistances = new();

    // Hướng local gốc (đơn vị) của từng point so với tâm tại lúc spawn
    readonly List<Vector2> baseLocalDirs = new();

    // Trạng thái phình hiện tại dùng chung cho springs và anti-splat target
    float inflateFactor = 1f;

    AudioManager audioManager;

    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }
    void Start()
    {
        centerRb = GetComponent<Rigidbody2D>();
        BuildRing();
    }

    void BuildRing()
    {
        if (!pointPrefab) { Debug.LogError("[SoftBody] Missing Point Prefab"); return; }

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        pointRBs.Clear(); rimSprings.Clear(); centerSprings.Clear();
        baseCenterDistances.Clear(); baseLocalDirs.Clear();

        // Spawn points in circle + ghi nhớ hướng local gốc
        for (int i = 0; i < pointCount; i++)
        {
            float ang = i * Mathf.PI * 2f / pointCount;
            Vector2 localDir = new(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 pos = (Vector2)transform.position + localDir * radius;

            var p = Instantiate(pointPrefab, pos, Quaternion.identity, transform);
            var rb = p.GetComponent<Rigidbody2D>() ?? p.AddComponent<Rigidbody2D>();
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            pointRBs.Add(rb);
            baseLocalDirs.Add(localDir); // lưu localDir đơn vị
        }

        // Rim springs
        for (int i = 0; i < pointRBs.Count; i++)
        {
            int next = (i + 1) % pointRBs.Count;
            int prev = (i - 1 + pointRBs.Count) % pointRBs.Count;

            CreateRimSpring(pointRBs[i], pointRBs[next]);
            if (linkPrevAndNext) CreateRimSpring(pointRBs[i], pointRBs[prev]);

            // Center springs cho mỗi point
            var c = pointRBs[i].gameObject.AddComponent<SpringJoint2D>();
            c.connectedBody = centerRb;
            c.enableCollision = enableCenterCollision;
            c.frequency = centerFrequency;
            c.dampingRatio = centerDamping;
            c.autoConfigureDistance = false;

            float restC = Vector2.Distance(pointRBs[i].position, (Vector2)transform.position);
            c.distance = restC;

            centerSprings.Add(c);
            baseCenterDistances.Add(restC);
        }

        if (rimSprings.Count > 0) baseRimDistance = rimSprings[0].distance;

        centerRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        centerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        inflateFactor = 1f;
    }

    void CreateRimSpring(Rigidbody2D a, Rigidbody2D b)
    {
        var sj = a.gameObject.AddComponent<SpringJoint2D>();
        sj.connectedBody = b;
        sj.enableCollision = enableNeighborCollision;
        sj.frequency = neighborFrequency;
        sj.dampingRatio = neighborDamping;
        sj.autoConfigureDistance = false;
        sj.distance = Vector2.Distance(a.position, b.position);
        rimSprings.Add(sj);
    }

    bool HoldingSpace()
    {
        bool pressed = Input.GetKey(inflateKey);
#if ENABLE_INPUT_SYSTEM
        if (!pressed && UnityEngine.InputSystem.Keyboard.current != null)
            pressed = UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed;
#endif
        return pressed;
    }

    // đặt chung trong SoftBodyBuilder2D
    void PlayJumpSFX()
    {
        if (audioManager != null)
            audioManager.PlaySFX(audioManager.Jump);
    }

    bool PressedSpaceThisFrame()
    {
        bool down = Input.GetKeyDown(inflateKey);
#if ENABLE_INPUT_SYSTEM
        if (!down && UnityEngine.InputSystem.Keyboard.current != null)
            down = UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
#endif
        if (down) PlayJumpSFX();   // <-- phát âm thanh đúng khoảnh khắc nhấn
        return down;
    }


    bool IsGrounded()
    {
        Vector2 origin = centerRb.position;
        var hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDist, groundMask);
        return hit.collider != null;
    }

    int CountGroundedPoints()
    {
        int cnt = 0;
        for (int i = 0; i < pointRBs.Count; i++)
            if (Physics2D.OverlapCircle(pointRBs[i].position, pointProbeRadius, groundMask))
                cnt++;
        return cnt;
    }

    // 0..~1: mức “xẹp” so với hình mục tiêu ở khung hiện tại
    float ComputeCompression()
    {
        float sum = 0f;
        int n = pointRBs.Count;
        Vector2 c = centerRb.position;
        for (int i = 0; i < n; i++)
        {
            float target = baseCenterDistances[i] * inflateFactor;
            float actual = Vector2.Distance(pointRBs[i].position, c);
            if (actual < target && target > 1e-3f)
                sum += (target - actual) / target; // tỉ lệ thiếu bán kính
        }
        return sum / n;
    }

    void Update()
    {
        bool inflating = HoldingSpace();

        // 1) Cập nhật inflateFactor, đồng bộ cho springs
        float targetMul = inflating ? holdInflateMul : 1f;
        float lerpSpd = Time.deltaTime * inflateLerpSpeed;
        inflateFactor = Mathf.Lerp(inflateFactor, targetMul, lerpSpd);

        float rimTarget = baseRimDistance * inflateFactor;
        foreach (var sj in rimSprings)
            sj.distance = Mathf.Lerp(sj.distance, rimTarget, lerpSpd);

        for (int i = 0; i < centerSprings.Count; i++)
        {
            float target = baseCenterDistances[i] * inflateFactor;
            var sj = centerSprings[i];
            sj.distance = Mathf.Lerp(sj.distance, target, lerpSpd);
        }

        // 2) Impulse nhảy khi NHẤN Space
        if (PressedSpaceThisFrame() && IsGrounded())
        {
            int groundedPts = CountGroundedPoints();
            float comp = ComputeCompression(); // 0..1
            float extra = groundedPts * jumpPerGroundedPoint
                        + Mathf.Min(comp * compressionBoostK, compressionBoostCap);

            centerRb.AddForce(Vector2.up * (jumpImpulse + extra), ForceMode2D.Impulse);

            // giữ kick riêng cho các point đang chạm
            for (int i = 0; i < pointRBs.Count; i++)
            {
                var prb = pointRBs[i];
                if (Physics2D.OverlapCircle(prb.position, pointProbeRadius, groundMask))
                    prb.AddForce(Vector2.up * perPointImpulse, ForceMode2D.Impulse);
            }
        }

        // 3) Áp suất + đẩy tâm lên khi GIỮ Space
        if (inflating)
        {
            float boost = IsGrounded() ? groundedBoost : 1f;

            foreach (var rb in pointRBs)
            {
                Vector2 dir = (rb.position - centerRb.position);
                float len = dir.magnitude;
                if (len > 1e-3f) dir /= len;
                rb.AddForce(dir * outwardPressure * boost * Time.deltaTime, ForceMode2D.Force);
            }

            centerRb.AddForce(Vector2.up * upwardPush * boost * Time.deltaTime, ForceMode2D.Force);
        }

        // 4) Clamp tốc độ tâm
        if (maxCenterSpeed > 0f)
        {
            var v = centerRb.linearVelocity;
            float spd = v.magnitude;
            if (spd > maxCenterSpeed) centerRb.linearVelocity = v * (maxCenterSpeed / spd);
        }

        // 5) Move A/D
        float h = 0f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;
#if ENABLE_INPUT_SYSTEM
        var kbd = UnityEngine.InputSystem.Keyboard.current;
        if (kbd != null)
        {
            if (kbd.aKey.isPressed) h = -1f;
            if (kbd.dKey.isPressed) h = 1f;
        }
#endif
        if (Mathf.Abs(h) > 0f)
        {
            Vector2 f = new(h * moveForce, 0f);
            foreach (var rb in pointRBs) rb.AddForce(f, ForceMode2D.Force);
        }
    }

    void FixedUpdate()
    {
        if (!enableShapeMemory) return;

        // Quaternion từ rotation của Rigidbody2D (độ)
        Quaternion q = Quaternion.Euler(0f, 0f, centerRb.rotation);
        bool grounded = IsGrounded();
        float gBoost = grounded ? groundedShapeBoost : 1f;

        for (int i = 0; i < pointRBs.Count; i++)
        {
            var prb = pointRBs[i];

            // Hướng world mong muốn của point i
            Vector2 worldDir = (Vector2)(q * (Vector3)baseLocalDirs[i]);
            worldDir.Normalize();

            // Vị trí mục tiêu theo hướng gốc + độ phình hiện tại
            float targetDist = baseCenterDistances[i] * inflateFactor;
            Vector2 targetPos = centerRb.position + worldDir * targetDist;

            // Lực shape-memory kiểu lò xo: F = k * (x_target - x) - c * (v_rel)
            Vector2 delta = targetPos - prb.position;
            Vector2 vRel = prb.linearVelocity - centerRb.linearVelocity;

            Vector2 force = delta * (shapeStiffness * gBoost) - vRel * shapeDamping;

            // Chặn lực để ổn định
            if (force.sqrMagnitude > shapeForceClamp * shapeForceClamp)
                force = force.normalized * shapeForceClamp;

            prb.AddForce(force, ForceMode2D.Force);

            // Anti-shear: dập vận tốc tiếp tuyến để tránh xẹp ngang
            if (enableAntiShear)
            {
                Vector2 tangent = new(-worldDir.y, worldDir.x); // quay 90 độ
                float vTan = Vector2.Dot(vRel, tangent);
                prb.AddForce(-tangent * vTan * antiShearDamping, ForceMode2D.Force);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || baseLocalDirs.Count != pointRBs.Count) return;
        Gizmos.color = new Color(0.2f, 0.9f, 0.9f, 0.9f);
        Quaternion q = Quaternion.Euler(0f, 0f, centerRb.rotation);
        for (int i = 0; i < pointRBs.Count; i++)
        {
            Vector2 worldDir = (Vector2)(q * (Vector3)baseLocalDirs[i]);
            Vector2 target = centerRb.position + worldDir.normalized * (baseCenterDistances[i] * inflateFactor);
            Gizmos.DrawLine(centerRb.position, target);
        }
    }
#endif
}
