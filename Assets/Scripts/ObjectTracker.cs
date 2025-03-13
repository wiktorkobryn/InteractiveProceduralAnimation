using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;
public enum TrackerState
{
    Off,
    Standby,
    Search,
    Detected,
    Lost
}

public enum TrackerMode
{
    Focus,  // awaits idly for any movement in area
    Scan    // moves in linear animation until target detection
}

public class ObjectTracker : MonoBehaviour
{
    public TrackerState State { get; private set; }
    public TrackerMode mode = TrackerMode.Scan;
    public StateIndicator stateIndicator;
    public bool isActive = true;
    private bool isActiveBeforeChange = true;
    public bool StateChanged { get; private set; } = false;

    public Transform neckBone, headBone;
    public Vector3 neckRotationAxis = new Vector3(0, 1, 0);
    public Vector3 headRotationAxis = new Vector3(1, 0, 0);
    private Quaternion restNeckRotation, restHeadRotation;

    // search state
    public float sideSearchDuration = 5.0f;
    public float neckBoneBoundMax = 60.0f, headBoneBoundMax = 80.0f;
    public string detectionLayerName = "Detectable";
    private LayerMask detectionLayer;
    //private HashSet<GameObject> objectsInAreaSet = new HashSet<GameObject>();
    public GameObject FocusedObject { get; private set; }
    public float refreshSearchInterval = 0.3f, checkInBoundsInterval;
    public float detectionRange = 10.0f;

    // reset state
    public float resetDuration = 2.0f, timeToBeLost = 3.0f;

    private void OnValidate()
    {
        Vector3.Normalize(neckRotationAxis);
        detectionLayer = LayerMask.GetMask(detectionLayerName);

        if (isActive != isActiveBeforeChange)
            Activate();
    }

    public void Activate()
    {
        StateChanged = true;

        if (isActive)
            State = TrackerState.Search;
        else
            State = TrackerState.Off;

        isActiveBeforeChange = isActive;
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
        //Debug.Log("neck: " + neckBone.localRotation.eulerAngles);
        //Debug.Log("head: " + headBone.localEulerAngles);
        //Debug.Log("---");
        //Debug.Log(Transf3D.CalculateAngle(neckBone.localRotation, restNeckRotation, new Vector3(1, 1, 1)));
        //Debug.Log(Transf3D.CalculateAngle(headBone.localRotation, restHeadRotation, new Vector3(1, 1, 1)));

        // only visible in editor
        Debug.DrawRay(headBone.position, headBone.TransformDirection(Vector3.up) * detectionRange, Color.blue);

        if (StateChanged)
        {
            StopAllCoroutines();

            switch(State)
            {
                case TrackerState.Search:
                    StartCoroutine(IdleSearchAnimation());
                    StartCoroutine(IdleSearchForTarget());
                    stateIndicator.ChangeSprite((int)TrackerState.Search);
                    break;
                case TrackerState.Detected:
                    StartCoroutine(FollowTarget());
                    StartCoroutine(CheckIfFocusedInBounds());
                    stateIndicator.ChangeSprite((int)TrackerState.Detected);
                    break;
                case TrackerState.Lost:
                    StartCoroutine(IdleSearchForTarget());
                    StartCoroutine(ResetRotationsAnimation());
                    stateIndicator.ChangeSprite((int)TrackerState.Lost);
                    break;
                default:
                case TrackerState.Off:
                    stateIndicator.ChangeSprite((int)TrackerState.Off);
                    break;
            }

            StateChanged = false;
        }
    }

    #region animation_states

    private IEnumerator IdleSearchAnimation()
    {
        Quaternion endRotation, startRotation;

        // first quarter
        endRotation = restNeckRotation * Quaternion.AngleAxis(neckBoneBoundMax, neckRotationAxis);
        startRotation = restNeckRotation;

        // synchronous coroutine
        yield return StartCoroutine(Transf3D.RotateOverTime(neckBone, sideSearchDuration / 2.0f, startRotation, endRotation));

        startRotation = endRotation;
        endRotation = restNeckRotation * Quaternion.AngleAxis(-neckBoneBoundMax, neckRotationAxis);


        // repeating idle movement
        while (State == TrackerState.Search)
        {
            yield return StartCoroutine(Transf3D.RotateOverTime(neckBone, sideSearchDuration, startRotation, endRotation));

            // swap with tuple
            (startRotation, endRotation) = (endRotation, startRotation);
        }
    }

