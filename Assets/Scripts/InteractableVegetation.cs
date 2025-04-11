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
    public String detectionLayerName = "Detectable";
    public Transform bodyBone, lookAtRest;
    private float bodyBoneRestRotY = 0.0f;
    [Range(0.01f, 30.0f)]
    public float swayWeight = 1.0f;
    public float maxSwayDistance = 0.6f;

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
                StartCoroutine(PushAway());
                break;
        }

        NotifyObservers();
        stateChanged = false;
    }

    private IEnumerator IdleAnimation()
    {
        bodyBone.LookAt(lookAtRest);
        bodyBone.Rotate(90, 0, 0);

        yield return new WaitForEndOfFrame();
    }

    private IEnumerator PushAway()
    {
        Vector3 direction, target;
        float distance, swayFactor;

        while (true)
        {
            direction = lookAtRest.position - collidingObject.transform.position;
            distance = direction.magnitude;

            // % value of distance between [0; maxSwayDistance]
            swayFactor = Mathf.InverseLerp(maxSwayDistance, 0.0f, distance);

            target = lookAtRest.position + direction.normalized * swayFactor * maxSwayDistance * swayWeight;

            bodyBone.LookAt(target);
            //bodyBone.Rotate(90, 0, 0);

            yield return new WaitForEndOfFrame();
        }
    }

    #region collision_detection

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(detectionLayerName) && collidingObject != other.gameObject)
        {
            collidingObject = other.gameObject;

            State = InteractableState.Colliding;
            stateChanged = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(detectionLayerName) && collidingObject == other.gameObject)
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
