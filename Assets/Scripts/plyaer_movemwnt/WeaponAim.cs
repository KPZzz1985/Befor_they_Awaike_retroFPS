// WeaponAim.cs
using UnityEngine;

public class WeaponAim : MonoBehaviour
{
    [Header("Aim Settings")]
    public Vector3 aimOffset;
    public float aimSpeed = 5f;
    public Animator weaponAnimator;
    public FirstPersonController_CC firstPersonController;
    public float reducedMoveSpeed = 2f;

    [Header("Camera Settings")]
    public Camera mainCamera;
    public float defaultFov;
    public float aimFov = 30f;
    private float fovVelocity = 0f;
    public float smoothTime = 0.2f;

    [Header("Shooting Settings")]
    public PM_Shooting pmShooting;  // ссылка на PM_Shooting

    private WeaponHolderMovement weaponHolderMovement;
    private Vector3 defaultPosition;
    public bool isAiming;
    private float originalMoveSpeed;

    private void Start()
    {
        weaponHolderMovement = GetComponent<WeaponHolderMovement>();
        defaultPosition = transform.localPosition;
        defaultFov = mainCamera.fieldOfView;
        originalMoveSpeed = firstPersonController.moveSpeed;
    }

    private void Update()
    {
        // ¬место pmShooting.isReloading провер€ем параметр аниматора
        if (weaponAnimator.GetBool("isReloading"))
        {
            isAiming = false;
        }
        else if (Input.GetMouseButtonDown(1))
        {
            isAiming = true;
            weaponHolderMovement.enabled = false;
            weaponAnimator.SetBool("isAim", true);
            firstPersonController.moveSpeed = reducedMoveSpeed;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isAiming = false;
            weaponHolderMovement.enabled = true;
            weaponAnimator.SetBool("isAim", false);
            firstPersonController.moveSpeed = originalMoveSpeed;
        }

        UpdateAimPosition();
        UpdateCameraFov();
    }

    private void UpdateAimPosition()
    {
        Vector3 targetPosition = isAiming ? aimOffset : defaultPosition;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * aimSpeed);
    }

    private void UpdateCameraFov()
    {
        float targetFov = isAiming ? aimFov : defaultFov;
        mainCamera.fieldOfView = Mathf.SmoothDamp(mainCamera.fieldOfView, targetFov, ref fovVelocity, smoothTime);
    }
}
