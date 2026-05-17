using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform target;
    public float distance = 5f;
    public float sensitivity = 3f;

    float yaw;
    float pitch;

    void Update()
    {
        if (Input.GetMouseButton(1)) // Right button to move camera
        {
            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * sensitivity;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
        }
    }

    void LateUpdate()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 position = target.position - rotation * Vector3.forward * distance;

        transform.position = position;
        transform.LookAt(target);
    }
}
