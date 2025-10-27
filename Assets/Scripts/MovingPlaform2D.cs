using UnityEngine;

public class MovingPlatform2D : MonoBehaviour
{
    public Transform pointA;
    public Transform pointMid;
    public Transform pointB;
    public float speed = 2f;
    public bool isLeft; // true = platform trái, false = platform phải

    private Vector3 target;

    void Start()
    {
        // platform trái đi từ A -> Mid, phải đi từ Mid -> B
        target = isLeft ? pointMid.position : pointB.position;
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // platform trái: A <-> Mid, phải: Mid <-> B
        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            if (isLeft)
                target = target == pointA.position ? pointMid.position : pointA.position;
            else
                target = target == pointMid.position ? pointB.position : pointMid.position;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (Mathf.Abs(rb.linearVelocity.y) < 0.1f)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
    }
}
