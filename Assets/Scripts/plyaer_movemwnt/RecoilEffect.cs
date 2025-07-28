using UnityEngine;

public class RecoilEffect : MonoBehaviour
{
    public float baseRecoilForce = 2f; // Базовая сила отдачи
    public float maxVerticalRecoil = 10f; // Максимальный угол отдачи

    private CameraRotator cameraRotator; // Ссылка на скрипт вращения камеры
    private PM_Shooting pmShooting; // Ссылка на текущий скрипт PM_Shooting оружия

    private void Start()
    {
        // Находим скрипт вращения камеры
        cameraRotator = FindObjectOfType<CameraRotator>();
        if (cameraRotator == null)
        {
            Debug.LogError("CameraRotator не найден.");
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
            Debug.LogWarning("PM_Shooting не назначен на активное оружие.");
            return;
        }

        // Расчет итоговой силы отдачи с учетом baseRecoilForce и recoilForce из PM_Shooting
        float finalRecoilForce = baseRecoilForce * pmShooting.recoilForce;
        cameraRotator._yRotation = Mathf.Clamp(cameraRotator._yRotation + finalRecoilForce, -cameraRotator.maxVerticalAngle, maxVerticalRecoil);
    }
}
