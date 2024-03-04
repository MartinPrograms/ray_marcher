using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFovChanger : MonoBehaviour
{
    public float fov = 60.0f;
    public float change_speed;
    [SerializeField] private float current_fov;
    [SerializeField] private float target_fov;

    public CustomCamera camera;
    public ComputeShaderRenderer renderer;

    void Update()
    {
        if (camera == null)
        {
            return;
        }

        // Check for scroll wheel input to change the field of view
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            target_fov = Mathf.Clamp(fov - (scroll * 100), 1.0f, 100.0f);
        }

        float difference = Mathf.Abs(target_fov - current_fov);
        
        if (difference > 0.1f)
        {
            current_fov = EaseOut(current_fov, target_fov, change_speed * Time.deltaTime);
            camera.fieldOfView = current_fov;
            fov = current_fov;
            renderer.ResetAccumulation();
        }
    }

    private float EaseOut(float start, float end, float value)
    {
        value = Mathf.Clamp01(value);
        value = 1 - Mathf.Pow(1 - value, 3);
        return Mathf.Lerp(start, end, value);
    }
}
