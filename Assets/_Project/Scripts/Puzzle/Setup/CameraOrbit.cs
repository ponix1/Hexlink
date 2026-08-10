using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 5f;

    private void Start()
    {
        Camera childCamera = GetComponentInChildren<Camera>();
        if (childCamera != null)
        {
            childCamera.transform.LookAt(transform);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up, mouseX * rotationSpeed, Space.World);
        }
    }
}