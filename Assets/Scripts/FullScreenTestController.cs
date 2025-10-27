// Assets/Scripts/FullScreenEffectController.cs
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FullScreenEffectController : MonoBehaviour
{
    [Header("Refs")]
    public TimedEffectController effects;          // auto-find nếu để trống
    public Graphic targetGraphic;                  // Image/RawImage full-screen (ưu tiên)
    public Renderer targetRenderer;                // hoặc Quad/SpriteRenderer

    [Header("Auto setup")]
    public bool autoFind = true;
    public string playerTag = "Player";
    public bool autoCreateOverlayIfMissing = true; // tự tạo Canvas + Image nếu thiếu
    public int overlaySortingOrder = 5000;

    [Header("Materials")]
    public Material speedUpMat;                    // gán material SpeedUp
    public Material shieldMat;                     // gán material Shield

    [Header("Preview")]
    public bool previewSpeedUp;
    public bool previewShield;
    public bool verbose;

    Material _current;
    static Sprite _whiteSprite;

    void Reset()
    {
        targetGraphic = GetComponent<Graphic>();
        targetRenderer = GetComponent<Renderer>();
    }

    void Awake()
    {
        // Auto-find refs
        if (autoFind)
        {
            if (!effects)
            {
                var player = GameObject.FindGameObjectWithTag(playerTag);
                if (player) effects = player.GetComponentInChildren<TimedEffectController>(true);
                if (!effects) effects = FindObjectOfType<TimedEffectController>(true);
            }
            if (!targetGraphic) targetGraphic = GetComponentInChildren<Graphic>(true);
            if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>(true);
        }

        // Tự tạo overlay nếu chưa có gì
        if (!targetGraphic && !targetRenderer && autoCreateOverlayIfMissing)
            targetGraphic = CreateOverlayImage();

        EnsureUiDrawable();

        // Ẩn lúc đầu
        Apply(null);
    }

    void LateUpdate()
    {
        Material want = null;

        // Preview > runtime
        if (previewSpeedUp && speedUpMat) want = speedUpMat;
        else if (previewShield && shieldMat) want = shieldMat;
        else if (effects)
        {
            if (effects.CurrentType == TimedEffectType.SpeedUp && speedUpMat) want = speedUpMat;
            else if (effects.HasShield && shieldMat) want = shieldMat;
        }

        if (!ReferenceEquals(want, _current)) Apply(want);
    }

    // ===== helpers =====
    void Apply(Material m)
    {
        _current = m;

        if (targetGraphic)
        {
            targetGraphic.material = m;
            targetGraphic.enabled = (m != null);
        }

        if (targetRenderer)
        {
            if (m != null)
            {
                targetRenderer.sharedMaterial = m;
                targetRenderer.enabled = true;
            }
            else
            {
                targetRenderer.enabled = false;
            }
        }

        if (verbose) Debug.Log($"[FullScreenFX] {(m ? "ON → " + m.name : "OFF")}", this);
    }

    void EnsureUiDrawable()
    {
        if (!targetGraphic) return;

        // RawImage cần texture
        var ri = targetGraphic as RawImage;
        if (ri && ri.texture == null) ri.texture = Texture2D.whiteTexture;

        // Image cần sprite
        var img = targetGraphic as Image;
        if (img && img.sprite == null)
        {
            if (_whiteSprite == null)
                _whiteSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0, 0, 1, 1),
                    new Vector2(0.5f, 0.5f)
                );
            img.sprite = _whiteSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
        }
    }

    Graphic CreateOverlayImage()
    {
        // Canvas
        var canvasGO = new GameObject("FullScreenFX_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var cv = canvasGO.GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = overlaySortingOrder;

        // Image
        var imgGO = new GameObject("FullScreenFX_Image", typeof(Image));
        imgGO.transform.SetParent(canvasGO.transform, false);
        var rt = imgGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = imgGO.GetComponent<Image>();
        img.raycastTarget = false; // không che input
        return img;
    }
}