    private IEnumerator IdleSearchForTarget()
    {
        RaycastHit hit;
        Ray visionRay;

        while(true)
        {
            visionRay = new Ray(headBone.position, headBone.TransformDirection(Vector3.up));

            if (Physics.Raycast(visionRay, out hit, detectionRange, detectionLayer))
            {
                FocusedObject = hit.collider.gameObject;

                Debug.Log(FocusedObject.name);
                break;
            }

            yield return new WaitForSecondsRealtime(refreshSearchInterval);
        }

        State = TrackerState.Detected;
        StateChanged = true;
    }

    private IEnumerator ResetRotationsAnimation()
    {
        FocusedObject = null;

        // wait for object to return in area
        yield return new WaitForSecondsRealtime(timeToBeLost);

        // waiting for rotations to finish
        Coroutine headReset = StartCoroutine(Transf3D.RotateOverTime(headBone, resetDuration, headBone.localRotation, restHeadRotation));
        Coroutine neckReset = StartCoroutine(Transf3D.RotateOverTime(neckBone, resetDuration, neckBone.localRotation, restNeckRotation));

        yield return headReset;
        yield return neckReset;

        // additional time spacing before search
        yield return new WaitForSecondsRealtime(0.3f);

        State = TrackerState.Search;
        StateChanged = true;
    }

    private IEnumerator FollowTarget()
    {
        while (true)
        {
            Coroutine neckMovement = StartCoroutine(Transf3D.RotateOverTime(neckBone, refreshSearchInterval, neckBone.localRotation, CalculateLocalNeckLookAtTarget()));
            Coroutine headMovement = StartCoroutine(Transf3D.RotateOverTime(headBone, refreshSearchInterval, headBone.localRotation, CalculateLocalHeadLookAtTarget()));

            // resume after parallel coroutines finish
            yield return neckMovement;
            yield return headMovement;
        }
    }

    private Quaternion CalculateLocalNeckLookAtTarget()
    {
        // tracked position in local space
        Vector3 neckDirection = neckBone.InverseTransformPoint(FocusedObject.transform.position);

        // rotation angle as of tan(alpha) = x/z
        float neckFollowRotationAngle = Mathf.Atan2(neckDirection.x, neckDirection.z) * Mathf.Rad2Deg;

        // local rotation around chosen axis (Y)
        Quaternion targetRotation = neckBone.localRotation * Quaternion.AngleAxis(neckFollowRotationAngle, neckRotationAxis);
        return targetRotation;
    }
    private Quaternion CalculateLocalHeadLookAtTarget()
    {
        Vector3 headDirection = headBone.InverseTransformPoint(FocusedObject.transform.position);
        float headFollowRotationAngle = Mathf.Atan2(headDirection.z, headDirection.y) * Mathf.Rad2Deg;

        Quaternion targetRotation = headBone.localRotation * Quaternion.AngleAxis(headFollowRotationAngle, headRotationAxis);
        return targetRotation;
    }

    private IEnumerator CheckIfFocusedInBounds()
    {
        Ray visionRay;

        while(true)
        {
            visionRay = new Ray(headBone.position, FocusedObject.transform.position - headBone.position);

            if (Physics.Raycast(visionRay, detectionRange, detectionLayer) == false ||
                Transf3D.CalculateAngle(neckBone.localRotation, restNeckRotation, Vector3.one) > neckBoneBoundMax ||
                Transf3D.CalculateAngle(headBone.localRotation, restHeadRotation, Vector3.one) > headBoneBoundMax)
            {
                State = TrackerState.Lost;
                StateChanged = true;
            }

            yield return new WaitForSeconds(checkInBoundsInterval);
        }
    }

    #endregion

    /*#region area_objects_detection

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == detectionLayer)
        {
            objectsInAreaSet.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == detectionLayer)
        {
            objectsInAreaSet.Remove(other.gameObject);
        }
    }

    #endregion*/
}
