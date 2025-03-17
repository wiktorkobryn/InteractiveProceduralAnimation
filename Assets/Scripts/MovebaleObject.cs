using UnityEngine;
using UnityEngine.InputSystem;

public class MovableObject : MonoBehaviour
{
    [Header("Movement")]
    public float movementSpeed = 10.0f;
    private Vector2 movementInput = new Vector2(0, 0);
    private Vector3 movementDirection = new Vector3(0, 0, 0);
    private Rigidbody rigidbody;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();

        rigidbody.isKinematic = false;
        rigidbody.useGravity = true;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void GetKeyboardInput()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
    }

    private void MoveSpectator()
    {
        Vector3 forward = transform.forward * movementInput.y;
        Vector3 right = transform.right * movementInput.x;

        movementDirection = (forward + right).normalized * movementSpeed * Time.deltaTime;

        rigidbody.MovePosition(rigidbody.position + movementDirection);
    }

    private void FixedUpdate()
    {
        GetKeyboardInput();
        MoveSpectator();
    }
}
