using UnityEngine;
using System.Collections;

/// <summary>
/// PlayerHealth �������� �� �������� ������, ������ � �������, ������� � �������.
/// �� ��������� ���������� TakeDamage(int damage, DamageType damageType), 
/// �� ������������ � ����� ��� ��, ��� ������� ����.
/// :contentReference[oaicite:1]{index=1}
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 1000;
    private float _healthPercentage = 100f;

    public float damageCameraShakeForce = 5f;
    private Vector3 originalCameraPosition;

    public SimpleFogEffect fogEffect;
    public Color initialFogColor;

    public float fogCooldownDuration = 5f;
    private Coroutine fogColorCooldownCoroutine;

    public float HealthPercentage
    {
        get { return _healthPercentage; }
        set { _healthPercentage = Mathf.Clamp(value, 0f, 100f); }
    }

    public int CurrentHealth => Mathf.RoundToInt((HealthPercentage / 100f) * maxHealth);

    public CameraHolder cameraHolderScript;
    public Camera playerCamera;

    public FirstPersonController_CC fpsController;

    public Material pixelEffectMaterial;
    public float pixelationTime = 5f;
    public Color[] deathColors = new Color[4];

    private void Start()
    {
        _healthPercentage = 100f;

        if (fpsController == null)
            fpsController = GetComponent<FirstPersonController_CC>();

        if (playerCamera != null)
            originalCameraPosition = playerCamera.transform.localPosition;

        if (fogEffect != null && fogEffect.fogMaterial != null)
            fogEffect.fogMaterial.SetColor("_FogColor", initialFogColor);
    }

    /// <summary>
    /// ������� ����� ��������� �����, ������� ��� ���.
    /// </summary>
    public void TakeDamage(int damage)
    {
        HealthPercentage -= ((float)damage / maxHealth) * 100f;
        if (CurrentHealth <= 0)
        {
            Die();
        }
        else
        {
            CameraShake();
            if (fogColorCooldownCoroutine != null)
                StopCoroutine(fogColorCooldownCoroutine);
            UpdateFogColor();
        }
    }

    /// <summary>
    /// ����� ����������, ����������� DamageType, �� �������������� ��� ��, ��� ������� ����.
    /// </summary>
    public void TakeDamage(int damage, DamageType damageType)
    {
        // ��� ������ ��� ����� ������� � �������� ������ �����
        TakeDamage(damage);
    }

    /// <summary>
    /// Restores player health by the specified amount.
    /// </summary>
    public void Heal(int healAmount)
    {
        HealthPercentage += ((float)healAmount / maxHealth) * 100f;
    }

    private void CameraShake()
    {
        if (playerCamera != null)
            StartCoroutine(ShakeCameraEffect());
    }

    private void UpdateFogColor()
    {
        if (fogEffect != null && fogEffect.fogMaterial != null)
        {
            Color targetFogColor = Color.Lerp(initialFogColor, Color.red, 1f - HealthPercentage / 100f);
            fogEffect.fogMaterial.SetColor("_FogColor", targetFogColor);
            fogColorCooldownCoroutine = StartCoroutine(FogColorCooldown());
        }
    }

    private IEnumerator FogColorCooldown()
    {
        float elapsedTime = 0;
        Color startColor = fogEffect.fogMaterial.GetColor("_FogColor");

        while (elapsedTime < fogCooldownDuration)
        {
            elapsedTime += Time.deltaTime;
            Color newColor = Color.Lerp(startColor, initialFogColor, elapsedTime / fogCooldownDuration);
            fogEffect.fogMaterial.SetColor("_FogColor", newColor);
            yield return null;
        }

        fogEffect.fogMaterial.SetColor("_FogColor", initialFogColor);
    }

    private IEnumerator ShakeCameraEffect()
    {
        float duration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            playerCamera.transform.localPosition = originalCameraPosition + Random.insideUnitSphere * damageCameraShakeForce;
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, originalCameraPosition, elapsedTime / duration);
            yield return null;
        }

        playerCamera.transform.localPosition = originalCameraPosition;
    }

    private void Die()
    {
        if (cameraHolderScript == null)
        {
            Debug.LogError("cameraHolderScript is null");
            return;
        }
        cameraHolderScript.enabled = false;

        if (playerCamera == null)
        {
            Debug.LogError("playerCamera is null");
            return;
        }

        Rigidbody camRb = playerCamera.gameObject.GetComponent<Rigidbody>();
        if (camRb == null)
            camRb = playerCamera.gameObject.AddComponent<Rigidbody>();

        BoxCollider camCollider = playerCamera.gameObject.GetComponent<BoxCollider>();
        if (camCollider == null)
            camCollider = playerCamera.gameObject.AddComponent<BoxCollider>();

        float sideForce = Random.Range(0, 2) == 0 ? 1f : -1f;
        Vector3 impulseDirection = new Vector3(sideForce, 0, -1f).normalized;
        camRb.AddRelativeForce(impulseDirection * 1f, ForceMode.Impulse);

        Debug.Log("Player died!");

        Invoke("DisablePlayer", 0.25f);
    }

    private void DisablePlayer()
    {
        if (fpsController != null)
            fpsController.enabled = false;

        StartCoroutine(PixelateDeathEffect());
    }

    private IEnumerator DestroyPlayer()
    {
        yield return new WaitForSeconds(3f);
        gameObject.SetActive(false);
    }

    private IEnumerator PixelateDeathEffect()
    {
        Debug.LogWarning("Pixelate");
        float elapsedTime = 0f;
        while (elapsedTime < pixelationTime)
        {
            float progress = elapsedTime / pixelationTime;
            pixelEffectMaterial.SetFloat("_PixelSize", Mathf.Lerp(1, 100, progress));
            for (int i = 0; i < 4; i++)
            {
                pixelEffectMaterial.SetColor($"_ColorArray[{i}]", deathColors[i]);
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        DisablePlayerFinal();
    }

    private void DisablePlayerFinal()
    {
        gameObject.SetActive(false);
    }
}
