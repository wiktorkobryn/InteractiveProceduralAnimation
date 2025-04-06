using UnityEngine;

public class SurfaceSticker : MonoBehaviour
{
    private Transform restPosition;
    private RaycastHit hit;
    private Vector3 targetPosition;
    public LayerMask layerToIgnore = Physics.IgnoreRaycastLayer;
    public float bodyOffsetY;

    private void Start()
    {
        restPosition = transform.parent;
    }

    private void LateUpdate()
    {
        if (Physics.Raycast(restPosition.position, Vector3.down, out hit, Mathf.Infinity, ~layerToIgnore))
        {
            targetPosition = transform.position;
            targetPosition.y = hit.point.y + bodyOffsetY;
            transform.position = targetPosition;
        }
    }
}
