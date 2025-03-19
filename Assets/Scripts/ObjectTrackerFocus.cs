using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class ObjectTrackerFocus : ObjectTrackerSearch
{
    private HashSet<GameObject> objectsInAreaSet = new HashSet<GameObject>();

    public override void Activate()
    {
        if (isActive != isActiveBeforeChange)
        {
            if (isActive)
                State = TrackerState.Standby;
            else
                State = TrackerState.Off;

            isActiveBeforeChange = isActive;
            StateChanged = true;
        }
    }

    protected override void Start()
    {
        base.Start();
        if (isActive)
            State = TrackerState.Standby;
    }

    protected override void DebugDrawRays()
    {
        if(FocusedObject != null)
            Debug.DrawRay(headBone.position, (FocusedObject.transform.position - headBone.position).normalized * detectionRange, Color.cyan);
    }

    protected override void OnTrackerStateChanged()
    {
        switch (State)
        {
            case TrackerState.Standby:
                StartCoroutine(IdleSearchForTarget());
                break;
            case TrackerState.Detected:
                StartCoroutine(FollowTarget());
                StartCoroutine(CheckIfFocusedInBounds());
                break;
            case TrackerState.Lost:
                StartCoroutine(ResetRotationsAnimation(false, TrackerState.Standby));
                StartCoroutine(IdleSearchForTarget());
                break;
            default:
            case TrackerState.Off:
                StartCoroutine(ResetRotationsAnimation(false));
                break;
        }
    }

    protected override IEnumerator IdleSearchForTarget()
    {
        while (true)
        {
            FocusedObject = FindClosestReachableObject();

            if (FocusedObject != null)
                break;

            // WaitForSeconds instead of WaitUntil for optimization
            yield return new WaitForSecondsRealtime(refreshSearchInterval);
        }

        Debug.Log("detected: " + FocusedObject);
        State = TrackerState.Detected;
        StateChanged = true;
    }

    protected override IEnumerator CheckIfFocusedInBounds()
    {
        while (true)
        {
            // refresh closest object
            FocusedObject = FindClosestReachableObject();

            if (FocusedObject == null)
            {
                Debug.Log("target lost: no reachable objects in area");
                break;
            }

            yield return new WaitForSeconds(checkInBoundsInterval);
        }

        State = TrackerState.Lost;
        StateChanged = true;
    }

    #region area_objects_detection

    private GameObject FindClosestReachableObject()
    {
        if (objectsInAreaSet.Count > 0)
        {
            RaycastHit hit;
            Ray visionRay;
            bool isHit;

            float minDistance = -1.0f;
            GameObject minDistanceObj = null;

            float length;

            foreach (GameObject obj in objectsInAreaSet)
            {
                length = (headBoneEnd.position - obj.transform.position).magnitude;

                if(length < minDistance || minDistanceObj == null)
                {
                    visionRay = new Ray(headBone.position, obj.transform.position - headBone.position);
                    isHit = Physics.Raycast(visionRay, out hit, detectionRange);

                    if(isHit && hit.collider.gameObject == obj)
                    {
                        minDistanceObj = obj;
                        minDistance = length;
                    }
                }
            }

            return minDistanceObj;
        }
            
        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("enter: " + other.gameObject.name);

        if (other.gameObject.layer == LayerMask.NameToLayer(detectionLayerName))
        {
            objectsInAreaSet.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(detectionLayerName))
        {
            objectsInAreaSet.Remove(other.gameObject);
        }
    }

    #endregion
}
