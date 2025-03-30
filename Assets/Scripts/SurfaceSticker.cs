using UnityEngine;

public class SurfaceSticker : MonoBehaviour
{
    private Transform restPosition;
    private RaycastHit hit;
    private Vector3 targetPosition;

    public float deadZoneDistance = 0.01f;

    private void Start()
    {
        restPosition = transform.parent;
    }

    private void LateUpdate()
    {
        if (Physics.Raycast(restPosition.position, Vector3.down, out hit, Mathf.Infinity))
        {
            float newY = hit.point.y;

            if (Mathf.Abs(transform.position.y - newY) > deadZoneDistance)
            {
                targetPosition = transform.position;
                targetPosition.y = newY;
                transform.position = targetPosition;
            }
        }
    }
}
