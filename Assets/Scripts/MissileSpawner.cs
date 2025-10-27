// Assets/Scripts/Enemies/MissableSpawner.cs
using System.Collections;
using UnityEngine;

public class MissableSpawner : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform player;
    [SerializeField] Missable missablePrefab;

    [Header("Flow")]
    [SerializeField] bool autoLoop = true;     // tuần tự: spawn -> chờ biến mất -> cooldown -> lặp
    [SerializeField] float startDelay = 3f;    // delay lúc vào game
    [SerializeField] float cooldown = 2f;      // nghỉ sau khi quả trước bị xoá

    [Header("Missable override (optional)")]
    [SerializeField] bool overrideConfig = true;
    [SerializeField] float speed = 7f;
    [SerializeField] float idleDuration = 2f;
    [SerializeField] float despawnX = -9f;

    [Header("Spawn position")]
    [SerializeField] float rightInset = 0.2f;  // sát mép phải camera
    [SerializeField] bool clampYToScreen = true;
    [SerializeField] float topInset = 0.2f, botInset = 0.2f;

    enum YSample { TransformY, ColliderCenter, Feet }
    [SerializeField] YSample ySource = YSample.ColliderCenter;

    Missable current;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(startDelay);
        if (autoLoop) yield return Loop();
    }

    IEnumerator Loop()
    {
        while (true)
        {
            yield return new WaitUntil(() => current == null);
            SpawnOnce();
            yield return new WaitUntil(() => current == null); // đợi bị Destroy
            yield return new WaitForSeconds(cooldown);         // rồi mới cooldown
        }
    }

    public void SpawnOnce()
    {
        if (!player || !missablePrefab) return;
        if (current != null) return;

        current = Instantiate(missablePrefab);
        if (overrideConfig) current.SetConfig(speed, idleDuration, despawnX);

        float y = SamplePlayerY();                  // Y hiện tại của Player
        if (clampYToScreen) y = ClampYToCamera(y);

        current.Arm(y, rightInset);                 // spawn mép phải + Y vừa lấy
        StartCoroutine(ClearWhenGone());
    }

    float SamplePlayerY()
    {
        float y = player.position.y;
        if (ySource != YSample.TransformY && player.TryGetComponent(out Collider2D c))
        {
            if (ySource == YSample.ColliderCenter) y = c.bounds.center.y;
            else if (ySource == YSample.Feet) y = c.bounds.min.y;
        }
        return y;
    }

    float ClampYToCamera(float y)
    {
        var cam = Camera.main;
        float halfH = cam.orthographicSize;
        float top = cam.transform.position.y + halfH - topInset;
        float bot = cam.transform.position.y - halfH + botInset;
        return Mathf.Clamp(y, bot, top);
    }

    IEnumerator ClearWhenGone()
    {
        // Unity object đã Destroy thì so sánh == null trả về true
        yield return new WaitUntil(() => current == null);
        current = null; // xoá tham chiếu cứng
    }
}
