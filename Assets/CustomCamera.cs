using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomCamera : MonoBehaviour
{
    public Vector3 position;
    public Vector3 forward;
    public Vector3 up;
    public Vector3 right;
    public float fieldOfView;

    public CustomCamera(Vector3 position, Vector3 forward, float fieldOfView)
    {
        this.position = position;
        this.forward = forward;
        this.fieldOfView = fieldOfView;
        this.right = Vector3.Cross(Vector3.up, forward).normalized;
        this.up = Vector3.Cross(forward, right).normalized;
    }

    public void Rotate(Vector3 axis, float angle)
    {
        forward = Quaternion.AngleAxis(angle, axis) * forward;
        right = Vector3.Cross(Vector3.up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;
    }
}

class MathHelper
{
    /// <summary>
    /// Converts degrees to radians
    /// </summary>
    /// <param name="degrees"></param>
    /// <returns>float, in radians</returns>
    public static float Deg2Rad(float degrees)
    {
        return degrees * Mathf.Deg2Rad;
    }
}
