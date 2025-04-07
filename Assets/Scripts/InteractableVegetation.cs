using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InteractableState
{
    Idle,
    Colliding
}

public interface IStateMachine
{
    void OnStateChanged();
}

public class InteractableVegetation : MonoBehaviour, IObservable, IStateMachine
{
    public InteractableState State { get; protected set; }
    private bool stateChanged = false;

    protected GameObject collidingObject;
    [Header("General")]
    public LayerMask detectionLayer;

    [Header("Animation: Idle")]
    float randomizationFactor = 1.0f; // changes max bound: max

    [Header("Observable")]
    public List<GameObject> observers = new List<GameObject>();

    protected void Start()
    {
        State = InteractableState.Idle;
        stateChanged = true;
    }

    protected void Update()
    {
        if(stateChanged)
            OnStateChanged();
    }

    public void OnStateChanged()
    {
        StopAllCoroutines();

        switch (State)
        {
            case InteractableState.Idle:
                collidingObject = null;
                StartCoroutine(IdleAnimation());
                break;
            case InteractableState.Colliding:
                StartCoroutine(PushAwayFromCollided());
                break;
        }

        NotifyObservers();
        stateChanged = false;
    }

    private IEnumerator IdleAnimation()
    {
        yield return null;
    }

    private IEnumerator PushAwayFromCollided()
    {
        yield return null;
    }

    #region collision_detection

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == detectionLayer && collidingObject != other.gameObject)
        {
            collidingObject = other.gameObject;

            State = InteractableState.Colliding;
            stateChanged = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == detectionLayer && collidingObject == other.gameObject)
        {
            State = InteractableState.Idle;
            stateChanged = true;
        }
    }

    #endregion

    #region observable

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
