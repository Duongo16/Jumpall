using System.Collections;
using UnityEngine;
using TMPro;

public enum TimedEffectType { None, LowGravity, SpeedUp }

public class TimedEffectController : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody2D rb;
    public SoftBodyEndlessRunner runner;
    public TMP_Text effectText;

    [Header("Config")]
    public float lowGravityDelta = 4f;
    public float speedUpMultiplier = 1.2f;
    public float speedUpAccel = 120f;

    // timed state
    TimedEffectType _type = TimedEffectType.None;
    float _timeLeft = 0f;

    // frame-gate: 1 frame chỉ nhận 1 loại effect
    int _lastApplyFrame = -1;

    // backups
    float _baseGravityScale = 1f;
    float _baseRunnerGravDelta = 0f;

    // speed state
    float _speedTarget = 0f;
    float _preSpeedAbs = 0f;
    int _xSign = 1;

    // shield timed
    public bool HasShield { get; private set; } = false;
    PlayerShield _shieldRef;
    bool _shieldCreatedByUs = false;
    Coroutine _shieldCR;

    void Awake()
    {
        if (!rb) rb = GetComponentInParent<Rigidbody2D>();
        if (!runner) runner = GetComponentInParent<SoftBodyEndlessRunner>();
        if (rb) _baseGravityScale = rb.gravityScale;
        if (runner) _baseRunnerGravDelta = runner.gravityDeltaOverride;

        if (!effectText)
        {
            var go = GameObject.Find("EffectLabel");
            if (go) effectText = go.GetComponent<TMP_Text>();
            if (!effectText) effectText = FindObjectOfType<TMP_Text>(true);
        }
        if (effectText) effectText.richText = true;

        UpdateLabel();
    }

    // gate helper
    bool BlockIfSameFrameDifferent(TimedEffectType requestType)
    {
        if (_lastApplyFrame == Time.frameCount && _type != requestType) return true; // khác loại cùng frame => bỏ
        _lastApplyFrame = Time.frameCount; // cùng loại thì cho phép (reset timer)
        return false;
    }

    // ===== Low Gravity (timed) =====
    public void ApplyLowGravity(float seconds)
    {
        if (BlockIfSameFrameDifferent(TimedEffectType.LowGravity)) return;

        bool same = (_type == TimedEffectType.LowGravity);
        if (!same) ClearCurrent();

        _type = TimedEffectType.LowGravity;
        _timeLeft = seconds;

        if (runner)
        {
            if (!same) _baseRunnerGravDelta = runner.gravityDeltaOverride;
            runner.gravityDeltaOverride = _baseRunnerGravDelta - lowGravityDelta;
        }
        else if (rb)
        {
            if (!same) _baseGravityScale = rb.gravityScale;
            rb.gravityScale = Mathf.Max(0.1f, _baseGravityScale - lowGravityDelta);
        }

        UpdateLabel();
    }

    // ===== Speed Up (timed) =====
    public void ApplySpeedUp(float seconds)
    {
        if (BlockIfSameFrameDifferent(TimedEffectType.SpeedUp)) return;

        bool same = (_type == TimedEffectType.SpeedUp);
        if (!same)
        {
            ClearCurrent();
            var v0 = rb ? rb.linearVelocity : Vector2.zero;
            _xSign = v0.x >= 0f ? 1 : -1;
            _preSpeedAbs = Mathf.Abs(v0.x); // snapshot trước buff
        }

        _type = TimedEffectType.SpeedUp;
        _timeLeft = seconds;

        float baseTarget = Mathf.Max(_preSpeedAbs, 3f);
        _speedTarget = baseTarget * speedUpMultiplier;

        UpdateLabel();
    }

    void FixedUpdate()
    {
        if (_type == TimedEffectType.SpeedUp && rb)
        {
            var v = rb.linearVelocity;
            float cur = Mathf.Abs(v.x);
            float next = Mathf.MoveTowards(cur, _speedTarget, speedUpAccel * Time.fixedDeltaTime);
            v.x = next * _xSign;
            rb.linearVelocity = v;
        }
    }

    void Update()
    {
        if (_type == TimedEffectType.None) return;

        _timeLeft -= Time.deltaTime;
        if (_timeLeft <= 0f)
        {
            ClearCurrent();
            return;
        }
        UpdateLabel();
    }

    public void ClearCurrent()
    {
        if (_type == TimedEffectType.LowGravity)
        {
            if (runner) runner.gravityDeltaOverride = _baseRunnerGravDelta;
            if (rb) rb.gravityScale = _baseGravityScale;
        }
        else if (_type == TimedEffectType.SpeedUp && rb)
        {
            var v = rb.linearVelocity;
            v.x = _preSpeedAbs * _xSign;
            rb.linearVelocity = v;
        }

        _type = TimedEffectType.None;
        _timeLeft = 0f;
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (!effectText) return;

        if (_type == TimedEffectType.None)
        {
            effectText.text = "";
            return;
        }

        int secs = Mathf.CeilToInt(_timeLeft);
        if (_type == TimedEffectType.SpeedUp)
            effectText.text = $"<size=30>Speed up</size>\n<size=90>{secs}</size>";
        else if (_type == TimedEffectType.LowGravity)
            effectText.text = $"<size=30>Low gravity</size>\n<size=90>{secs}</size>";
    }

    // ===== Shield 20s, no stack, no countdown =====
    public bool TryGrantTimedShield(PlayerShield shield, GameObject root, float seconds = 20f)
    {
        if (HasShield) return false;

        _shieldCreatedByUs = false;
        if (!shield) { shield = root.AddComponent<PlayerShield>(); _shieldCreatedByUs = true; }

        _shieldRef = shield;
        _shieldRef.SendMessage("Add", 1, SendMessageOptions.DontRequireReceiver);
        HasShield = true;

        if (_shieldCR != null) StopCoroutine(_shieldCR);
        _shieldCR = StartCoroutine(CoShieldTimer(seconds));
        return true;
    }

    IEnumerator CoShieldTimer(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (_shieldRef)
        {
            if (_shieldCreatedByUs) Destroy(_shieldRef);
            else _shieldRef.SendMessage("Remove", 1, SendMessageOptions.DontRequireReceiver);
        }

        HasShield = false;
        _shieldRef = null;
        _shieldCR = null;
    }

    public TimedEffectType CurrentType => _type;
}

