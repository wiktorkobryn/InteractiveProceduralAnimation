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
    Move
}

[Serializable]
public class MovableIKBone
{
    public Transform targetIK, homeMarker;
    public bool CanMove { get; set; } = false;
}

public class ProceduralWalker : MonoBehaviour, IObservable
{
    public WalkerState State { get; private set; }
    public bool StateChanged { get; protected set; } = false;

    public bool isActive = true;
    protected bool isActiveBeforeChange = true;
    public List<GameObject> observers = new List<GameObject>();

    public Transform bodyBone;
    public Vector3 idleAnimationAxis = new Vector3(0, 0, 1);
    public float idleMovementAmplitude = 0.3f,
                 idleMovementFrequency = 1.5f;

    public List<MovableIKBone> leftLegs, rightLegs;

    private List<MovableIKBone> legsMovingFirst, legsMovingSecond;
    private int firstPlacedCount = 0, secondPlacedCount = 0;

    public float maxLegHomeDistance = 0.6f, stepDuration = 0.5f;

    [Range(0.0f, 0.99f)]
    public float legOvershootFactor = 0.5f;
    public float startMovingDelay = 0.1f;

    

    protected void Start()
    {
        Activate();
        if (isActive)
            State = WalkerState.Move;
            //State = WalkerState.Idle;
        else
            State = WalkerState.Off;

        StateChanged = true;

        legsMovingFirst = new List<MovableIKBone>();
        legsMovingSecond = new List<MovableIKBone>();
    }

    protected void OnValidate()
    {
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
            StateChanged = true;
        }
    }

    protected void OnStateChanged()
    {
        switch(State)
        {
            default:
            case WalkerState.Off:
                break;
            case WalkerState.Idle:
                StartCoroutine(Transf3D.IdleFloatConstant(bodyBone, idleAnimationAxis, idleMovementFrequency, idleMovementAmplitude));
                break;
            case WalkerState.Move:
                // delay for home positions to settle on the ground
                StartCoroutine(StartWalkingAfterDelay());
                break;
        }
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

    private void FixedUpdate()
    {
        if(StateChanged)
        {
            StopAllCoroutines();
            OnStateChanged();

            NotifyObservers();
            StateChanged = false;
        }
    }

    #region animations

    protected IEnumerator StartWalkingAfterDelay()
    {
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

        while(true)
        {
            yield return new WaitUntil(() => bone.CanMove);

            distance = Transf3D.GlobalDistance(bone.homeMarker, bone.targetIK);

            if (distance > maxLegHomeDistance)
            {
                endDirection = (bone.homeMarker.position - bone.targetIK.position).normalized;
                endPosition = bone.homeMarker.position + endDirection * (distance * legOvershootFactor);
                Transf3D.PositionOnTheGround(ref endPosition, 1.5f);

                //movement = StartCoroutine(Transf3D.MoveOverTimeLinear(bone.targetIK, stepDuration, bone.targetIK.position, endPosition));
                //movement = StartCoroutine(Transf3D.MoveOverTimeSpherical(bone.targetIK, stepDuration, bone.targetIK.position, endPosition));
                movement = StartCoroutine(Transf3D.MoveOverTimeQuadratic(bone.targetIK, stepDuration, bone.targetIK.position, endPosition));

                yield return movement;
                bone.CanMove = false;

                if (legsMovingFirst.Contains(bone))
                    firstPlacedCount++;
                else
                    secondPlacedCount++;
            }
        }
    }

    #endregion

    #region observers

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
