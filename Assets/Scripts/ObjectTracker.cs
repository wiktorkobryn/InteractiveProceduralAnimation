using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
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
    private Quaternion restNeckRotation, restHeadRotation;

    // search state
    public float sideSearchDuration = 5.0f, sideSearchBound = 45.0f;

    // reset state
    public float resetDuration = 2.0f, timeToReset = 3.0f;

    private void OnValidate()
    {
        Vector3.Normalize(neckRotationAxis);
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
        if(StateChanged)
        {
            switch(State)
            {
                case TrackerState.Search:
                    StartCoroutine(IdleSearchAnimation());
                    StartCoroutine(SearchForTarget());
                    stateIndicator.ChangeSprite((int)TrackerState.Search);
                    break;
                default:
                    StartCoroutine(ResetRotationsAnimation());
                    stateIndicator.ChangeSprite((int)TrackerState.Lost);
                    break;
            }

            StateChanged = false;
        }
    }

    #region animation

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

    private IEnumerator SearchForTarget()
    {
        // tu logika wyszukiwania obiektu + raycast
        //
        yield return new WaitForSecondsRealtime(8.0f);
        State = TrackerState.Lost;
        StateChanged = true;
        //

        StopAllCoroutines();
        yield return null;
    }

    private IEnumerator ResetRotationsAnimation()
    {
        // wait for object to return in area
        yield return new WaitForSecondsRealtime(timeToReset);

        // waiting for rotations to finish
        StartCoroutine(RotateOverTime(headBone, resetDuration, headBone.localRotation, restHeadRotation));
        yield return StartCoroutine(RotateOverTime(neckBone, resetDuration, neckBone.localRotation, restNeckRotation));

        // additional time spacing
        yield return new WaitForSecondsRealtime(0.5f);

        State = TrackerState.Search;
        StateChanged = true;
    }

    #endregion
}
