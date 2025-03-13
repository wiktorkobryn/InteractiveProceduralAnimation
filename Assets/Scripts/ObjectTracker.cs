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
    Lost,
    Reset
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
    public bool StateChanged { get; private set; } = false;

    public Transform neckBone, headBone;
    public Vector3 neckRotationAxis = new Vector3(0, 1, 0);
    public Vector3 headRotationAxis = new Vector3(1, 0, 0);
    private Quaternion restNeckRotation, restHeadRotation;

    // search state
    public float sideSearchDuration = 5.0f, sideSearchBound = 45.0f;
    public string detectionLayerName = "Detectable";
    private LayerMask detectionLayer;
    //private HashSet<GameObject> objectsInAreaSet = new HashSet<GameObject>();
    private Ray visionRay;
    public GameObject FocusedObject { get; private set; }
    public float refreshSearchInterval = 0.3f;
    public float detectionRange = 10.0f;

    // reset state
    public float resetDuration = 2.0f, timeToBeLost = 3.0f;

    private void OnValidate()
    {
        Vector3.Normalize(neckRotationAxis);
        detectionLayer = LayerMask.GetMask(detectionLayerName);
    }

    public void Start() 
    {
        restNeckRotation = neckBone.localRotation;
        restHeadRotation = headBone.localRotation;

        State = TrackerState.Search;
        StateChanged = true;
    }

    private void FixedUpdate()
    {
        // only visible in editor
        Debug.DrawRay(headBone.position, headBone.TransformDirection(Vector3.up) * detectionRange, Color.blue);

        if (StateChanged)
        {
            switch(State)
            {
                case TrackerState.Search:
                    StartCoroutine(IdleSearchAnimation());
                    StartCoroutine(IdleSearchForTarget());
                    stateIndicator.ChangeSprite((int)TrackerState.Search);
                    break;
                case TrackerState.Detected:
                    StartCoroutine(FollowTarget());
                    stateIndicator.ChangeSprite((int)TrackerState.Detected);
                    break;
                default:
                    StartCoroutine(ResetRotationsAnimation());
                    stateIndicator.ChangeSprite((int)TrackerState.Lost);
                    break;
            }

            StateChanged = false;
        }
    }

    #region animation_states

    private IEnumerator RotateOverTime(Transform bone, float durarion, Quaternion startRotation, Quaternion endRotation)
    {
        float elapsedTime = 0.0f;

        while (elapsedTime < durarion)
        {
            bone.transform.localRotation = Quaternion.Slerp(startRotation, endRotation, elapsedTime / durarion);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator IdleSearchAnimation()
    {
        Quaternion endRotation, startRotation;

        // first quarter
        endRotation = restNeckRotation * Quaternion.AngleAxis(sideSearchBound, neckRotationAxis);
        startRotation = restNeckRotation;

        // synchronous coroutine
        yield return StartCoroutine(RotateOverTime(neckBone, sideSearchDuration / 2.0f, startRotation, endRotation));

        startRotation = endRotation;
        endRotation = restNeckRotation * Quaternion.AngleAxis(-sideSearchBound, neckRotationAxis);


        // repeating idle movement
        while (State == TrackerState.Search)
        {
            yield return StartCoroutine(RotateOverTime(neckBone, sideSearchDuration, startRotation, endRotation));

            // swap with tuple
            (startRotation, endRotation) = (endRotation, startRotation);
        }
    }

    private IEnumerator IdleSearchForTarget()
    {
        RaycastHit hit;

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

        StopAllCoroutines();
    }

    private IEnumerator ResetRotationsAnimation()
    {
        // wait for object to return in area
        yield return new WaitForSecondsRealtime(timeToBeLost);

        // waiting for rotations to finish
        StartCoroutine(RotateOverTime(headBone, resetDuration, headBone.localRotation, restHeadRotation));
        yield return StartCoroutine(RotateOverTime(neckBone, resetDuration, neckBone.localRotation, restNeckRotation));

        // additional time spacing
        yield return new WaitForSecondsRealtime(0.5f);

        State = TrackerState.Search;
        StateChanged = true;
    }

    private bool IsInBounds360(float axisValue, float boundPositive)
    {
        if (boundPositive < 0)
            boundPositive *= -1;

        // euler angles have values [0;360)
        if ((axisValue < 180.0f && axisValue < sideSearchBound) || 
            (axisValue > 180.0f && axisValue > 360.0f - sideSearchBound))
            return true;

        return false;
    }

    private IEnumerator FollowTarget()
    {
        while (true)
        {
            //Coroutine neckMovement = StartCoroutine(RotateOverTime(neckBone, refreshSearchInterval, neckBone.localRotation, CalculateLocalNeckLookAtTarget()));
            Coroutine headMovement = StartCoroutine(RotateOverTime(headBone, refreshSearchInterval, headBone.localRotation, CalculateLocalHeadLookAtTarget()));

            // resume after parallel coroutines finish
            //yield return neckMovement;
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
