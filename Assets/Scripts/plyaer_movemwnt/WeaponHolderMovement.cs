using UnityEngine;

public class WeaponHolderMovement : MonoBehaviour
{
    [Header("Player Controller")]
    public FirstPersonController_CC playerController;
    public SideLeaningFPSController leaningController;

    [Header("Movement Settings")]
    public float xOffset = 0.1f;
    public float yOffset = 0.1f;
    public float smoothTime = 0.1f;

    [Header("Leaning Swing Settings")]
    public float leanXOffset = 0.05f;
    public float leanYOffset = 0.05f;
    public float leanSmoothTime = 0.05f;

    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;

    void Start()
    {
        if (playerController == null)
        {
            Debug.LogWarning("Player Controller is not assigned. WeaponHolderMovement will not work.");
            enabled = false;
            return;
        }

        if (leaningController == null)
        {
            Debug.LogWarning("Leaning Controller is not assigned. Leaning effects will not be applied.");
        }

        originalPosition = transform.localPosition;
    }

    void Update()
    {
        UpdateTargetPosition();
        ApplySmoothedOffset();
    }

    private void UpdateTargetPosition()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical);
        direction.Normalize();

        targetPosition = originalPosition + new Vector3(direction.x * xOffset, direction.y * yOffset, direction.z * yOffset);

        // ƒобавление свинга при наклоне влево или вправо
        if (leaningController != null)
        {
            if (Input.GetKey(KeyCode.Q))
            {
                targetPosition = Vector3.Lerp(targetPosition, originalPosition + new Vector3(-leanXOffset, leanYOffset, 0f), leanSmoothTime);
            }
            else if (Input.GetKey(KeyCode.E))
            {
                targetPosition = Vector3.Lerp(targetPosition, originalPosition + new Vector3(leanXOffset, leanYOffset, 0f), leanSmoothTime);
            }
        }
    }

    private void ApplySmoothedOffset()
    {
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPosition, ref currentVelocity, smoothTime);
    }
}
