using System.Collections;
using UnityEngine;

public class Piston : MonoBehaviour
{
    public Transform arm;
    public float pushDistance = 1.0f;
    [Range(0.01f, 2.0f)]
    public float moveDuration = 1.0f;
    public KeyCode pushKey = KeyCode.P;

    private bool isMoving = false;
    private Vector3 armRestPose, armTargetPose;
    public Vector3 axisToPush = new Vector3(1, 0, 0);

    private void OnValidate()
    {
        axisToPush = axisToPush.normalized;
    }

    private void Start()
    {
        armRestPose = arm.localPosition;
        armTargetPose = arm.localPosition + axisToPush * pushDistance;

        StartCoroutine(PushDetection());
    }

    private IEnumerator PushDetection()
    {
        Coroutine pushing;

        while(true)
        {
            if (isMoving == false && Input.GetKey(pushKey))
            {
                pushing = StartCoroutine(Transf3D.MoveOverTimeLinear(arm, moveDuration, armRestPose, armTargetPose, false, false));
                yield return pushing;
                (armRestPose, armTargetPose) = (armTargetPose, armRestPose);
            }

            yield return new WaitForEndOfFrame();
        }
    }
}
