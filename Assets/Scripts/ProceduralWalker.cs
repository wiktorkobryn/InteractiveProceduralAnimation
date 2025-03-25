using System;
using System.Collections.Generic;
using UnityEngine;

public enum WalkerState
{
    Off = 0,
    Idle
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

    protected void Start()
    {
        Activate();
        if (isActive)
            State = WalkerState.Idle;
        else
            State = WalkerState.Off;

        StateChanged = true;
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
        }
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
