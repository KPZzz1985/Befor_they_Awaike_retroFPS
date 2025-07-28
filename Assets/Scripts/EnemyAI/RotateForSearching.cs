using UnityEngine;

public class RotateForSearching : MonoBehaviour
{
    [SerializeField] private GameObject head;
    [SerializeField] private Vector2 xRotationLimit = new Vector2(-45f, 45f);
    [SerializeField] private Vector2 yRotationLimit = new Vector2(-45f, 45f);
    [SerializeField] private float rotationSpeed = 3f;

    public LookRegistrator lookRegistrator;
    private Vector3 currentRotationSpeed;

    private void Start()
    {
        SetRandomRotationSpeed();
    }

    private void Update()
    {
        if (!lookRegistrator.PlayerSeen)
        {
            // Rotate head
            head.transform.Rotate(currentRotationSpeed * Time.deltaTime);

            // Apply rotation limits and reset rotation speed if reached
            Vector3 currentEulerAngles = head.transform.localEulerAngles;

            if (currentEulerAngles.x > 180) currentEulerAngles.x -= 360;
            if (currentEulerAngles.y > 180) currentEulerAngles.y -= 360;

            bool resetXSpeed = false;
            bool resetYSpeed = false;

            if (currentEulerAngles.x < xRotationLimit.x || currentEulerAngles.x > xRotationLimit.y)
            {
                currentEulerAngles.x = Mathf.Clamp(currentEulerAngles.x, xRotationLimit.x, xRotationLimit.y);
                resetXSpeed = true;
            }

            if (currentEulerAngles.y < yRotationLimit.x || currentEulerAngles.y > yRotationLimit.y)
            {
                currentEulerAngles.y = Mathf.Clamp(currentEulerAngles.y, yRotationLimit.x, yRotationLimit.y);
                resetYSpeed = true;
            }

            head.transform.localEulerAngles = currentEulerAngles;

            if (resetXSpeed || resetYSpeed)
            {
                SetRandomRotationSpeed(resetXSpeed, resetYSpeed);
            }
        }
        else
        {
            // Disable RotateForSearching script after player is detected
            this.enabled = false;
        }
    }

    private void SetRandomRotationSpeed(bool resetXSpeed = true, bool resetYSpeed = true)
    {
        if (resetXSpeed)
        {
            currentRotationSpeed.x = Random.Range(-rotationSpeed, rotationSpeed);
        }
        if (resetYSpeed)
        {
            currentRotationSpeed.y = Random.Range(-rotationSpeed, rotationSpeed);
        }
    }
}