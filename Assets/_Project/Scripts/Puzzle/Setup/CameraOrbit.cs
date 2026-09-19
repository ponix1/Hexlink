using UnityEngine;
using UnityEngine.EventSystems;

public class CameraOrbit : MonoBehaviour
{
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private KeyCode recenterKey = KeyCode.F;
    [SerializeField] private float screenShift = 0.3f;
    [SerializeField] private float zoomSpeed = 8f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 30f;

    private float currentHorizontalAngle;
    private float currentVerticalAngle;
    private Camera childCamera;

    private void Start()
    {
        currentHorizontalAngle = transform.eulerAngles.y;
        currentVerticalAngle = transform.eulerAngles.x;

        // The rig stays at the board centre so orbiting stays centred; the "board higher
        // on screen" framing is done by skewing the projection matrix instead - it is
        // independent of camera rotation and distance.
        childCamera = GetComponentInChildren<Camera>();
        if (childCamera != null)
        {
            childCamera.transform.LookAt(transform);
            Matrix4x4 projection = childCamera.projectionMatrix;
            projection[1, 2] -= screenShift;
            childCamera.projectionMatrix = projection;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(recenterKey))
        {
            Vector3 pos = transform.position;
            pos.x = boardCenter.x;
            pos.z = boardCenter.z;
            transform.position = pos;
        }

        Zoom();

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

            currentHorizontalAngle += mouseX * GameOptions.OrbitSpeed;
            currentVerticalAngle -= mouseY * GameOptions.OrbitSpeed;

            transform.eulerAngles = new Vector3(currentVerticalAngle, currentHorizontalAngle, 0f);
        }
    }

    private void Zoom()
    {
        if (childCamera == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        // Scrolling over UI panels (palette, info tab) should drive those, not the board.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Transform cameraTransform = childCamera.transform;
        float distance = Vector3.Distance(transform.position, cameraTransform.position);
        float target = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        cameraTransform.position = transform.position - cameraTransform.forward * target;
    }

    private void Pan()
    {
        if (childCamera == null) return;

        Vector3 forward = childCamera.transform.forward;
        Vector3 right = childCamera.transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        float zoomFactor = Vector3.Distance(transform.position, childCamera.transform.position);

        transform.position += (-right * mouseX - forward * mouseY) * (GameOptions.PanSpeed * zoomFactor);
    }
}