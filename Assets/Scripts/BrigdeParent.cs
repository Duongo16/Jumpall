// BridgeCollapse.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BridgeCollapse : MonoBehaviour
{
    [Tooltip("Để trống = tự quét toàn bộ HingeJoint2D con")]
    public List<HingeJoint2D> hinges = new();

    public bool alsoDisableDistanceJoints = true;
    public bool alsoDisableFixedJoints = true;
    public bool addRandomTorque = false;
    public float torqueImpulse = 20f;

    [Header("Delay trước khi gãy")]
    public float collapseDelay = 0.6f;

    bool collapsed;
    bool pending;

    void Awake()
    {
        if (hinges.Count == 0)
            hinges.AddRange(GetComponentsInChildren<HingeJoint2D>(true));
    }

    public void TriggerCollapse(float? overrideDelay = null)
    {
        if (collapsed || pending) return;
        StartCoroutine(CollapseAfterDelay(overrideDelay ?? collapseDelay));
    }

    IEnumerator CollapseAfterDelay(float delay)
    {
        pending = true;
        yield return new WaitForSeconds(delay);
        Collapse();
        pending = false;
    }

    public void Collapse()
    {
        if (collapsed) return;
        collapsed = true;

        foreach (var hj in hinges) if (hj) hj.enabled = false;
        if (alsoDisableDistanceJoints)
            foreach (var dj in GetComponentsInChildren<DistanceJoint2D>(true)) if (dj) dj.enabled = false;
        if (alsoDisableFixedJoints)
            foreach (var fj in GetComponentsInChildren<FixedJoint2D>(true)) if (fj) fj.enabled = false;

        if (addRandomTorque)
            foreach (var rb in GetComponentsInChildren<Rigidbody2D>())
                if (rb) rb.AddTorque(Random.Range(-torqueImpulse, torqueImpulse), ForceMode2D.Impulse);
    }
}
