using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляющий компонент для системы специальных смертей.
/// Выбирает подходящий обработчик смерти и запускает его.
/// </summary>
public class DeathHandlerManager : MonoBehaviour
{
    [Header("Death Handlers")]
    [Tooltip("Список всех доступных обработчиков смерти")]
    private List<IDeathHandler> deathHandlers = new List<IDeathHandler>();
    
    [Header("Settings")]
    [Tooltip("Шанс активации специальной смерти (0-1). Если 0, всегда обычная смерть")]
    [Range(0f, 1f)]
    public float specialDeathChance = 1f;
    
    private void Awake()
    {
        // Регистрируем все обработчики смерти
        RegisterDeathHandlers();
    }
    
    /// <summary>
    /// Регистрирует все доступные обработчики смерти
    /// </summary>
    private void RegisterDeathHandlers()
    {
        // Добавляем обработчик огненной смерти
        deathHandlers.Add(new FireDeathHandler());
        
        // TODO: Добавить другие обработчики:
        // deathHandlers.Add(new AcidDeathHandler());
        // deathHandlers.Add(new ElectricDeathHandler());
        // deathHandlers.Add(new DarkFireDeathHandler());
    }
    
    /// <summary>
    /// Обрабатывает смерть врага - выбирает специальный или обычный тип смерти
    /// </summary>
    /// <param name="enemyHealth">Компонент здоровья врага</param>
    /// <param name="damageType">Тип урона, который убил врага</param>
    /// <param name="hitPoint">Точка попадания</param>
    /// <param name="hitDirection">Направление удара</param>
    public void HandleEnemyDeath(EnemyHealth enemyHealth, DamageType damageType, Vector3 hitPoint, Vector3 hitDirection)
    {
        // Проверяем, есть ли подходящий обработчик для этого типа урона
        IDeathHandler handler = FindHandlerForDamageType(damageType);
        
        // Если обработчик найден И выпал шанс специальной смерти
                    if (handler != null && UnityEngine.Random.value <= specialDeathChance)
        {
            Debug.Log($"Активирована специальная смерть для типа урона: {damageType}");
            StartCoroutine(ExecuteSpecialDeath(enemyHealth, handler, damageType, hitPoint, hitDirection));
        }
        else
        {
            // Обычная смерть
            Debug.Log($"Обычная смерть для типа урона: {damageType}");
            ExecuteNormalDeath(enemyHealth);
        }
    }
    
    /// <summary>
    /// Ищет подходящий обработчик для типа урона
    /// </summary>
    private IDeathHandler FindHandlerForDamageType(DamageType damageType)
    {
        foreach (var handler in deathHandlers)
        {
            if (handler.CanHandle(damageType))
                return handler;
        }
        return null;
    }
    
    /// <summary>
    /// Выполняет специальную смерть
    /// </summary>
    private IEnumerator ExecuteSpecialDeath(EnemyHealth enemyHealth, IDeathHandler handler, DamageType damageType, Vector3 hitPoint, Vector3 hitDirection)
    {
        // Помечаем, что враг в процессе специальной смерти
        enemyHealth.isHandlingDeath = true;
        
        // Уведомляем системы о "смерти" (но не окончательной)
        enemyHealth.TriggerDeathEvent();
        
        // Запускаем обработчик специальной смерти
        yield return StartCoroutine(handler.HandleDeath(enemyHealth, damageType, hitPoint, hitDirection));
        
        // После завершения специальной смерти - переходим к обычной
        enemyHealth.isHandlingDeath = false;
        enemyHealth.SetDeathType(DamageType.Generic);  // Сбрасываем тип смерти для дебага
        ExecuteNormalDeath(enemyHealth);
    }
    
    /// <summary>
    /// Выполняет обычную смерть (как было раньше)
    /// </summary>
    private void ExecuteNormalDeath(EnemyHealth enemyHealth)
    {
        enemyHealth.isDead = true;
        
        // Если еще не вызывали OnDeath (при обычной смерти без спецэффектов)
        if (!enemyHealth.isHandlingDeath)
        {
            enemyHealth.TriggerDeathEvent();
            // При обычной смерти без спецэффектов - НЕ устанавливаем finalMoveSpeed
            // hadSpecialDeath остается false, что означает обычную смерть с импульсом
            Debug.Log($"ExecuteNormalDeath: Обычная смерть для {enemyHealth.name}, импульс будет применен");
        }
        
        // Выбираем один из двух вариантов обычной смерти (как было раньше)
        if (UnityEngine.Random.value > 0.5f)
            enemyHealth.DieAlternative();
        else
            enemyHealth.Die();
    }
}