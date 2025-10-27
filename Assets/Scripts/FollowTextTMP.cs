// Assets/Scripts/FollowTextTMP.cs
using UnityEngine;
using TMPro;

public class FollowTextTMP : MonoBehaviour
{
    public Transform target;
    public Vector3 worldOffset = new(0f, 1.5f, 0f);
    public float smooth = 20f;
    public bool lockRotation = true;

    TMP_Text _tmp;

    void Awake() { _tmp = GetComponent<TMP_Text>(); }

    void LateUpdate()
    {
        if (!target) return;
        Vector3 goal = target.position + worldOffset;
        transform.position = Vector3.Lerp(transform.position, goal, smooth * Time.deltaTime);
        if (lockRotation) transform.rotation = Quaternion.identity; // không xoay theo Player
    }

}
