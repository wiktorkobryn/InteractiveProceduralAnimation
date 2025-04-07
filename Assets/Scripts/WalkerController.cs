using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Splines;
using static UnityEngine.CullingGroup;

public class WalkerController : MonoBehaviour, IObservable
{
    public WalkerState State { get; private set; }

    [Header("General")]
    public bool isActive = true;
    protected bool isActiveBeforeChange = false;
    public Transform raycastPoint, walkerBody;
    public float bodyOffsetY;
    public LayerMask layerToIgnore;
    public bool rotateToPlaneNormal = false;

    private RaycastHit hit;
    private Vector3 targetPosition;

    [Header("Movement")]
    public float movementSpeed = 20.0f;
    public float rotationSpeed = 20.0f;
    [Range(1f, 5f)]
    public float speedMultiplier = 2.0f;
    private float currentMovSpeed = 20.0f;
    private float movementVerical, movementHorizontal;

    [Header("Observable")]
    public List<GameObject> observers = new List<GameObject>();

    private void Start()
    {
        walkerBody.localPosition = new Vector3(0,bodyOffsetY,0);
        StartCoroutine(ActivateNextFrame());
    }

    private IEnumerator ActivateNextFrame()
    {
        yield return new WaitForEndOfFrame();
        Activate();
    }

    public virtual void Activate()
    {
        if (isActive != isActiveBeforeChange)
        {
            if (isActive)
                State = WalkerState.Idle;
            else
                State = WalkerState.Off;

            isActiveBeforeChange = isActive;
            NotifyObservers();
        }
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
            
            if(rotateToPlaneNormal)
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
        movementVerical = Input.GetAxis("Vertical") * Time.deltaTime * currentMovSpeed;
        movementHorizontal = Input.GetAxis("Horizontal") * Time.deltaTime * currentMovSpeed;

        transform.Translate(new Vector3(movementHorizontal, 0, movementVerical));
    }

    public bool IsMoving()
    {
        return State == WalkerState.Move || State == WalkerState.MoveFast;
    }

    public bool IsNotRotating()
    {
        return Input.GetKey(KeyCode.Q) == false && Input.GetKey(KeyCode.E) == false;
    }

    public bool IsAnyInput()
    {
        return movementHorizontal != 0 || movementVerical != 0 || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E);
    }

    private void FixedUpdate()
    {
        if (State != WalkerState.Off)
        {
            ApplyRotation();
            ApplyPosition();

            if (IsAnyInput() == false && IsMoving())        // stop moving
            {
                State = WalkerState.Idle;
                NotifyObservers();
            }
            else if (State == WalkerState.Move && IsAnyInput() && IsNotRotating() && Input.GetKey(KeyCode.LeftShift))  // start sprinting
            {
                currentMovSpeed = movementSpeed * speedMultiplier;
                State = WalkerState.MoveFast;
                NotifyObservers();
            }
            else if (IsAnyInput() && State != WalkerState.Move)   // start moving
            {
                currentMovSpeed = movementSpeed;
                State = WalkerState.Move;
                NotifyObservers();
            }
        }
    }

    #endregion

    #region observable

    public void NotifyObservers()
    {
        foreach (GameObject g in observers)
        {
            // MonoBehaviour & Unity inspector interface limitations
            g.SendMessage("OnValueChanged", (int)State);
        }
    }

    public void AddObserver(GameObject obj)
    {
        observers.Add(obj);
    }

    public void DeleteObserver(GameObject obj)
    {
        observers.Remove(obj);
    }

    #endregion
}
