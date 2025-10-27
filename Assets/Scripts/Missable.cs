// Assets/Scripts/Enemies/Missable.cs  (bản đã thêm Shield + Damage)
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Missable : MonoBehaviour
{
    [Header("Anim")]
    [SerializeField] Animator anim;
    [SerializeField] string idleStateName = "Idle";
    [SerializeField] string flyTrigger = "isFly";
    [SerializeField] string exploreTrigger = "isExplore";

    [Header("Timing")]
    [SerializeField] float idleDuration = 2f;
    [SerializeField] float exploreDestroyDelay = 0.6f;

    [Header("Move")]
    [SerializeField] float speed = 6f;
    [SerializeField] bool flyRightToLeft = true;
    [SerializeField] float flyStartXOffset = 3f;

    [Header("Spawn")]
    [SerializeField] bool spawnAtCameraRightEdge = true;
    [SerializeField] float rightInset = 0.2f;
    [SerializeField] float spawnX = 7.6f;
    [SerializeField] float spawnYOffset = 0f;
    [SerializeField] bool followCameraDuringIdle = true;

    [Header("Despawn")]
    [SerializeField] bool useDynamicDespawn = true;
    [SerializeField] float despawnMargin = 0.5f;
    [SerializeField] float despawnX = -9f;

    [Header("Detect")]
    [SerializeField] string playerTag = "Player";

    Rigidbody2D rb;
    Collider2D hitbox;
    bool isExploring;
    Coroutine runCo;
    float ySpawned;

    void Reset()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = true;
        hitbox.isTrigger = true;
    }

    void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!hitbox) hitbox = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
        rb.isKinematic = true;
        hitbox.isTrigger = true;
    }

    public void Arm(float playerYAtFire, float rightEdgeInset)
    {
        rightInset = rightEdgeInset;
        float xSpawn = spawnAtCameraRightEdge ? CamRightX() - rightInset : spawnX;
        ySpawned = playerYAtFire + spawnYOffset;
        transform.position = new Vector3(xSpawn, ySpawned, transform.position.z);

        if (runCo != null) StopCoroutine(runCo);
        runCo = StartCoroutine(RunStateMachine());
    }

    IEnumerator RunStateMachine()
    {
        if (!string.IsNullOrEmpty(idleStateName))
            anim.Play(idleStateName, 0, 0f);

        float t = 0f;
        while (t < idleDuration && !isExploring)
        {
            if (followCameraDuringIdle && spawnAtCameraRightEdge)
            {
                float x = CamRightX() - rightInset;
                transform.position = new Vector3(x, ySpawned, transform.position.z);
            }
            t += Time.deltaTime;
            yield return null;
        }

        transform.position += new Vector3(flyStartXOffset, 0f, 0f);
        anim.SetTrigger(flyTrigger);

        float dir = flyRightToLeft ? -1f : 1f;

        while (!isExploring)
        {
            transform.Translate(Vector3.right * dir * speed * Time.deltaTime, Space.World);

            float leftBound = useDynamicDespawn ? CamLeftX() - despawnMargin : despawnX;
            float rightBound = useDynamicDespawn ? CamRightX() + despawnMargin : despawnX;

            if ((flyRightToLeft && transform.position.x <= leftBound) ||
                (!flyRightToLeft && transform.position.x >= rightBound))
            {
                DestroySelf();
                yield break;
            }
            yield return null;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isExploring) return;
        if (!IsPlayer(other)) return;

        var root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;

        // Shield: bỏ qua va chạm 1 lần
        var shield = root.GetComponentInParent<PlayerShield>();
        if (shield && (shield.IsTemporarilyIgnoring() || shield.TryConsume()))
        {
            StartCoroutine(TempIgnore(other));
            return; // không nổ, tiếp tục bay
        }

        // Không có shield => gây sát thương và nổ
        var hp = root.GetComponentInParent<PlayerHealth>();
        if (hp) hp.TakeDame(1);

        isExploring = true;
        hitbox.enabled = false;
        anim.SetTrigger(exploreTrigger);
        Invoke(nameof(DestroySelf), exploreDestroyDelay);
    }

    IEnumerator TempIgnore(Collider2D other)
    {
        Physics2D.IgnoreCollision(hitbox, other, true);
        yield return new WaitForSeconds(0.2f);
        if (hitbox && other) Physics2D.IgnoreCollision(hitbox, other, false);
    }

    public void AE_ExploreEnd()
    {
        CancelInvoke(nameof(DestroySelf));
        DestroySelf();
    }

    void DestroySelf()
    {
        if (this) Destroy(gameObject);
    }

    public void SetConfig(float newSpeed, float newIdleDuration, float newDespawnX)
    {
        speed = newSpeed;
        idleDuration = newIdleDuration;
        despawnX = newDespawnX;
    }

    bool IsPlayer(Collider2D other)
    {
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
        {
            var rb = other.attachedRigidbody;
            if (!(rb && rb.CompareTag(playerTag))) return false;
        }
        return true;
    }

    float CamHalfW()
    {
        var cam = Camera.main;
        return cam.orthographicSize * cam.aspect;
    }
    float CamRightX() => Camera.main.transform.position.x + CamHalfW();
    float CamLeftX() => Camera.main.transform.position.x - CamHalfW();
}
