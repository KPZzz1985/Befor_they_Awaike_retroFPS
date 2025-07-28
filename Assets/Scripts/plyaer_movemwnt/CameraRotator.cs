using UnityEngine;

public class CameraRotator : MonoBehaviour
{
    public Transform targetTransform;
    public float horizontalSpeed = 2f;
    public float verticalSpeed = 2f;
    public float maxVerticalAngle = 60f;
    public bool invertMouse = false;

    public float _xRotation = 0f;
    public float _yRotation = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * horizontalSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * verticalSpeed * (invertMouse ? -1 : 1);

        _xRotation += mouseX;
        _yRotation += mouseY;
        _yRotation = Mathf.Clamp(_yRotation, -maxVerticalAngle, maxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(_yRotation, _xRotation, 0);
        targetTransform.rotation = rotation;
    }
}