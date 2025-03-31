using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

public enum WalkerState
{
    Off = 0,
    Idle,
    Move,
    MoveFast
}

[Serializable]
public class MovableIKBone
{
    public Transform targetIK, homeMarker;
    public bool CanMove { get; set; } = false;
}

public class ProceduralWalker : MonoBehaviour, IObserver<int>
{
    public WalkerState State { get; private set; } = WalkerState.Off;

    public Transform bodyBone, bodyBoneRest;
    public float maxBodyMoveDistance = 0.2f;

    public Vector3 idleAnimationAxis = new Vector3(0, 0, 1);
    public float idleMovementAmplitude = 0.3f,
                 idleMovementFrequency = 1.5f;

    public List<MovableIKBone> leftLegs, rightLegs;

    private List<MovableIKBone> legsMovingFirst, legsMovingSecond;
    private int firstPlacedCount = 0, secondPlacedCount = 0;

    public float maxLegHomeDistance = 0.6f, stepDuration = 0.5f, runStepMultiplier = 3.0f;
    private float currentMaxLegDistance = 1.0f;

    [Range(0.0f, 0.99f)]
    public float legOvershootFactor = 0.5f;
    public float startMovingDelay = 0.1f;

    public float updateBodyInterval = 0.2f;
    public float bodyOffsetY = 0.0f;
    public LayerMask layerToIgnore;

    private Coroutine idleAnimation = null, bodyMovement = null;


    protected void Start()
    {
        legsMovingFirst = new List<MovableIKBone>();
        legsMovingSecond = new List<MovableIKBone>();

        StartCoroutine(StartWalkingAfterDelay());
    }

    protected void OnStateChanged()
    {
        if (State == WalkerState.MoveFast)
        {
            currentMaxLegDistance = maxLegHomeDistance * runStepMultiplier;
        }
        else
        {
            StopBodyCoroutines();
            currentMaxLegDistance = maxLegHomeDistance;

            switch (State)
            {
                default:
                case WalkerState.Off:
                    break;
                case WalkerState.Idle:
                    idleAnimation = StartCoroutine(Transf3D.IdleFloatConstant(bodyBone, idleAnimationAxis, idleMovementFrequency, idleMovementAmplitude));
                    break;
                case WalkerState.Move:
                    bodyMovement = StartCoroutine(PositionBody());
                    //StartCoroutine(RotateBodyToPlane());
                    break;
            }
        }
    }

    private void StopBodyCoroutines()
    {
        if (bodyMovement != null)
            StopCoroutine(bodyMovement);
        if (idleAnimation != null)
            StopCoroutine(idleAnimation);
    }

    private void SetLegGroups()
    {
        legsMovingFirst.Clear();
        legsMovingSecond.Clear();

        for(int i = 0; i < rightLegs.Count; i++)
        {
            if (i % 2 == 0)
            {
                legsMovingFirst.Add(rightLegs[i]);
                legsMovingSecond.Add(leftLegs[i]);
            }      
            else
            {
                legsMovingFirst.Add(leftLegs[i]);
                legsMovingSecond.Add(rightLegs[i]);
            }
        }

        foreach (MovableIKBone bone in legsMovingFirst)
            bone.CanMove = true;
    }

    #region animations

    protected IEnumerator StartWalkingAfterDelay()
    {
        // delay for home positions to settle on the ground
        yield return new WaitForSecondsRealtime(startMovingDelay);

        firstPlacedCount = 0;
        secondPlacedCount = 0;
        SetLegGroups();

        foreach (MovableIKBone bone in legsMovingFirst.Concat(legsMovingSecond))
            StartCoroutine(MoveLeg(bone));

        while (true)
        {
            // legs moving first
            yield return new WaitUntil(() => firstPlacedCount >= legsMovingFirst.Count);
            firstPlacedCount = 0;
            foreach (MovableIKBone bone in legsMovingSecond)
                bone.CanMove = true;

            // legs moving second
            yield return new WaitUntil(() => secondPlacedCount >= legsMovingSecond.Count);
            secondPlacedCount = 0;
            foreach (MovableIKBone bone in legsMovingFirst)
                bone.CanMove = true;
        }
    }

    protected IEnumerator MoveLeg(MovableIKBone bone)
    {
        float distance;
        Coroutine movement;
        Vector3 endPosition, endDirection;

        PositionAnchor posLock = bone.targetIK.GetComponent<PositionAnchor>();
        posLock.Anchor = true;

        while (true)
        {
            yield return new WaitUntil(() => bone.CanMove);

            distance = Vector3.Distance(bone.homeMarker.position, bone.targetIK.position);

            if (distance > currentMaxLegDistance)
            {
                endDirection = (bone.homeMarker.position - bone.targetIK.position).normalized;
                endPosition = bone.homeMarker.position + endDirection * (distance * legOvershootFactor);
                Transf3D.PositionOnTheGround(ref endPosition, 1.5f);

                //movement = StartCoroutine(Transf3D.MoveOverTimeLinear(bone.targetIK, stepDuration, bone.targetIK.position, endPosition));
                //movement = StartCoroutine(Transf3D.MoveOverTimeSpherical(bone.targetIK, stepDuration, bone.targetIK.position, endPosition));

                posLock.Anchor = false;
                movement = StartCoroutine(Transf3D.MoveOverTimeQuadratic(bone.targetIK, stepDuration, bone.targetIK.position, endPosition));

                yield return movement;
                posLock.Anchor = true;
                bone.CanMove = false;

                if (legsMovingFirst.Contains(bone))
                    firstPlacedCount++;
                else
                    secondPlacedCount++;
            }

            // distance can be checked once per frame at most
            yield return new WaitForEndOfFrame();
        }
    }

    protected IEnumerator PositionBody()
    {
        while(true)
        {
            yield return new WaitForFixedUpdate();

            Vector3 targetPos = Transf3D.AveragePosition(leftLegs.Concat(rightLegs)) + new Vector3(0, bodyOffsetY, 0);
            targetPos = Transf3D.AveragePosition(targetPos, bodyBoneRest.position);
            bodyBone.position = targetPos;
        }
    }

    protected IEnumerator RotateBodyToPlane()
    {
        RaycastHit hit;
        Quaternion targetRotation;

        while(true)
        {
            if (Physics.Raycast(bodyBoneRest.position, Vector3.down, out hit, Mathf.Infinity, ~layerToIgnore))
            {
                targetRotation = Quaternion.FromToRotation(-(bodyBone.localRotation * Vector3.down), hit.normal) * bodyBone.localRotation;
                //yield return StartCoroutine(Transf3D.RotateOverTime(bodyBone, updateBodyInterval, bodyBone.localRotation, targetRotation, false, true));

                //
                bodyBone.localRotation = targetRotation;
                yield return new WaitForSecondsRealtime(updateBodyInterval);

            }
            else
                yield return new WaitForSecondsRealtime(updateBodyInterval);
        }
    }

    #endregion

    #region observer

    public void OnValueChanged(int value)
    {
        State = (WalkerState)value;
        OnStateChanged();
    }

    #endregion
}
