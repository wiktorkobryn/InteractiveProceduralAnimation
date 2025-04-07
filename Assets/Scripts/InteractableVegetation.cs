using UnityEngine;

public enum InteractableState
{
    Idle,
    Colliding
}

public class InteractableVegetation : MonoBehaviour
{
    public InteractableState State { get; protected set; }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
