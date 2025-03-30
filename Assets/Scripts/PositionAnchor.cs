using UnityEngine;

public class PositionAnchor : MonoBehaviour
{
    [SerializeField]
    private bool anchor = false;
    public bool Anchor
    {
        get { return anchor; }
        set
        {
            anchor = value;
            SetLockPosition();
        }
    }

    private Vector3 positionLastFrame;
    private Quaternion rotationLastFrame;

    private void Start()
    {
        if (anchor == true)
            SetLockPosition();
    }

    private void SetLockPosition()
    {
        positionLastFrame = transform.position;
        rotationLastFrame = transform.rotation;
    }

    private void LateUpdate()
    {
        if (anchor == true)
        {
            transform.position = positionLastFrame;
            transform.rotation = rotationLastFrame;
        }
    }
}
