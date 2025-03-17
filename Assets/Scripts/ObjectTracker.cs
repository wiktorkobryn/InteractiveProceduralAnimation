using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectTracker : MonoBehaviour, IObservable
{
    public TrackerState State { get; private set; }

    [Header("General")]
    public bool isActive = true;
    private bool isActiveBeforeChange = true;
    public bool movementInOutEasing = false;
    public float detectionRange = 10.0f;
    public bool StateChanged { get; private set; } = false;
    public List<GameObject> observers = new List<GameObject>();

    [Header("Bones")]
    public Transform rootBone;
    public Transform neckBone, headBone, headBoneEnd;

    [Header("Actions")]
    public Vector3 neckRotationAxis = new Vector3(0, 1, 0);
    public Vector3 headRotationAxis = new Vector3(1, 0, 0);
    [Range(0f, 180f)]
    public float neckBoneBoundMax = 60.0f, headBoneBoundMax = 80.0f;
    private Quaternion restNeckRotation, restHeadRotation;

    [Header("State: Search")]
    public string detectionLayerName = "Detectable";
    public float sideSearchDuration = 5.0f;

    public GameObject exclusiveFocused;
    public float refreshSearchInterval = 0.3f, checkInBoundsInterval = 0.2f;

    private void OnValidate()
    {
        Vector3.Normalize(neckRotationAxis);
        Vector3.Normalize(headRotationAxis);

        if (isActive != isActiveBeforeChange)
            Activate(isActive);
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

    public void Activate(bool state)
    {
        isActive = state;
        StateChanged = true;

        if (isActive)
            State = TrackerState.Search;
        else
            State = TrackerState.Off;

        isActiveBeforeChange = isActive;
    }

    public void ResetState()
    {
        State = TrackerState.Lost;
        StateChanged = true;
    }

    public void Start() 
    {
        restNeckRotation = neckBone.localRotation;
        restHeadRotation = headBone.localRotation;

        if (isActive)
            State = TrackerState.Search;
        else
            State = TrackerState.Off;

        StateChanged = true;
    }

    private void FixedUpdate()
    {
        // only visible in editor
        DebugDrawRays();

        if (StateChanged)
        {
            StopAllCoroutines();

            switch(State)
            {
                case TrackerState.Detected:
                    StartCoroutine(FollowTarget());
                    break;
                default:
                case TrackerState.Off:
                    break;
            }

            NotifyObservers();
            StateChanged = false;
        }
    }

    #region animation_states

    private void DebugDrawRays()
    {
        Debug.DrawRay(headBoneEnd.position, (exclusiveFocused.transform.position - headBone.position).normalized * detectionRange, Color.green);
    }

    private IEnumerator FollowTarget()
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

    private Quaternion CalculateLocalNeckLookAtTarget()
    {
        // tracked position in local space
        Vector3 neckDirection = neckBone.InverseTransformPoint(exclusiveFocused.transform.position);

        // rotation angle as of tan(alpha) = x/z
        float neckFollowRotationAngle = Mathf.Atan2(neckDirection.x, neckDirection.z) * Mathf.Rad2Deg;

        // local rotation around chosen axis (Y)
        Quaternion targetRotation = neckBone.localRotation * Quaternion.AngleAxis(neckFollowRotationAngle, neckRotationAxis);
        return targetRotation;
    }
    
    private Quaternion CalculateLocalHeadLookAtTarget()
    {
        Vector3 headDirection = headBone.InverseTransformPoint(exclusiveFocused.transform.position);
        float headFollowRotationAngle = Mathf.Atan2(headDirection.z, headDirection.y) * Mathf.Rad2Deg;

        Quaternion targetRotation = headBone.localRotation * Quaternion.AngleAxis(headFollowRotationAngle, headRotationAxis);
        return targetRotation;
    }

    #endregion

    public void NotifyObservers()
    {
        foreach(GameObject g in observers)
        {
            // MonoBehaviour & Unity inspector interface limitations
            g.SendMessage("OnValueChanged", State);
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
}
