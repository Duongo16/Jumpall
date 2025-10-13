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

    [Tooltip("Khi giữ, scale khoảng cách lò xo lên bao nhiêu lần (>1).")]
    [Range(1.05f, 3f)] public float holdInflateMul = 3f;

    [Tooltip("Tốc độ nội suy về target distances khi giữ / nhả.")]
    public float inflateLerpSpeed = 16f;

    [Tooltip("Áp suất đẩy ra ngoài cho mỗi point khi đang giữ (Force per frame).")]
    public float outwardPressure = 200f;

    [Tooltip("Lực đẩy lên trung tâm trong khi giữ (để dễ ‘nhảy’).")]
    public float upwardPush = 12f;

    [Tooltip("Tỉ lệ cộng thêm lực nếu đang chạm đất (nhảy bốc hơn).")]
    public float groundedBoost = 1.6f;

    [Tooltip("Giới hạn tốc độ tối đa của center để không ‘phóng tên lửa’. 0 = bỏ qua.")]
    public float maxCenterSpeed = 20f;

    [Header("Move (A/D)")]
    public float moveForce = 14f;

    [Header("Grounding (để boost nhảy)")]
    public LayerMask groundMask;
    public float groundCheckDist = 0.19f;

    Rigidbody2D centerRb;
    readonly List<Rigidbody2D> pointRBs = new();
    readonly List<SpringJoint2D> rimSprings = new();
    readonly List<SpringJoint2D> centerSprings = new();

    float baseRimDistance;
    readonly List<float> baseCenterDistances = new();

    void Start()
    {
        centerRb = GetComponent<Rigidbody2D>();
        BuildRing();
    }

    void BuildRing()
    {
        if (!pointPrefab) { Debug.LogError("[SoftBody] Chưa gán Point Prefab!"); return; }

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        pointRBs.Clear(); rimSprings.Clear(); centerSprings.Clear(); baseCenterDistances.Clear();

        for (int i = 0; i < pointCount; i++)
        {
            float ang = i * Mathf.PI * 2f / pointCount;
            Vector2 pos = (Vector2)transform.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
            var p = Instantiate(pointPrefab, pos, Quaternion.identity, transform);

            var rb = p.GetComponent<Rigidbody2D>() ?? p.AddComponent<Rigidbody2D>();
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            pointRBs.Add(rb);
        }

        for (int i = 0; i < pointRBs.Count; i++)
        {
            int next = (i + 1) % pointRBs.Count;
            int prev = (i - 1 + pointRBs.Count) % pointRBs.Count;

            CreateRimSpring(pointRBs[i], pointRBs[next]);
            if (linkPrevAndNext) CreateRimSpring(pointRBs[i], pointRBs[prev]);

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

    bool IsGrounded()
    {
        Vector2 origin = centerRb.position;
        var hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDist, groundMask);
        return hit.collider != null;
    }

    void Update()
    {
        bool inflating = HoldingSpace();

        // 1) Target distances
        float rimTarget = inflating ? baseRimDistance * holdInflateMul : baseRimDistance;
        float lerpSpd = Time.deltaTime * inflateLerpSpeed;

        foreach (var sj in rimSprings)
            sj.distance = Mathf.Lerp(sj.distance, rimTarget, lerpSpd);

        for (int i = 0; i < centerSprings.Count; i++)
        {
            float target = baseCenterDistances[i] * (inflating ? holdInflateMul : 1f);
            var sj = centerSprings[i];
            sj.distance = Mathf.Lerp(sj.distance, target, lerpSpd);
        }

        // 2) “Áp suất”: khi giữ thì các point bị đẩy ra khỏi tâm + center được đẩy lên
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

        // 3) Clamp tốc độ trung tâm để tránh nổ quá đà
        if (maxCenterSpeed > 0f)
        {
            var v = centerRb.linearVelocity;
            float spd = v.magnitude;
            if (spd > maxCenterSpeed) centerRb.linearVelocity = v * (maxCenterSpeed / spd);
        }

        // 4) A/D
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
            Vector2 f = new Vector2(h * moveForce, 0f);
            foreach (var rb in pointRBs) rb.AddForce(f, ForceMode2D.Force);
        }
    }
}