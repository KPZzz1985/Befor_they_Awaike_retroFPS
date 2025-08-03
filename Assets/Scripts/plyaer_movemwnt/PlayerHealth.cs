using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;

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

    [Header("Fog Effects (DEPRECATED - УСТАРЕЛО)")]
    [Tooltip("УСТАРЕЛО: Fog эффекты заменены на UI плашки для оптимизации")]
    public SimpleFogEffect fogEffect;
    [Tooltip("УСТАРЕЛО: Цвет тумана больше не используется динамически")]
    public Color initialFogColor;

    [System.Obsolete("Заменен на UI анимации в HUDController")]
    public float fogCooldownDuration = 5f;
    [System.Obsolete("Заменен на UI анимации в HUDController")]
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
    
    [Header("UI References")]
    [Tooltip("Ссылка на HUDController для UI эффектов")]
    public HUDController hudController;

    [Header("Pixel Effects (DEPRECATED - УСТАРЕЛО)")]
    [Tooltip("УСТАРЕЛО: Пиксельные эффекты заменены на UI затемнение для оптимизации")]
    [System.Obsolete("Заменен на UI затемнение в HUDController")]
    public Material pixelEffectMaterial;
    [System.Obsolete("Заменен на UI затемнение в HUDController")]
    public float pixelationTime = 5f;
    [System.Obsolete("Заменен на UI затемнение в HUDController")]
    public Color[] deathColors = new Color[4];

    private void Start()
    {
        _healthPercentage = 100f;

        if (fpsController == null)
            fpsController = GetComponent<FirstPersonController_CC>();

        if (playerCamera != null)
            originalCameraPosition = playerCamera.transform.localPosition;

        // Включаем SimpleFogEffect для работы с декалями
        if (fogEffect != null && fogEffect.fogMaterial != null)
        {
            fogEffect.fogMaterial.SetColor("_FogColor", initialFogColor);
            fogEffect.enabled = true;
            Debug.Log("PlayerHealth: SimpleFogEffect компонент включен для работы с декалями");
        }
            
        // Автоматически находим HUDController если не назначен
        if (hudController == null)
            hudController = FindObjectOfType<HUDController>();
            
    }

    /// <summary>
    ///    ,   .
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
            // Легкий эффект встряски камеры
            CameraShake();
            
            // Новый UI эффект вместо тяжелого Fog
            TriggerDamageUIEffect();
        }
    }

    /// <summary>
    /// Новый перегруженный, принимающий DamageType, но обрабатывает пока так же, как простой урон.
    /// ОПТИМИЗИРОВАНО: убрана тяжелая логика с Fog, добавлен UI эффект.
    /// </summary>
    public void TakeDamage(int damage, DamageType damageType)
    {
        // В будущем можно добавить специфичные эффекты для разных типов урона
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

    /// <summary>
    /// УСТАРЕВШИЙ МЕТОД: UpdateFogColor больше не используется (оптимизация).
    /// Оставлен для совместимости, заменен на UI эффекты в HUDController.
    /// </summary>
    [System.Obsolete("Использовать TriggerDamageUIEffect() вместо UpdateFogColor для лучшей производительности")]
    private void UpdateFogColor()
    {
        // ПОЛНОСТЬЮ ОТКЛЮЧЕНО: тяжелая операция с Fog материалом заменена на UI эффекты
        // СТАРЫЙ КОД УБРАН ПОЛНОСТЬЮ - больше НЕ используется fogColorCooldownCoroutine
        
        Debug.LogWarning("UpdateFogColor() вызван, но метод устарел. Используйте UI эффекты в HUDController.");
    }

    /// <summary>
    /// УСТАРЕВШИЙ МЕТОД: FogColorCooldown больше не используется (оптимизация).
    /// Оставлен для совместимости, заменен на UI эффекты в HUDController.
    /// </summary>
    [System.Obsolete("Заменен на UI анимации в HUDController через UniTask")]
    private IEnumerator FogColorCooldown()
    {
        // ПОЛНОСТЬЮ ОТКЛЮЧЕНО: больше НЕ используется Fog система
        
        Debug.LogWarning("FogColorCooldown() вызван, но метод устарел. Используйте UI анимации в HUDController.");
        yield break;
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
        Debug.Log("Player died!");
        
        // УПРОЩЕННАЯ логика смерти: только отключаем управление камерой
        if (cameraHolderScript != null)
            cameraHolderScript.enabled = false;
            
        // Отделяем камеру от игрока с небольшим импульсом
        DetachCameraWithImpulse();
        
        // Запускаем UI эффект смерти (затемнение экрана)
        TriggerDeathUIEffect();
        
        // Отключаем управление игроком
        if (fpsController != null)
            fpsController.enabled = false;
    }

    /// <summary>
    /// Отделяет камеру от игрока с небольшим физическим импульсом
    /// </summary>
    private void DetachCameraWithImpulse()
    {
        if (playerCamera == null) return;
        
        // Добавляем физику к камере
        Rigidbody camRb = playerCamera.gameObject.GetComponent<Rigidbody>();
        if (camRb == null)
            camRb = playerCamera.gameObject.AddComponent<Rigidbody>();

        BoxCollider camCollider = playerCamera.gameObject.GetComponent<BoxCollider>();
        if (camCollider == null)
            camCollider = playerCamera.gameObject.AddComponent<BoxCollider>();

        // Применяем небольшой импульс
        float sideForce = UnityEngine.Random.Range(0, 2) == 0 ? 1f : -1f;
        Vector3 impulseDirection = new Vector3(sideForce, 0, -1f).normalized;
        camRb.AddRelativeForce(impulseDirection * 1f, ForceMode.Impulse);
    }
    
    /// <summary>
    /// Запускает UI эффект вспышки при получении урона
    /// </summary>
    private void TriggerDamageUIEffect()
    {
        if (hudController != null)
        {
            Debug.Log("PlayerHealth: Запуск UI эффекта урона через HUDController");
            // Запускаем асинхронно, не блокируя основной поток
            hudController.ShowDamageFlash().Forget();
        }
        else
        {
            Debug.LogError("PlayerHealth: HUDController не найден! UI эффект урона не сработает.");
        }
    }
    
    /// <summary>
    /// Запускает UI эффект затемнения при смерти
    /// </summary>
    private void TriggerDeathUIEffect()
    {
        if (hudController != null)
        {
            Debug.Log("PlayerHealth: Запуск UI эффекта смерти через HUDController");
            // Запускаем асинхронно, не блокируя основной поток
            hudController.ShowDeathOverlay().Forget();
        }
        else
        {
            Debug.LogError("PlayerHealth: HUDController не найден! UI эффект смерти не сработает.");
        }
    }
}
