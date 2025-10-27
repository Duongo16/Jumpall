    // Assets/Scripts/GroundAnyCollider.cs
    using System.Collections;
    using Unity.Cinemachine;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    [RequireComponent(typeof(Collider2D))]
    public class GroundAnyCollider : MonoBehaviour
    {
        [Header("Refs")]
        public Camera cam;
        public Rigidbody2D playerRb;

        [Header("Y rules")]
        public Vector2 yOffsetRange = new(-0.5f, 1.2f);
        public float maxStepUp = 0.9f;
        public float maxStepDown = 2.0f;
        public bool alignGridY = false;
        public float gridY = 0.5f;

        [Header("Adaptive gap")]
        public bool adaptiveGap = true;
        public float minGap = 2f;
        public float maxGap = 24f;
        public float speedGain = 0.25f;
        public float jitter = 0.6f;

        [Header("Jump estimate")]
        public float estJumpImpulse = 10f;
        public float estHoldUpForce = 40f;
        public float estHoldTime = 0.25f;
        public float airGravityScale = 10f;
        public float safeFactor = 0.85f;

        [Header("Despawn")]
        public float extraDespawnLeft = 40f;

        [Header("Fast drop on pass")]
        public bool dropOnPass = true;
        public float passMargin = 0.3f;
        public float passDropDelay = 0f;
        public float passDropSpeed = 30f;
        public float passDropDistance = 30f;
        public bool passDisableCollider = true;
        public bool destroyAfterPassDrop = true;

        [Header("Spawn settle (random fall-in)")]
        public bool settleOnSpawn = true;
        [Range(0f, 100f)] public float settlePercent = 50f;
        public float settleLift = 2f;
        public float settleSpeed = 8f;
        public float settleDelay = 0f;

        [Header("Special shapes")]
        public float overrideWidth = 0f;

        // debug
        public float lastGap;
        public float lastSafeMax;
        public float groundHeight;

        // ===== Falling ground =====
        [Header("Falling ground")]
        public bool fallingEnabled = true;
        [Range(0f, 100f)] public float fallablePercent = 25f;
        public string playerTag = "Player";
        [Range(0f, 5f)] public float fallDelay = 0.2f;
        public float fallSpeed = 3f;
        public float fallDistance = 3f;
        public bool disableColliderOnFall = false;
        public bool markFallableTint = true;
        public Color fallableTint = new(1f, 0.9f, 0.5f, 1f);
        public bool destroyAfterFall = false; // để false để còn rơi nhanh lần 2

        // ===== Mystery Box spawn =====
        [Header("Mystery Box")]
        public GameObject mysteryBoxPrefab;
        [Range(0f, 100f)] public float boxSpawnPercent = 25f;
        public float boxYOffset = 1.0f;
        public bool attachToGround = true;
        public bool spawnOnFallable = true;

        Collider2D col;
        CompositeCollider2D comp;
        Tilemap tm;
        SpriteRenderer sr;

        bool spawnedNext;
        float baseY, prevY;

        // falling state
        bool isFallable;
        bool fallingTriggered;   // đang có coroutine rơi chạy
        float fallStartY;
        float fallen;

        // settle state
        bool settling;
        float settleTargetY;

        // two-phase fall state
        bool firstFallDone;      // đã rơi xong lần 1 tới fallDistance
        bool inPassDrop;         // đang rơi nhanh lần 2
        bool passRequested;      // đã vượt qua trong lúc rơi lần 1

        private CinemachineImpulseSource impulseSource;

        void Awake()
        {
            if (!cam) cam = Camera.main;
            col = GetComponent<Collider2D>();
            comp = GetComponent<CompositeCollider2D>();
            tm = GetComponent<Tilemap>();
            sr = GetComponent<SpriteRenderer>();
            impulseSource = GetComponent<CinemachineImpulseSource>();
            if (tm) tm.CompressBounds();

            baseY = transform.position.y;
            prevY = baseY;
            groundHeight = CurrentBounds().max.y;
        }

        void FixedUpdate()
        {
            if (!cam) return;

            float screenRight = cam.ViewportToWorldPoint(new Vector3(1f, .5f, 0f)).x;
            float screenLeft = cam.ViewportToWorldPoint(new Vector3(0f, .5f, 0f)).x;

            if (!spawnedNext && CurrentBounds().max.x < screenRight)
            {
                SpawnNext();
                spawnedNext = true;
            }

            // rơi nhanh khi Player đã vượt block
            if (dropOnPass && playerRb)
            {
                float rightEdge = CurrentBounds().max.x;
                if (playerRb.position.x > rightEdge + passMargin)
                {
                    if (!isFallable)
                    {
                        if (!inPassDrop) StartPassDrop();
                    }
                    else
                    {
                        if (firstFallDone)
                        {
                            if (!inPassDrop) StartPassDrop();
                        }
                        else
                        {
                            passRequested = true;
                        }
                    }
                }
            }

            if (CurrentBounds().max.x < screenLeft - extraDespawnLeft)
                Destroy(gameObject);
        }

        Bounds CurrentBounds() => comp ? comp.bounds : col.bounds;
        float Width() => overrideWidth > 0f ? overrideWidth : CurrentBounds().size.x;

        void SpawnNext()
        {
            float width = Width();
            float vx = playerRb ? Mathf.Max(0f, playerRb.linearVelocity.x) : 0f;

            float desiredY = baseY + Random.Range(yOffsetRange.x, yOffsetRange.y);
            float limitedY = Mathf.Clamp(desiredY, prevY - maxStepDown, prevY + maxStepUp);
            if (alignGridY) limitedY = Mathf.Round(limitedY / gridY) * gridY;

            float gapX = ComputeGap(vx, limitedY - prevY);
            float newX = CurrentBounds().max.x + width * 0.5f + gapX;

            var go = Instantiate(gameObject, transform.parent);
            var g = go.GetComponent<GroundAnyCollider>();

            var p = go.transform.position;
            p.x = newX; p.y = limitedY;
            go.transform.position = p;

            // refresh refs
            g.cam = cam; g.playerRb = playerRb;
            g.spawnedNext = false;
            g.baseY = baseY; g.prevY = limitedY;
            g.col = go.GetComponent<Collider2D>();
            g.comp = go.GetComponent<CompositeCollider2D>();
            g.tm = go.GetComponent<Tilemap>();
            g.sr = go.GetComponent<SpriteRenderer>();
            if (g.tm) g.tm.CompressBounds();
            g.groundHeight = g.CurrentBounds().max.y;

            // purge cloned boxes
            var oldBoxes = go.GetComponentsInChildren<MisteryBox>(true);
            for (int i = 0; i < oldBoxes.Length; i++) Destroy(oldBoxes[i].gameObject);

            // random fallable tint
            g.isFallable = g.fallingEnabled && (Random.value < g.fallablePercent / 100f);
            if (g.isFallable && g.markFallableTint)
            {
                if (g.tm) g.tm.color = g.fallableTint;
                else if (g.sr) g.sr.color = g.fallableTint;
            }

            // settle on spawn
            bool willSettle = g.settleOnSpawn && (Random.value < g.settlePercent / 100f);
            if (willSettle)
            {
                g.settling = true;
                g.settleTargetY = limitedY;

                var sp = go.transform.position;
                sp.y = limitedY + g.settleLift;
                go.transform.position = sp;

                g.StartCoroutine(g.CoSettle());
            }

            // spawn box
            bool canSpawnBox = g.mysteryBoxPrefab
                               && (Random.value < g.boxSpawnPercent / 100f)
                               && (g.spawnOnFallable || !g.isFallable);

            if (canSpawnBox)
            {
                if (willSettle) g.StartCoroutine(SpawnBoxAfterSettle(g));
                else g.StartCoroutine(SpawnBoxAfterBoundsReady(g));
            }
        }

        IEnumerator CoSettle()
        {
            if (settleDelay > 0f) yield return new WaitForSeconds(settleDelay);

            // rơi theo nhịp vật lý
            while (transform.position.y > settleTargetY)
            {
                yield return new WaitForFixedUpdate();
                float ny = Mathf.MoveTowards(transform.position.y, settleTargetY,
                                             settleSpeed * Time.fixedDeltaTime);
                var pos = transform.position; pos.y = ny; transform.position = pos;
            }

            settling = false;
            groundHeight = CurrentBounds().max.y;
        }

        IEnumerator SpawnBoxAfterSettle(GroundAnyCollider g)
        {
            while (g.settling) yield return null;
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();

            var groundB = g.CurrentBounds();
            float x = groundB.center.x;
            var parent = g.attachToGround ? g.transform : g.transform.parent;

            var box = Instantiate(g.mysteryBoxPrefab, new Vector3(x, groundB.max.y, 0f), Quaternion.identity);
            box.transform.SetParent(parent, true);

            var bb = CalcObjectBounds(box);
            float halfH = bb.extents.y;
            var pos = box.transform.position;
            pos.x = x;
            pos.y = groundB.max.y + halfH + g.boxYOffset;
            box.transform.position = pos;
        }

        IEnumerator SpawnBoxAfterBoundsReady(GroundAnyCollider g)
        {
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();

            var groundB = g.CurrentBounds();
            float x = groundB.center.x;

            var parent = g.attachToGround ? g.transform : g.transform.parent;

            var box = Instantiate(g.mysteryBoxPrefab, new Vector3(x, groundB.max.y, 0f), Quaternion.identity);
            box.transform.SetParent(parent, true);

            var bb = CalcObjectBounds(box);
            float halfH = bb.extents.y;
            var pos = box.transform.position;
            pos.x = x;
            pos.y = groundB.max.y + halfH + g.boxYOffset;
            box.transform.position = pos;
        }

        Bounds CalcObjectBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                return b;
            }
            var cols = go.GetComponentsInChildren<Collider2D>(true);
            if (cols.Length > 0)
            {
                Bounds b = cols[0].bounds;
                for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
                return b;
            }
            return new Bounds(go.transform.position, Vector3.zero);
        }

        float ComputeGap(float vx, float dY)
        {
            float baseGap = adaptiveGap ? (minGap + vx * speedGain) : Random.Range(minGap, minGap + 1f);
            baseGap += Random.Range(-jitter, jitter);

            float safeMax = EstimateMaxJumpDist(vx, dY) * safeFactor;
            lastSafeMax = safeMax;

            float clamped = Mathf.Clamp(baseGap, minGap, Mathf.Min(maxGap, Mathf.Max(minGap, safeMax)));
            lastGap = clamped;
            return clamped;
        }

        float EstimateMaxJumpDist(float vx, float dY)
        {
            float mass = playerRb ? Mathf.Max(0.01f, playerRb.mass) : 1f;
            float v0y = estJumpImpulse / mass;
            float dvHold = estHoldUpForce / mass * estHoldTime;

            float g = Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.01f, airGravityScale);
            float vy = Mathf.Max(0f, v0y + dvHold);

            float A = 0.5f * g;
            float B = -vy;
            float C = dY;
            float disc = B * B - 4f * A * C;
            if (disc <= 0f) return 0f;
            float t1 = (-B - Mathf.Sqrt(disc)) / (2f * A);
            float t2 = (-B + Mathf.Sqrt(disc)) / (2f * A);
            float t = Mathf.Max(t1, t2);
            if (t <= 0f) return 0f;

            return vx * t;
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (!string.IsNullOrEmpty(playerTag) && !c.collider.CompareTag(playerTag)) return;
            TryStartFall();
        }

        void OnTriggerEnter2D(Collider2D c)
        {
            if (!string.IsNullOrEmpty(playerTag) && !c.CompareTag(playerTag)) return;
            TryStartFall();
        }

        // ======= FALL COROS =======

        IEnumerator CoFall(float delay, float speed, float distance, bool disableCol, bool destroy, System.Action onDone = null)
        {
            fallingTriggered = true;

            if (disableCol)
            {
                if (comp) comp.isTrigger = true;
                else if (col) col.isTrigger = true;
            }

            if (delay > 0f) yield return new WaitForSeconds(delay);

            fallStartY = transform.position.y;
            fallen = 0f;

            // rơi theo nhịp vật lý
            while (fallen < distance)
            {
                yield return new WaitForFixedUpdate();
                float step = speed * Time.fixedDeltaTime;
                fallen = Mathf.Min(fallen + step, distance);
                float y = fallStartY - fallen;
                var pos = transform.position; pos.y = y; transform.position = pos;
            }

            onDone?.Invoke();

            if (destroy) { Destroy(gameObject); yield break; }

            fallingTriggered = false; // cho phép rơi lần 2
        }

        void StartPassDrop()
        {
            if (inPassDrop) return;
            inPassDrop = true;

            StartCoroutine(CoFall(passDropDelay, passDropSpeed, passDropDistance, passDisableCollider, destroyAfterPassDrop, null));
        }

        void TryStartFall()
        {
            if (!fallingEnabled || !isFallable || fallingTriggered || firstFallDone) return;

            if (impulseSource && CameraShakeManager.instance != null)
                CameraShakeManager.instance.CameraShake(impulseSource);

            // rơi lần 1 tới fallDistance, KHÔNG hủy
            StartCoroutine(CoFall(
                fallDelay,
                fallSpeed,
                fallDistance,
                disableColliderOnFall,
                destroyAfterFall, // nên để false
                OnFirstFallDone
            ));
        }

        void OnFirstFallDone()
        {
            firstFallDone = true;
            if (passRequested && !inPassDrop) StartPassDrop();
        }

        void OnValidate()
        {
            if (alignGridY && gridY <= 0) gridY = 0.1f;
            if (maxStepUp < 0) maxStepUp = 0;
            if (maxStepDown < 0) maxStepDown = 0;
            if (minGap < 0) minGap = 0;
            if (maxGap < minGap) maxGap = minGap;
            if (safeFactor <= 0f) safeFactor = 0.5f;
            if (airGravityScale <= 0f) airGravityScale = 1f;

            fallablePercent = Mathf.Clamp(fallablePercent, 0f, 100f);
            if (fallSpeed < 0f) fallSpeed = 0f;
            if (fallDistance < 0f) fallDistance = 0f;

            boxSpawnPercent = Mathf.Clamp(boxSpawnPercent, 0f, 100f);

            if (passMargin < 0f) passMargin = 0f;
            if (passDropSpeed < 0f) passDropSpeed = 0f;
            if (passDropDistance < 0f) passDropDistance = 0f;

            settlePercent = Mathf.Clamp(settlePercent, 0f, 100f);
            if (settleLift < 0f) settleLift = 0f;
            if (settleSpeed < 0f) settleSpeed = 0f;
            if (settleDelay < 0f) settleDelay = 0f;
        }
    }
