using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 5f;

    private float currentHorizontalAngle;
    private float currentVerticalAngle;

    private void Start()
    {
        currentHorizontalAngle = transform.eulerAngles.y;
        currentVerticalAngle = transform.eulerAngles.x;

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
            float mouseY = Input.GetAxis("Mouse Y");

            currentHorizontalAngle += mouseX * rotationSpeed;
            currentVerticalAngle -= mouseY * rotationSpeed;

            transform.eulerAngles = new Vector3(currentVerticalAngle, currentHorizontalAngle, 0f);
        }
    }
}