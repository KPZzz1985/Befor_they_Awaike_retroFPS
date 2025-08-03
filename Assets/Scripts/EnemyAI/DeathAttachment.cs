using UnityEngine;

/// <summary>
/// Класс для маппинга префабов эффектов с типами урона при специальной смерти
/// </summary>
[System.Serializable]
public class DeathAttachment
{
    [Header("Attachment Settings")]
    [Tooltip("Префаб для спавна (должен содержать FireEffectZone или аналогичный компонент)")]
    public GameObject attachmentPrefab;
    
    [Tooltip("Тип урона для которого предназначен этот эффект")]
    public DamageType damageType;
    
    [Header("Spawn Settings")]
    [Tooltip("Минимальное время жизни эффекта (если не задано в StatusEffectData)")]
    [SerializeField] private float minLifetime = 3f;
    
    [Tooltip("Максимальное время жизни эффекта (если не задано в StatusEffectData)")]
    [SerializeField] private float maxLifetime = 6f;
    
    /// <summary>
    /// Проверяет, подходит ли этот эффект для данного типа урона
    /// </summary>
    public bool MatchesDamageType(DamageType targetType)
    {
        return damageType == targetType;
    }
    
    /// <summary>
    /// Получает рандомное время жизни для этого эффекта
    /// </summary>
    public float GetRandomLifetime()
    {
        return UnityEngine.Random.Range(minLifetime, maxLifetime);
    }
    
    /// <summary>
    /// Проверяет, валидный ли префаб для спавна
    /// </summary>
    public bool IsValid()
    {
        return attachmentPrefab != null;
    }
}