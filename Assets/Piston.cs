using System.Collections;
using UnityEngine;

public class Piston : MonoBehaviour
{
    private bool isMoving = false;
    public Transform arm;
    public float pushDistance = 1.0f, moveDuration = 1.0f;
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
            if (isMoving == false && Input.GetKey(KeyCode.P))
            {
                pushing = StartCoroutine(Transf3D.MoveOverTimeLinear(arm, moveDuration, armRestPose, armTargetPose, false, false));
                yield return pushing;
                (armRestPose, armTargetPose) = (armTargetPose, armRestPose);
            }

            yield return new WaitForEndOfFrame();
        }
    }
}
