using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectTrackerSearch : MonoBehaviour, IObservable
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
    //private HashSet<GameObject> objectsInAreaSet = new HashSet<GameObject>();
    public GameObject FocusedObject { get; private set; }
    public float refreshSearchInterval = 0.3f, checkInBoundsInterval = 0.2f;
    public List<Transform> raycastPoints = new List<Transform>();

    [Header("State: Detected")]
    private bool outOfBoundsReached = false;

    [Header("State: Lost")]
    public float resetDuration = 2.0f;
    public float timeToBeLost = 3.0f;

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

        if (raycastPoints.Count < 1)
            raycastPoints = headBoneEnd.GetComponentsInChildren<Transform>().ToList();

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
                    break;
            }

            NotifyObservers();
            StateChanged = false;
        }
    }

    #region animation_states

    private void DebugDrawRays()
    {
        //Debug.DrawRay(headBone.position, headBone.TransformDirection(Vector3.up) * detectionRange, Color.blue);

        foreach(Transform p in raycastPoints)
        {
            Debug.DrawRay(headBoneEnd.position, (p.position - headBoneEnd.position).normalized * detectionRange, Color.red);
        }
    }

    private IEnumerator IdleSearchAnimation()
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

    private IEnumerator IdleSearchForTarget()
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

    private GameObject SearchWithRaycasts()
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

    private IEnumerator ResetRotationsAnimation()
    {
        // wait for object to return in area
        yield return new WaitForSecondsRealtime(timeToBeLost);

        // waiting for rotations to finish
        Coroutine headReset = StartCoroutine(Transf3D.RotateOverTime(headBone, resetDuration, headBone.localRotation, restHeadRotation, movementInOutEasing));
        Coroutine neckReset = StartCoroutine(Transf3D.RotateOverTime(neckBone, resetDuration, neckBone.localRotation, restNeckRotation, movementInOutEasing));

        yield return headReset;
        yield return neckReset;

        // additional time spacing before search
        yield return new WaitForSecondsRealtime(0.3f);

        State = TrackerState.Search;
        StateChanged = true;
    }

    private IEnumerator FollowTarget()
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

    private bool IsFocusedInVision()
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
