using UnityEngine;
using UnityEngine.InputSystem;

public class Spectator : MonoBehaviour
{
    [Header("Camera")]
    public Camera cameraFPS;
    public Vector2 sensitivity = new Vector2(5000f, 5000f);
    public Quaternion CameraDirection { get; private set; }

    private Vector2 mouseInput = new Vector2(0, 0);
    private Vector2 rotation = new Vector2(0, 0);

    [Header("Movement")]
    public float movementSpeed = 10.0f;
    private Vector2 movementInput = new Vector2(0, 0);
    private Vector3 movementDirection = new Vector3(0, 0, 0);

    private void Start()
    {
        if (cameraFPS == null)
            cameraFPS = GetComponentInChildren<Camera>();

        SimulationManager.HideMouseCursor();
    }

    private void GetMouseInput()
    {
        mouseInput.x = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensitivity.x;
        mouseInput.y = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensitivity.y;

        rotation.y += mouseInput.x;
        rotation.x -= mouseInput.y;

        // limiting rotation
        rotation.x = Mathf.Clamp(rotation.x, -90.0f, 90.0f);
    }

    private void GetKeyboardInput()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
    }

    private void RotateCamera()
    {
        transform.rotation = Quaternion.Euler(0, rotation.y, 0);
        CameraDirection = Quaternion.Euler(0, rotation.y, 0);
        cameraFPS.transform.localRotation = Quaternion.Euler(rotation.x, 0, 0);
    }

    private void MoveSpectator()
    {
        Vector3 forward = transform.forward * movementInput.y;
        Vector3 right = transform.right * movementInput.x;

        movementDirection = (forward + right).normalized * movementSpeed * Time.deltaTime;
        transform.position += movementDirection;
    }

    private void Update()
    {
        GetMouseInput();
        GetKeyboardInput();

        RotateCamera();
        MoveSpectator();
    }
}
