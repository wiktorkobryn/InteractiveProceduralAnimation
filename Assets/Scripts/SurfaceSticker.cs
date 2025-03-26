using UnityEngine;

public class SurfaceSticker : MonoBehaviour
{
    private Transform restPosition;
    private RaycastHit hit;
    private Vector3 targetPosition;

    private void Start()
    {
        restPosition = transform.parent;
    }

    private void FixedUpdate()
    {
        if(Physics.Raycast(restPosition.position, Vector3.down, out hit, Mathf.Infinity))
        {
            targetPosition = transform.position;
            targetPosition.y = hit.point.y;

            transform.position = targetPosition;
        }
    }
}
