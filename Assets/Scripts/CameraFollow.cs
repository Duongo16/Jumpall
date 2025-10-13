using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public float followSpeed = 2f;
    public float yOffset = 1f;
    public Transform followTarget;

    private void Update()
    {
        Vector3 newPos = new Vector3(followTarget.position.x, followTarget.position.y + yOffset, -10f);
        transform.position = Vector3.Slerp(transform.position, newPos, followSpeed*Time.deltaTime);
    }

}
