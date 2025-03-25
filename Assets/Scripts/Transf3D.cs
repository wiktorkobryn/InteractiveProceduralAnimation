using System;
using System.Collections;
using UnityEngine;

public static class Transf3D
{
    public static IEnumerator RotateOverTime(Transform bone, float durarion, Quaternion startRotation, Quaternion endRotation, bool easingInOut = false)
    {
        float elapsedTime = 0.0f;
        float t = 0.0f;

        while (elapsedTime < durarion)
        {
            t = elapsedTime / durarion;

            if(easingInOut)
                t = Mathf.SmoothStep(0.0f, 1.0f, t);

            bone.transform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            elapsedTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
    }

    public static IEnumerator IdleFloatConstant(Transform bone, Vector3 axis, float frequency, float amplitude)
    {
        Vector3 restPosition = bone.localPosition;
        float moveOffset;

        while(true)
        {
            moveOffset = Mathf.Sin(Time.time * Mathf.PI * 2 * frequency) * amplitude;
            bone.localPosition = restPosition + axis * moveOffset;

            yield return new WaitForEndOfFrame();
        }
    }

    public static float CalculateAngleAbs(Quaternion currentRotation, Quaternion baseRotation, Vector3 axis)
    {
        return Quaternion.Angle(baseRotation, Quaternion.Euler(currentRotation.eulerAngles.x * axis.x,
                                                               currentRotation.eulerAngles.y * axis.y,
                                                               currentRotation.eulerAngles.z * axis.z));
    }

    public static float CalculateAngleSigned(Quaternion currentRotation, Quaternion baseRotation, Vector3 axis)
    {
        // quaternions cannot be substracted - multiplication by inversion
        Quaternion deltaRotation = Quaternion.Inverse(baseRotation) * currentRotation;

        // rotation conversion to angle [180;0] + direction axis
        deltaRotation.ToAngleAxis(out float angle, out Vector3 rotationAxis);
        if (Vector3.Dot(rotationAxis, axis) < 0)
            angle = -angle;

        return angle;
    }
}
