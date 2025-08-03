using UnityEngine;
using System.Collections;

/// <summary>
/// Интерфейс для обработчиков специальной смерти врагов.
/// Каждый тип смерти (огонь, кислота, электричество, etc.) реализует этот интерфейс.
/// </summary>
public interface IDeathHandler
{
    /// <summary>
    /// Проверяет, подходит ли этот обработчик для данного типа урона
    /// </summary>
    bool CanHandle(DamageType damageType);
    
    /// <summary>
    /// Запускает обработку специальной смерти
    /// </summary>
    /// <param name="enemyHealth">Компонент здоровья врага</param>
    /// <param name="damageType">Тип урона, который убил врага</param>
    /// <param name="hitPoint">Точка попадания</param>
    /// <param name="hitDirection">Направление удара</param>
    /// <returns>Корутина обработки смерти</returns>
    IEnumerator HandleDeath(EnemyHealth enemyHealth, DamageType damageType, Vector3 hitPoint, Vector3 hitDirection);
}