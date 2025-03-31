using TMPro;
using UnityEngine;
using UnityEngine.Splines;

public class WalkerController : MonoBehaviour
{
    public Transform raycastPoint, walkerBody;
    public Vector3 bodyOffset = Vector3.zero;
    public LayerMask layerToIgnore;

    private RaycastHit hit;
    private Vector3 targetPosition;

    private void SetPositionFromPlane()
    {
        targetPosition = walkerBody.position;
        targetPosition.y = hit.point.y;
        walkerBody.position = targetPosition + bodyOffset;
    }

    private void SetRotationFromPlane()
    {
        walkerBody.rotation = Quaternion.FromToRotation(walkerBody.up, hit.normal) * walkerBody.rotation;
    }

    void LateUpdate()
    {
        if (Physics.Raycast(raycastPoint.position, Vector3.down, out hit, Mathf.Infinity, ~layerToIgnore))
        {
            SetPositionFromPlane();
            SetRotationFromPlane();
        }
    }
}
