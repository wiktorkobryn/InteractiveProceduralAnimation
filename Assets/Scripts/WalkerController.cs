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

    [Header("Movement")]
    public float movementSpeed = 20.0f;
    public float rotationSpeed = 20.0f;
    private float movementVerical, movementHorizontal;

    private void Start()
    {
        walkerBody.localPosition = bodyOffset;
    }

    #region orientation_control

    private void SetPositionFromPlane()
    {
        targetPosition = this.transform.position;
        targetPosition.y = hit.point.y;

        this.transform.position = targetPosition;
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

    #endregion

    #region movement_inputs
    private void ApplyRotation()
    {
        if (Input.GetKey(KeyCode.Q))
            transform.Rotate(Vector3.down * rotationSpeed * Time.deltaTime);
        else if (Input.GetKey(KeyCode.E))
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }

    private void ApplyPosition()
    {
        movementVerical = Input.GetAxis("Vertical") * Time.deltaTime * movementSpeed;
        movementHorizontal = Input.GetAxis("Horizontal") * Time.deltaTime * movementSpeed;

        transform.Translate(new Vector3(movementHorizontal, 0, movementVerical));
    }

    private void FixedUpdate()
    {
        ApplyRotation();
        ApplyPosition();
    }

    #endregion
}
