using UnityEngine;

public class CameraSwing : MonoBehaviour
{
    [Header("Swing Settings")]
    public float swingAmount = 0.05f;
    public float swingSpeed = 2f;
    public float jumpSwingAmount = 0.1f;

    [Header("Lissajous Curve Settings")]
    public float lissajousA = 1f;
    public float lissajousB = 1f;
    public float lissajousDelta = Mathf.PI / 2;

    [Header("Player Controller")]
    public FirstPersonController_CC playerController;

    [Header("Swing Curves")]
    public AnimationCurve swingCurveX;
    public AnimationCurve swingCurveY;

    private Vector3 initialPosition;

    void Start()
    {
        if (playerController == null)
        {
            Debug.LogWarning("Player Controller is not assigned. CameraSwing will not work.");
            enabled = false;
            return;
        }

        initialPosition = transform.localPosition;

        if (swingCurveX == null || swingCurveX.length == 0)
        {
            swingCurveX = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        if (swingCurveY == null || swingCurveY.length == 0)
        {
            swingCurveY = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float movementAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));

        float lissajousX = lissajousA * Mathf.Sin(swingSpeed * Time.time + lissajousDelta);
        float lissajousY = lissajousB * Mathf.Sin(swingSpeed * Time.time);

        Vector3 targetSwing = new Vector3(lissajousX * swingCurveX.Evaluate(movementAmount), lissajousY * swingCurveY.Evaluate(movementAmount), 0);
        targetSwing += new Vector3(0, -Mathf.Abs(lissajousY) * jumpSwingAmount, 0) * (playerController.isGrounded ? 0f : 1f);
        transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition + targetSwing * swingAmount, Time.deltaTime * swingSpeed);
    }
}