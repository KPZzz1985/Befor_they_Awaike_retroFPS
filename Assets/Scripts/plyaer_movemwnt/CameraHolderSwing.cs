using UnityEngine;

public class CameraHolderSwing : MonoBehaviour
{
    [Header("Swing Settings")]
    public CameraRotator cameraRotator;
    public float maxSwingAngle = 5f;
    public float swingSpeed = 5f;
    public bool invertSwingX = false;

    private Quaternion defaultRotation;
    private Vector2 previousRotation;

    private void Start()
    {
        defaultRotation = transform.localRotation;
        previousRotation = new Vector2(cameraRotator._xRotation, cameraRotator._yRotation);
    }

    private void Update()
    {
        Vector2 currentRotation = new Vector2(cameraRotator._xRotation, cameraRotator._yRotation);
        Vector2 deltaRotation = currentRotation - previousRotation;

        float xSwing = Mathf.Clamp(deltaRotation.y * maxSwingAngle, -maxSwingAngle, maxSwingAngle);
        float ySwing = Mathf.Clamp(deltaRotation.x * maxSwingAngle, -maxSwingAngle, maxSwingAngle);

        if (invertSwingX)
        {
            ySwing = -ySwing;
        }

        Quaternion targetRotation = defaultRotation * Quaternion.Euler(-xSwing, ySwing, 0);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * swingSpeed);

        previousRotation = currentRotation;
    }
}