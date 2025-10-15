// DoorGateTrigger2D.cs  — no idle flash when closing
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DoorGateTrigger2D : MonoBehaviour
{
    [Header("Door refs")]
    public Transform topDoor;
    public Transform bottomDoor;

    [Header("Motion")]
    public float openDistance = 1.2f;
    public float openSpeed = 3f;
    public float closeSpeed = 2f;
    public float autoCloseDelay = 0.15f;
    [SerializeField] float arriveEps = 0.001f;

    [Header("Animator (bools optional)")]
    [SerializeField] Animator anim;
    [SerializeField] string openParam = "isOpen";
    [SerializeField] string closeParam = "isClose";

    [Header("Animator direct jump (bypass Idle)")]
    [SerializeField] bool useDirectCrossFade = true;
    [SerializeField] string openStateName = "Open";   // tên state trong Animator
    [SerializeField] string closeStateName = "Close"; // tên state trong Animator
    [SerializeField] float crossFadeDuration = 0.05f; // 0–0.1s khuyến nghị

    [Header("Detect")]
    public string playerTag = "Player";

    enum DoorState { Closed, Opening, Open, Closing }
    DoorState _state = DoorState.Closed;

    Vector3 _topClosed, _botClosed;
    Vector3 _topOpen, _botOpen;
    Rigidbody2D _rbTop, _rbBot;

    int _insideCount;
    float _exitTime;
    bool _pendingClose;

    int _openHash, _closeHash, _lastPlayedHash = 0;

    void Reset()
    {
        var bc = GetComponent<BoxCollider2D>(); bc.isTrigger = true;
        var rb = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; rb.simulated = true;
    }

    void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!topDoor || !bottomDoor) { Debug.LogError("[DoorGate] Missing door refs", this); return; }

        _topClosed = topDoor.position;
        _botClosed = bottomDoor.position;
        _topOpen = _topClosed + Vector3.up * openDistance;
        _botOpen = _botClosed - Vector3.up * openDistance;

        _rbTop = topDoor.GetComponent<Rigidbody2D>();
        _rbBot = bottomDoor.GetComponent<Rigidbody2D>();

        _openHash = Animator.StringToHash(openStateName);
        _closeHash = Animator.StringToHash(closeStateName);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        _insideCount++;
        _pendingClose = false;

        if (_state == DoorState.Closed || _state == DoorState.Closing)
            BeginOpen();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        _insideCount = Mathf.Max(0, _insideCount - 1);
        if (_insideCount == 0) { _exitTime = Time.time; _pendingClose = true; }
    }

    void FixedUpdate()
    {
        if (_insideCount == 0 && _pendingClose && Time.time >= _exitTime + autoCloseDelay)
        {
            if (_state == DoorState.Open || _state == DoorState.Opening) BeginClose();
            _pendingClose = false;
        }

        switch (_state)
        {
            case DoorState.Opening:
                StepMove(_topOpen, _botOpen, openSpeed, () => _state = DoorState.Open);
                break;

            case DoorState.Closing:
                StepMove(_topClosed, _botClosed, closeSpeed, () =>
                {
                    _state = DoorState.Closed;
                    SetBool(closeParam, false); // chỉ tắt 1 lần khi đã đóng
                });
                break;
        }
    }

    void BeginOpen()
    {
        _state = DoorState.Opening;
        if (useDirectCrossFade) CrossFadeOnce(_openHash);
        SetBool(closeParam, false);
        SetBool(openParam, true);
    }

    void BeginClose()
    {
        _state = DoorState.Closing;
        if (useDirectCrossFade) CrossFadeOnce(_closeHash);
        SetBool(openParam, false);
        SetBool(closeParam, true);
    }

    void CrossFadeOnce(int stateHash)
    {
        if (!anim) return;
        if (_lastPlayedHash == stateHash) return;          // tránh spam
        anim.CrossFadeInFixedTime(stateHash, crossFadeDuration, 0, 0f);
        _lastPlayedHash = stateHash;
    }

    void StepMove(Vector3 topTarget, Vector3 botTarget, float speed, System.Action afterArrive)
    {
        float step = speed * Time.fixedDeltaTime;
        bool topArrived = MoveOne(topDoor, _rbTop, topTarget, step);
        bool botArrived = MoveOne(bottomDoor, _rbBot, botTarget, step);
        if (topArrived && botArrived) afterArrive?.Invoke();
    }

    bool MoveOne(Transform t, Rigidbody2D rb, Vector3 target, float step)
    {
        if (!t) return true;
        Vector3 cur = t.position;
        if ((cur - target).sqrMagnitude <= arriveEps * arriveEps) return true;

        Vector3 next = Vector3.MoveTowards(cur, target, step);
        if (rb && rb.bodyType != RigidbodyType2D.Dynamic) rb.MovePosition(next);
        else t.position = next;

        return (next - target).sqrMagnitude <= arriveEps * arriveEps;
    }

    void SetBool(string p, bool v)
    {
        if (!anim || string.IsNullOrEmpty(p)) return;
        if (anim.GetBool(p) == v) return; // không spam để tránh nháy
        anim.SetBool(p, v);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (topDoor)
        {
            _topClosed = topDoor.position;
            _topOpen = _topClosed + Vector3.up * openDistance;
        }
        if (bottomDoor)
        {
            _botClosed = bottomDoor.position;
            _botOpen = _botClosed - Vector3.up * openDistance;
        }
    }
#endif
}
