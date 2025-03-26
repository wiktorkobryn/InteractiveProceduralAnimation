using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectTracker : MonoBehaviour, IObservable
{
    public TrackerState State { get; protected set; }

    [Header("General")]
    public bool isActive = true;
    protected bool isActiveBeforeChange = true;
    public bool movementInOutEasing = false;
    public float detectionRange = 10.0f;
    public bool StateChanged { get; protected set; } = false;
    public List<GameObject> observers = new List<GameObject>();

    [Header("Bones")]
    public Transform rootBone;
    public Transform neckBone, headBone, headBoneEnd;

    [Header("Actions")]
    public Vector3 neckRotationAxis = new Vector3(0, 1, 0);
    public Vector3 headRotationAxis = new Vector3(1, 0, 0);
    [Range(0f, 180f)]
    public float neckBoneBoundMax = 60.0f, headBoneBoundMax = 80.0f;
    protected Quaternion restNeckRotation, restHeadRotation;

    [Header("State: Search")]
    public string detectionLayerName = "Detectable";
    public float sideSearchDuration = 5.0f;
    public GameObject exclusiveFocused;
    public float refreshSearchInterval = 0.3f, checkInBoundsInterval = 0.2f;

    [Header("State: Lost")]
    public float resetDuration = 2.0f;
    public float timeToBeLost = 3.0f;

    protected void OnValidate()
    {
        Vector3.Normalize(neckRotationAxis);
        Vector3.Normalize(headRotationAxis);

        Activate();
    }

    [ContextMenu("FindBones()")]
    public void FindBones()
    {
        if (rootBone != null)
        {
            neckBone = rootBone.GetChild(0);
            headBone = neckBone.GetChild(0);
            headBoneEnd = headBone.GetChild(0);
        }
    }

    public virtual void Activate()
    {
        if (isActive != isActiveBeforeChange)
        {
            if (isActive)
                State = TrackerState.Detected;
            else
                State = TrackerState.Off;

            isActiveBeforeChange = isActive;
            StateChanged = true;
        }
    }

    public void ResetState()
    {
        State = TrackerState.Lost;
        StateChanged = true;
    }

    protected virtual void Start() 
    {
        restNeckRotation = neckBone.localRotation;
        restHeadRotation = headBone.localRotation;

        Activate();
        if (isActive)
            State = TrackerState.Detected;
        else
            State = TrackerState.Off;

        StateChanged = true;
    }

    protected virtual void FixedUpdate()
    {
        // only visible in editor
        if(isActive)
            DebugDrawRays();

        if (StateChanged)
        {
            StopAllCoroutines();

            OnTrackerStateChanged();

            NotifyObservers();
            StateChanged = false;
        }
    }

    protected virtual void OnTrackerStateChanged()
    {
        switch (State)
        {
            case TrackerState.Detected:
                StartCoroutine(FollowTarget());
                break;
            default:
            case TrackerState.Off:
                StartCoroutine(ResetRotationsAnimation(false));
                break;
        }
    }

    #region animation

    protected virtual IEnumerator ResetRotationsAnimation(bool waitForLost = true, TrackerState stateToChange = TrackerState.Off)
    {
        // wait for object to return in area
        if(waitForLost)
            yield return new WaitForSecondsRealtime(timeToBeLost);

        // waiting for rotations to finish
        Coroutine headReset = StartCoroutine(Transf3D.RotateOverTime(headBone, resetDuration, headBone.localRotation, restHeadRotation, movementInOutEasing));
        Coroutine neckReset = StartCoroutine(Transf3D.RotateOverTime(neckBone, resetDuration, neckBone.localRotation, restNeckRotation, movementInOutEasing));

        yield return headReset;
        yield return neckReset;

        // additional time spacing before search
        yield return new WaitForSecondsRealtime(0.3f);

        if (stateToChange != TrackerState.Off)
        {
            State = stateToChange;
            StateChanged = true;
        }   
    }

    protected virtual void DebugDrawRays()
    {
        Debug.DrawRay(headBoneEnd.position, (exclusiveFocused.transform.position - headBone.position).normalized * detectionRange, Color.green);
    }

    protected virtual IEnumerator FollowTarget()
    {
        Quaternion targetRotationNeck, targetRotationHead;
        float angleNeck = 0f,
              angleHead = 0f;

        while (true)
        {
            if (exclusiveFocused == null)
                break;

            targetRotationNeck = CalculateLocalNeckLookAtTarget();
            targetRotationHead = CalculateLocalHeadLookAtTarget();

            angleNeck = Transf3D.CalculateAngleSigned(targetRotationNeck, restNeckRotation, Vector3.one);
            angleHead = Transf3D.CalculateAngleSigned(targetRotationHead, restHeadRotation, Vector3.one);

            // target out of neck bone bounds (default: horizontally)
            if (Mathf.Abs(angleNeck) > neckBoneBoundMax)
            {
                targetRotationNeck = restNeckRotation * Quaternion.AngleAxis(neckBoneBoundMax * Mathf.Sign(angleNeck), neckRotationAxis);
            }

            // target out of head bone bounds (default: vertically)
            if (Mathf.Abs(angleHead) > headBoneBoundMax)
            {
                targetRotationHead = restHeadRotation * Quaternion.AngleAxis(headBoneBoundMax * Mathf.Sign(angleHead), headRotationAxis);
            }

            Coroutine neckMovement = StartCoroutine(Transf3D.RotateOverTime(neckBone, refreshSearchInterval,
                                                    neckBone.localRotation, targetRotationNeck, movementInOutEasing));
            Coroutine headMovement = StartCoroutine(Transf3D.RotateOverTime(headBone, refreshSearchInterval,
                                                    headBone.localRotation, targetRotationHead, movementInOutEasing));

            // resume after parallel coroutines finish
            yield return neckMovement;
            yield return headMovement;
        }

        State = TrackerState.Off;
        StateChanged = true;
    }

    protected virtual Quaternion CalculateLocalNeckLookAtTarget()
    {
        // tracked position in local space
        Vector3 neckDirection = neckBone.InverseTransformPoint(exclusiveFocused.transform.position);

        // rotation angle as of tan(alpha) = x/z
        float neckFollowRotationAngle = Mathf.Atan2(neckDirection.x, neckDirection.z) * Mathf.Rad2Deg;

        // local rotation around chosen axis (Y)
        Quaternion targetRotation = neckBone.localRotation * Quaternion.AngleAxis(neckFollowRotationAngle, neckRotationAxis);
        return targetRotation;
    }
    
    protected virtual Quaternion CalculateLocalHeadLookAtTarget()
    {
        Vector3 headDirection = headBone.InverseTransformPoint(exclusiveFocused.transform.position);
        float headFollowRotationAngle = Mathf.Atan2(headDirection.z, headDirection.y) * Mathf.Rad2Deg;

        Quaternion targetRotation = headBone.localRotation * Quaternion.AngleAxis(headFollowRotationAngle, headRotationAxis);
        return targetRotation;
    }

    #endregion

    #region observers

    public void NotifyObservers()
    {
        foreach(GameObject g in observers)
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
