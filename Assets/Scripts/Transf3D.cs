using System;
using System.Collections;
using UnityEngine;

public static class Transf3D
{
    public static IEnumerator RotateOverTime(Transform bone, float durarion, Quaternion startRotation, Quaternion endRotation)
    {
        float elapsedTime = 0.0f;

        while (elapsedTime < durarion)
        {
            bone.transform.localRotation = Quaternion.Slerp(startRotation, endRotation, elapsedTime / durarion);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }

    public static float CalculateAngle(Quaternion currentRotation, Quaternion baseRotation, Vector3 axis)
    {
        return Quaternion.Angle(baseRotation, Quaternion.Euler(currentRotation.eulerAngles.x * axis.x,
                                                               currentRotation.eulerAngles.y * axis.y,
                                                               currentRotation.eulerAngles.z * axis.z));
    }
}
