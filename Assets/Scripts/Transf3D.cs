using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.Splines;

public static class Transf3D
{
    #region cotoutines

    /// <summary> local rotation </summary>
    public static IEnumerator RotateOverTime(Transform bone, float duration, Quaternion startRotation, Quaternion endRotation, bool easingInOut = false)
    {
        float elapsedTime = 0.0f;
        float t = 0.0f;

        while (elapsedTime < duration)
        {
            t = elapsedTime / duration;

            if (easingInOut)
                t = Mathf.SmoothStep(0.0f, 1.0f, t);

            bone.transform.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
            elapsedTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary> local position </summary>
    public static IEnumerator IdleFloatConstant(Transform bone, Vector3 axis, float frequency, float amplitude)
    {
        Vector3 restPosition = bone.localPosition;
        float moveOffset;

        while (true)
        {
            moveOffset = Mathf.Sin(Time.time * Mathf.PI * 2 * frequency) * amplitude;
            bone.localPosition = restPosition + axis * moveOffset;

            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary> global position </summary>
    public static IEnumerator MoveOverTimeLinear(Transform bone, float duration, Vector3 startPosition, Vector3 endPosition, bool easingInOut = false)
    {
        float elapsedTime = 0.0f;
        float t = 0.0f;

        while (elapsedTime < duration)
        {
            t = elapsedTime / duration;

            if (easingInOut)
                t = Mathf.SmoothStep(0.0f, 1.0f, t);

            bone.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            elapsedTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary> global position </summary>
    public static IEnumerator MoveOverTimeQuadratic(Transform bone, float duration, Vector3 startPosition, Vector3 endPosition, bool easingInOut = false)
    {
        Vector3 centerPosition = (endPosition + startPosition) * 0.5f;
        centerPosition += (Vector3.up * Vector3.Distance(startPosition, endPosition));

        float elapsedTime = 0.0f;
        float t = 0.0f;

        while (elapsedTime < duration)
        {
            t = elapsedTime / duration;

            if (easingInOut)
                t = Mathf.SmoothStep(0.0f, 1.0f, t);
            
            // quadratic bezier curve
            bone.position = Vector3.Lerp(
                                    Vector3.Lerp(startPosition, centerPosition, t),
                                    Vector3.Lerp(centerPosition, endPosition, t), t);

            elapsedTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary> global position </summary>
    public static IEnumerator MoveOverTimeSpherical(Transform bone, float duration, Vector3 startPosition, Vector3 endPosition, bool easingInOut = false)
    {
        Vector3 center = (endPosition + startPosition) * 0.5f;
        center -= new Vector3(0, Vector3.Distance(startPosition, endPosition) * 0.5f, 0);

        Vector3 startDirection = startPosition - center;
        Vector3 endDirection = endPosition - center;

        float elapsedTime = 0.0f;
        float t = 0.0f;

        while (elapsedTime < duration)
        {
            t = elapsedTime / duration;

            if (easingInOut)
                t = Mathf.SmoothStep(1.0f, 1.0f, t);

            bone.position = Vector3.Slerp(startDirection, endDirection, t);
            bone.position += center;

            elapsedTime += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
    }

    #endregion

    #region methods

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

    public static bool PositionOnTheGround(ref Vector3 globalPoint, float projectionHeight)
    {
        Vector3 raycastStart = globalPoint + (Vector3.up * projectionHeight);
        RaycastHit hit;

        if (Physics.Raycast(raycastStart, Vector3.down, out hit, Mathf.Infinity))
        {
            globalPoint.y = hit.point.y;
            return true;
        }

        return false;
    }

    public static Vector3 AveragePosition(params Vector3[] points)
    {
        try
        {
            if (points == null)
                throw new NullReferenceException();
            else if (points.Length < 1)
                throw new ArgumentException("points.Length < 1");

            Vector3 sum = Vector3.zero;

            foreach (Vector3 p in points)
            {
                sum += p;
            }

            return sum / points.Length;
        }
        catch(Exception ex)
        {
            Debug.LogError(ex.Message);
            return Vector3.zero;
        }
    }

    public static Vector3 AveragePosition(IEnumerable<MovableIKBone> bones)
    {
        try
        {
            if (bones == null)
                throw new NullReferenceException();
            else if (bones.Count() < 1)
                throw new ArgumentException("points.Length < 1");

            Vector3 sum = Vector3.zero;

            foreach (MovableIKBone b in bones)
            {
                sum += b.targetIK.position;
            }

            return sum / bones.Count();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            return Vector3.zero;
        }
    }

    #endregion
}
