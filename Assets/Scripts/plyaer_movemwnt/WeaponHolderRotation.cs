using UnityEngine;

public class WeaponHolderRotation : MonoBehaviour
{
    [Header("Camera Settings")]
    public CameraRotator cameraRotator;

    [Header("Rotation Settings")]
    public float rotationOffsetY = 2f;
    public float rotationOffsetX = 2f;
    public float maxRotationX = 5f;
    public float maxRotationY = 5f;
    public float smoothTime = 0.1f;

    private Quaternion originalRotation;
    private Quaternion targetRotation;
    private Vector3 currentVelocity;

    void Start()
    {
        if (cameraRotator == null)
        {
            Debug.LogWarning("CameraRotator script is not assigned. WeaponHolderRotation will not work.");
            enabled = false;
            return;
        }

        originalRotation = transform.localRotation;
    }

    void Update()
    {
        UpdateTargetRotation();
        ApplySmoothedRotation();
    }

    private void UpdateTargetRotation()
    {
        float mouseY = cameraRotator._yRotation;
        float mouseX = cameraRotator._xRotation;

        mouseY = (mouseY > 180) ? mouseY - 360 : mouseY;
        mouseX = (mouseX > 180) ? mouseX - 360 : mouseX;

        float targetY = Mathf.Clamp(-mouseX * rotationOffsetY / 360, -maxRotationY, maxRotationY);
        float targetX = Mathf.Clamp(mouseY * rotationOffsetX / 360, -maxRotationX, maxRotationX);

        targetRotation = Quaternion.Euler(originalRotation.eulerAngles + new Vector3(targetX, targetY, 0));
    }

    private void ApplySmoothedRotation()
    {
        transform.localRotation = Quaternion.Euler(Vector3.SmoothDamp(transform.localRotation.eulerAngles, targetRotation.eulerAngles, ref currentVelocity, smoothTime));
    }
}