// BridgeSegmentTrigger.cs
using UnityEngine;

public class BridgeSegmentTrigger : MonoBehaviour
{
    public BridgeCollapse root;
    public string playerTag = "Player";
    public LayerMask playerLayers; // bỏ trống nếu dùng tag

    void Reset() => root = GetComponentInParent<BridgeCollapse>();

    void OnCollisionEnter2D(Collision2D col)
    {
        if (IsPlayer(col.collider)) root?.TriggerCollapse();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other)) root?.TriggerCollapse();
    }

    bool IsPlayer(Collider2D c)
    {
        if (c.CompareTag(playerTag)) return true;
        if (playerLayers.value != 0 && ((1 << c.gameObject.layer) & playerLayers.value) != 0) return true;
        return false;
    }
}
