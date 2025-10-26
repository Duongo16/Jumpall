using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 spawnPoint;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        spawnPoint = transform.position; // lưu vị trí bắt đầu khi game khởi động
    }

    void Update()
    {
        if (mainCam == null) return;

        // Lấy biên của camera
        Vector3 viewPos = mainCam.WorldToViewportPoint(transform.position);

        // Nếu player ra ngoài vùng nhìn thấy (0–1 là trong camera)
        if (viewPos.x < 0 || viewPos.x > 1 || viewPos.y < 0 || viewPos.y > 1)
        {
            Respawn();
        }
    }

    void Respawn()
    {
        // Reset velocity để tránh quán tính khi respawn
        var rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // Cho player respawn cao hơn 1 chút để rơi nhẹ xuống
        transform.position = spawnPoint + new Vector3(0, 1f, 0);
    }
}
