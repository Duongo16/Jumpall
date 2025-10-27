using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public class MisteryBox : MonoBehaviour
{
    [Header("Pickup")]
    public string playerTag = "Player";
    public bool destroyOnPickup = true;

    [Header("Timed effects")]
    public float timedDuration = 10f; // LowGravity / SpeedUp

    // guards
    bool _consumed = false;
    int _enterFrame = -1;
    Collider2D _col;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // chặn nhiều collider cùng lúc
        if (_consumed || _enterFrame == Time.frameCount) return;
        if (!IsPlayer(other)) return;

        _consumed = true;
        _enterFrame = Time.frameCount;
        if (_col) _col.enabled = false;

        var root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        var rb2d = root.GetComponentInParent<Rigidbody2D>();
        var runner = root.GetComponentInParent<SoftBodyEndlessRunner>();
        var softBody = root.GetComponentInParent<SoftBodyBuilder2D>();
        var health = root.GetComponentInParent<PlayerHealth>();
        var shield = root.GetComponentInParent<PlayerShield>();

        var effects = root.GetComponentInParent<TimedEffectController>();
        if (!effects)
        {
            effects = root.AddComponent<TimedEffectController>();
            effects.rb = rb2d;
            effects.runner = runner;

            if (!effects.effectText)
            {
                var go = GameObject.Find("EffectLabel");
                if (go) effects.effectText = go.GetComponent<TMP_Text>();
                if (!effects.effectText)
                {
                    var any = Object.FindObjectOfType<TMP_Text>(true);
                    if (any) effects.effectText = any;
                }
            }
        }

        int pick = Random.Range(0, 5);
        string fxName = "";

        switch (pick)
        {
            case 0: // SHRINK -2 (instant)
                if (softBody)
                {
                    softBody.pointCount = Mathf.Max(3, softBody.pointCount - 2);
                    softBody.SendMessage("RebuildNow", SendMessageOptions.DontRequireReceiver);
                    softBody.SendMessage("Rebuild", SendMessageOptions.DontRequireReceiver);
                    softBody.SendMessage("BuildRing", SendMessageOptions.DontRequireReceiver);
                    softBody.SendMessage("RebuildShape", SendMessageOptions.DontRequireReceiver);
                    fxName = "SHRINK -2";
                }
                break;

            case 1: // LOW GRAVITY (timed, non-stack, trùng ⇒ reset)
                effects.lowGravityDelta = 4f;
                effects.ApplyLowGravity(timedDuration);
                fxName = "LOW GRAVITY";
                break;

            case 2: // SPEED UP x1.2 (timed, non-stack, trùng ⇒ reset)
                effects.speedUpMultiplier = 1.2f;
                effects.ApplySpeedUp(timedDuration);
                fxName = "SPEED UP";
                break;

            case 3: // HEALTH +1 (instant)
                if (health) { health.GetHealth(1); fxName = "HEALTH +1"; }
                break;

            case 4: // SHIELD +1 (20s, no stack, no countdown)
                if (effects.TryGrantTimedShield(shield, root, 20f))
                    fxName = "SHIELD +1";
                else
                    fxName = "SHIELD HELD";
                break;
        }

        if (!string.IsNullOrEmpty(fxName))
            EffectTextUI.Instance?.Flash(fxName, 0.5f);

        if (destroyOnPickup) Destroy(gameObject);
    }

    bool IsPlayer(Collider2D other)
    {
        // lọc theo tag collider/rigidbody gốc
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
        {
            var rb = other.attachedRigidbody;
            if (!(rb && rb.CompareTag(playerTag))) return false;
        }
        return true;
    }
}
