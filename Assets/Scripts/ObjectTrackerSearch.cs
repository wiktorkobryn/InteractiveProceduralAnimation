using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectTrackerSearch : ObjectTracker, IObservable
{
    [Header("General")]
    public GameObject FocusedObject { get; private set; }
    public List<Transform> raycastPoints = new List<Transform>();

    [Header("State: Detected")]
    private bool outOfBoundsReached = false;

    public override void Activate()
    {
        if (isActive != isActiveBeforeChange)
        {
            if (isActive)
                State = TrackerState.Search;
            else
                State = TrackerState.Off;

            isActiveBeforeChange = isActive;
            StateChanged = true;
        }
    }

    protected override void Start() 
    {
        if (raycastPoints.Count < 1)
            raycastPoints = headBoneEnd.GetComponentsInChildren<Transform>().ToList();

        base.Start();
        if (isActive)
            State = TrackerState.Search;
    }

    protected override void OnTrackerStateChanged()
    {
        switch (State)
        {
            case TrackerState.Search:
                StartCoroutine(IdleSearchAnimation());
                StartCoroutine(IdleSearchForTarget());
                break;
            case TrackerState.Detected:
                StartCoroutine(FollowTarget());
                StartCoroutine(CheckIfFocusedInBounds());
                break;
            case TrackerState.Lost:
                StartCoroutine(ResetRotationsAnimation());
                StartCoroutine(IdleSearchForTarget());
                break;
            default:
            case TrackerState.Off:
                StartCoroutine(ResetRotationsAnimation(false, false));
                break;
        }
    }

    #region animation_states

    protected override void DebugDrawRays()
    {
        //Debug.DrawRay(headBone.position, headBone.TransformDirection(Vector3.up) * detectionRange, Color.blue);

        foreach(Transform p in raycastPoints)
        {
            Debug.DrawRay(headBoneEnd.position, (p.position - headBoneEnd.position).normalized * detectionRange, Color.red);
        }
    }

    protected IEnumerator IdleSearchAnimation()
    {
        Quaternion endRotation, startRotation;

        // first quarter
        endRotation = restNeckRotation * Quaternion.AngleAxis(neckBoneBoundMax, neckRotationAxis);
        startRotation = restNeckRotation;

        // synchronous coroutine
        yield return StartCoroutine(Transf3D.RotateOverTime(neckBone, sideSearchDuration / 2.0f, startRotation, endRotation, movementInOutEasing));

        startRotation = endRotation;
        endRotation = restNeckRotation * Quaternion.AngleAxis(-neckBoneBoundMax, neckRotationAxis);


        // repeating idle movement
        while (State == TrackerState.Search)
        {
            yield return StartCoroutine(Transf3D.RotateOverTime(neckBone, sideSearchDuration, startRotation, endRotation, movementInOutEasing));

            // swap with tuple
            (startRotation, endRotation) = (endRotation, startRotation);
        }
    }

    protected IEnumerator IdleSearchForTarget()
    {
        GameObject detected = null;

        while(true)
        {
            detected = SearchWithRaycasts();
            
            if (detected != null)
            {
                FocusedObject = detected;
                Debug.Log(FocusedObject.name);
                break;
            }

            yield return new WaitForSecondsRealtime(refreshSearchInterval);
        }

        State = TrackerState.Detected;
        StateChanged = true;
    }

    protected GameObject SearchWithRaycasts()
    {
        Ray ray;
        RaycastHit hit;

        foreach (Transform p in raycastPoints)
        {
            ray = new Ray(headBoneEnd.position, p.position - headBone.position);

            if (Physics.Raycast(ray, out hit, detectionRange) && hit.collider.gameObject.layer == LayerMask.NameToLayer(detectionLayerName))
                return hit.collider.gameObject;
        }

        return null;
    }

    protected override IEnumerator FollowTarget()
    {
        Quaternion targetRotationNeck, targetRotationHead;
        float angleNeck = 0f,
              angleHead = 0f;

        while (true)
        {
            outOfBoundsReached = false;
            targetRotationNeck = CalculateLocalNeckLookAtTarget();
            targetRotationHead = CalculateLocalHeadLookAtTarget();

            angleNeck = Transf3D.CalculateAngleSigned(targetRotationNeck, restNeckRotation, Vector3.one);
            angleHead = Transf3D.CalculateAngleSigned(targetRotationHead, restHeadRotation, Vector3.one);

            // target out of neck bone bounds (default: horizontally)
            if (Mathf.Abs(angleNeck) > neckBoneBoundMax)
            {
                targetRotationNeck = restNeckRotation * Quaternion.AngleAxis(neckBoneBoundMax * Mathf.Sign(angleNeck), neckRotationAxis);
                outOfBoundsReached = true;
            }

            // target out of head bone bounds (default: vertically)
            if (Mathf.Abs(angleHead) > headBoneBoundMax)
            {
                targetRotationHead = restHeadRotation * Quaternion.AngleAxis(headBoneBoundMax * Mathf.Sign(angleHead), headRotationAxis);
                outOfBoundsReached = true;
            }

            Coroutine neckMovement = StartCoroutine(Transf3D.RotateOverTime(neckBone, refreshSearchInterval,
                                                    neckBone.localRotation, targetRotationNeck, movementInOutEasing));
            Coroutine headMovement = StartCoroutine(Transf3D.RotateOverTime(headBone, refreshSearchInterval,
                                                    headBone.localRotation, targetRotationHead, movementInOutEasing));

            // resume after parallel coroutines finish
            yield return neckMovement;
            yield return headMovement;
        }
    }

    protected IEnumerator CheckIfFocusedInBounds()
    {
        Ray visionRay;
        RaycastHit hit;
        bool isHit, isInVision;

        while (true)
        {
            isInVision = IsFocusedInVision();

            if (outOfBoundsReached == true && isInVision == false)
            {
                Debug.Log("target lost: out of bounds");
                break;
            }
            else
            {
                visionRay = new Ray(headBoneEnd.position, FocusedObject.transform.position - headBoneEnd.position);
                isHit = Physics.Raycast(visionRay, out hit, detectionRange);

                if ((isHit == false || hit.collider.gameObject != FocusedObject) && isInVision == false)
                {
                    Debug.Log("target lost: covered, not visible");
                    break;
                }
            }

            yield return new WaitForSeconds(checkInBoundsInterval);
        }

        FocusedObject = null;
        State = TrackerState.Lost;
        StateChanged = true;
    }

    protected override Quaternion CalculateLocalNeckLookAtTarget()
    {
        // tracked position in local space
        Vector3 neckDirection = neckBone.InverseTransformPoint(FocusedObject.transform.position);

        // rotation angle as of tan(alpha) = x/z
        float neckFollowRotationAngle = Mathf.Atan2(neckDirection.x, neckDirection.z) * Mathf.Rad2Deg;

        // local rotation around chosen axis (Y)
        Quaternion targetRotation = neckBone.localRotation * Quaternion.AngleAxis(neckFollowRotationAngle, neckRotationAxis);
        return targetRotation;
    }

    protected override Quaternion CalculateLocalHeadLookAtTarget()
    {
        Vector3 headDirection = headBone.InverseTransformPoint(FocusedObject.transform.position);
        float headFollowRotationAngle = Mathf.Atan2(headDirection.z, headDirection.y) * Mathf.Rad2Deg;

        Quaternion targetRotation = headBone.localRotation * Quaternion.AngleAxis(headFollowRotationAngle, headRotationAxis);
        return targetRotation;
    }

    protected bool IsFocusedInVision()
    {
        Ray ray;
        RaycastHit hit;

        foreach(Transform p in raycastPoints)
        {
            ray = new Ray(headBoneEnd.position, p.position - headBone.position);

            if (Physics.Raycast(ray, out hit, detectionRange) && hit.collider.gameObject == FocusedObject)
                return true;
        }

        return false;
    }

    #endregion
}
