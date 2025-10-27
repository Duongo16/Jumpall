using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody2D playerRb;                 // kéo Rigidbody2D của Player vào
    public Transform cam;                        // kéo Main Camera vào (dùng biên spawn)
    [Header("Tiles")]
    public GameObject segmentPrefab;             // prefab 1 miếng nền (đã canh mép liền mạch)
    public float segmentWidth = 20f;             // bề ngang 1 miếng theo world unit
    public int poolSize = 6;                     // >= (spawnAhead + despawnBehind)/segmentWidth + 2

    [Header("Parallax")]
    public float depth = 4f;                     // nhỏ = chạy nhanh, lớn = chậm (xa)
    public float speedMul = 1f;                  // hệ số nhân tốc Player
    public float extraSpeed = 0f;                // cộng thêm nếu cần

    [Header("Spawn window")]
    public float spawnAhead = 60f;               // luôn phủ kín tới cam.x + spawnAhead
    public float despawnBehind = 30f;            // dịch trái quá cam.x - despawnBehind thì quấn ra phải

    readonly List<Transform> segs = new();
    int leftIdx = 0, rightIdx = 0;

    void Start()
    {
        if (!cam) cam = Camera.main ? Camera.main.transform : null;
        if (!playerRb || !segmentPrefab || !cam) { enabled = false; return; }

        // Khởi tạo pool thành dải liên tục quanh camera
        float startX = Mathf.Floor((cam.position.x - despawnBehind) / segmentWidth) * segmentWidth;
        for (int i = 0; i < poolSize; i++)
        {
            var t = Instantiate(segmentPrefab, transform).transform;
            t.position = new Vector3(startX + i * segmentWidth, transform.position.y, transform.position.z);
            segs.Add(t);
        }
        leftIdx = 0;
        rightIdx = segs.Count - 1;
    }

    void FixedUpdate()
    {
        // 1) Tính tốc độ parallax theo Player
        float vx = playerRb.linearVelocity.x;
        float parallaxV = (vx * speedMul) / Mathf.Max(0.001f, depth) + extraSpeed;
        float dx = parallaxV * Time.fixedDeltaTime;

        // 2) Di chuyển tất cả miếng
        for (int i = 0; i < segs.Count; i++)
        {
            var p = segs[i].position;
            p.x -= dx;
            segs[i].position = p;
        }

        // 3) Bảo đảm luôn có nền phía trước: quấn miếng trái ra bên phải
        float leftEdge = cam.position.x - despawnBehind;
        float rightEdge = cam.position.x + spawnAhead;

        // quấn các miếng nằm hẳn phía sau cửa sổ nhìn
        while (segs[leftIdx].position.x + segmentWidth * 0.5f < leftEdge)
        {
            float newX = segs[rightIdx].position.x + segmentWidth;
            var p = segs[leftIdx].position;
            p.x = newX;
            segs[leftIdx].position = p;

            rightIdx = leftIdx;
            leftIdx = (leftIdx + 1) % segs.Count;
        }

        // (tùy tốc độ rất cao) lặp thêm để chắc chắn phủ đến rightEdge
        while (segs[rightIdx].position.x < rightEdge)
        {
            int idx = (rightIdx + 1) % segs.Count;
            float newX = segs[rightIdx].position.x + segmentWidth;
            var p = segs[idx].position;
            p.x = newX;
            segs[idx].position = p;

            rightIdx = idx;
            if (idx == leftIdx) leftIdx = (leftIdx + 1) % segs.Count; // giữ vòng hợp lệ
        }
    }
}

