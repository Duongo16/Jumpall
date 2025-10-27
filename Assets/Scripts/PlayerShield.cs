// Assets/Scripts/PlayerShield.cs
using UnityEngine;

public class PlayerShield : MonoBehaviour
{
    [Tooltip("Số lần chặn Missable")]
    public int charges = 0;

    [Tooltip("Thời gian bỏ qua va chạm sau khi chặn")]
    public float postConsumeIgnoreTime = 0.15f;

    float _ignoreUntil;

    public void Add(int count = 1) => charges += count;

    public bool TryConsume()
    {
        if (charges <= 0) return false;
        charges--;
        _ignoreUntil = Time.time + postConsumeIgnoreTime;
        return true;
    }

    public bool IsTemporarilyIgnoring() => Time.time < _ignoreUntil;
}
