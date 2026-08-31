using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float panSpeed = 0.002f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;

    private float currentHorizontalAngle;
    private float currentVerticalAngle;
    private Camera childCamera;

    private void Start()
    {
        currentHorizontalAngle = transform.eulerAngles.y;
        currentVerticalAngle = transform.eulerAngles.x;

        childCamera = GetComponentInChildren<Camera>();
        if (childCamera != null)
        {
            childCamera.transform.LookAt(transform);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            Vector3 pos = transform.position;
            pos.x = boardCenter.x;
            pos.z = boardCenter.z;
            transform.position = pos;
        }

        bool rightHeld = Input.GetMouseButton(1);

        if (rightHeld && Input.GetMouseButton(0))
        {
            Pan();
            return;
        }

        if (rightHeld)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentHorizontalAngle += mouseX * rotationSpeed;
            currentVerticalAngle -= mouseY * rotationSpeed;

            transform.eulerAngles = new Vector3(currentVerticalAngle, currentHorizontalAngle, 0f);
        }
    }

    private void Pan()
    {
        if (childCamera == null) return;

        Vector3 forward = childCamera.transform.forward;
        Vector3 right = childCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        float zoomFactor = Vector3.Distance(transform.position, childCamera.transform.position);

        transform.position += (-right * mouseX - forward * mouseY) * (panSpeed * zoomFactor);
    }
}