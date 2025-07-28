using UnityEngine;

public class RecoilEffect : MonoBehaviour
{
    public float baseRecoilForce = 2f; // ������� ���� ������
    public float maxVerticalRecoil = 10f; // ������������ ���� ������

    private CameraRotator cameraRotator; // ������ �� ������ �������� ������
    private PM_Shooting pmShooting; // ������ �� ������� ������ PM_Shooting ������

    private void Start()
    {
        // ������� ������ �������� ������
        cameraRotator = FindObjectOfType<CameraRotator>();
        if (cameraRotator == null)
        {
            Debug.LogError("CameraRotator �� ������.");
        }
    }

    public void SetPM_Shooting(PM_Shooting newShootingScript)
    {
        pmShooting = newShootingScript;
    }

    public void ApplyRecoil()
    {
        if (pmShooting == null)
        {
            Debug.LogWarning("PM_Shooting �� �������� �� �������� ������.");
            return;
        }

        // ������ �������� ���� ������ � ������ baseRecoilForce � recoilForce �� PM_Shooting
        float finalRecoilForce = baseRecoilForce * pmShooting.recoilForce;
        cameraRotator._yRotation = Mathf.Clamp(cameraRotator._yRotation + finalRecoilForce, -cameraRotator.maxVerticalAngle, maxVerticalRecoil);
    }
}
