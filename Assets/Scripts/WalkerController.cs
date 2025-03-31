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

    public Transform raycastPoint, walkerBody;
    public float bodyOffsetY;
    public LayerMask layerToIgnore;
    public bool rotateToPlaneNormal = false;

    private RaycastHit hit;
    private Vector3 targetPosition;

    [Header("Movement")]
    public float movementSpeed = 20.0f;
    public float rotationSpeed = 20.0f;
    private float movementVerical, movementHorizontal;

    public List<GameObject> observers = new List<GameObject>();
    public bool isActive = true;
    protected bool isActiveBeforeChange = false;

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
        movementVerical = Input.GetAxis("Vertical") * Time.deltaTime * movementSpeed;
        movementHorizontal = Input.GetAxis("Horizontal") * Time.deltaTime * movementSpeed;

        transform.Translate(new Vector3(movementHorizontal, 0, movementVerical));
    }

    public bool IsMoving()
    {
        return State == WalkerState.Move || State == WalkerState.MoveFast;
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

            if (IsAnyInput() == false && IsMoving())
            {
                State = WalkerState.Idle;
                NotifyObservers();
            }
            else if (IsAnyInput() && IsMoving() == false)
            {
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
