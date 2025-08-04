using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Обработчик огненной смерти. Враг горит и хаотично двигается перед окончательной смертью.
/// </summary>
public class FireDeathHandler : IDeathHandler
{
    [Header("Fire Death Settings")]
    private float minBurnDuration = 4f;        // Минимальное время горения
    private float maxBurnDuration = 6f;        // Максимальное время горения
    private float minPauseTime = 0.5f;           // Минимальное время паузы
    private float maxPauseTime = 1.5f;         // Максимальное время паузы
    private float minMoveSpeed = 5f;           // Минимальная скорость движения
    private float maxMoveSpeed = 6f;           // Максимальная скорость движения
    private float navMeshSearchRadius = 50f;   // Радиус поиска случайных точек на NavMesh
    
    [Header("Ground Fire Trail Settings")]
    [Tooltip("Минимальная скорость для оставления огненных следов")]
    [SerializeField] private float minSpeedForTrail = 1f;
    
    [Tooltip("Интервал между спавном следов на земле (в секундах)")]
    [SerializeField] private float trailSpawnInterval = 0.15f; // Уменьшено для более частых следов
    
    [Tooltip("Минимальное расстояние между следами (в метрах)")]
    [SerializeField] private float minDistanceBetweenTrails = 0.5f; // Уменьшено для более частых следов
    
    [Tooltip("УБРАНО: теперь используем тег Ground как в декал системе")]
    // Больше не используем LayerMask, используем CompareTag("Ground") как в CreateDecalOnGround
    

    
    /// <summary>
    /// Проверяет, может ли этот обработчик работать с данным типом урона
    /// </summary>
    public bool CanHandle(DamageType damageType)
    {
        return damageType == DamageType.Fire || damageType == DamageType.DarkFire;
    }
    
    /// <summary>
    /// Обрабатывает огненную смерть по новой логике: рандомное движение с паузами
    /// </summary>
    public IEnumerator HandleDeath(EnemyHealth enemyHealth, DamageType damageType, Vector3 hitPoint, Vector3 hitDirection)
    {
        Debug.Log($"Начинается огненная смерть для {enemyHealth.name}");
        
        // Получаем компоненты врага
        var animator = enemyHealth.GetComponent<Animator>();
        var navMeshAgent = enemyHealth.GetComponent<NavMeshAgent>();
        
        // 1. Останавливаем все боевые системы, оставляем NavMeshAgent
        DisableEnemySystems(enemyHealth);
        
        // Убеждаемся что NavMeshAgent доступен
        if (navMeshAgent != null && !navMeshAgent.enabled)
        {
            navMeshAgent.enabled = true;
            Debug.Log($"NavMeshAgent включен для специальной смерти {enemyHealth.name}");
        }
        
        // 2. Спавним огненные эффекты на ригидбоди
        enemyHealth.SpawnDeathAttachments(damageType);
        
        // 3. Применяем специальные текстуры на все Damageable компоненты
        enemyHealth.ApplySpecialDeathTextures(damageType);
        
        // 4. Определяем общее время горения (4-6 секунд)
        float totalBurnTime = UnityEngine.Random.Range(minBurnDuration, maxBurnDuration);
        float timeElapsed = 0f;
        
        // 5. Включаем флаг горения
        if (animator != null)
        {
            animator.SetBool("isBurning", true);
            animator.SetFloat("Speed", 0f); // Начинаем с нулевой скорости
        }
        
        Debug.Log($"Огненная смерть будет длиться {totalBurnTime:F1} секунд для {enemyHealth.name}");
        
        // 6. Запускаем систему огненных следов на земле
        enemyHealth.StartCoroutine(SpawnGroundFireTrails(enemyHealth, damageType, totalBurnTime, animator));
        
        // 7. Основной цикл движения с паузами
        float finalSpeed = 0f; // Отслеживаем финальную скорость
        
        while (timeElapsed < totalBurnTime)
        {
            // ФАЗА ПАУЗЫ: стоим на месте
            float pauseTime = UnityEngine.Random.Range(minPauseTime, maxPauseTime);
            
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }
            finalSpeed = 0f;
            
            Debug.Log($"Пауза {pauseTime:F1} сек для {enemyHealth.name}");
            yield return new WaitForSeconds(pauseTime);
            timeElapsed += pauseTime;
            
            // Проверяем, не закончилось ли время
            if (timeElapsed >= totalBurnTime) break;
            
            // ФАЗА ДВИЖЕНИЯ: идем к случайной точке
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                Vector3 randomPoint = GetRandomNavMeshPoint(enemyHealth.transform.position);
                float moveSpeed = UnityEngine.Random.Range(minMoveSpeed, maxMoveSpeed);
                
                navMeshAgent.speed = moveSpeed;
                navMeshAgent.SetDestination(randomPoint);
                
                if (animator != null)
                {
                    animator.SetFloat("Speed", moveSpeed);
                }
                finalSpeed = moveSpeed;
                
                Debug.Log($"Движение к {randomPoint} со скоростью {moveSpeed:F1} для {enemyHealth.name}");
                
                // Ждем пока не дойдем ИЛИ не закончится время
                while (timeElapsed < totalBurnTime && 
                       navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh &&
                       (navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.5f))
                {
                    yield return new WaitForSeconds(0.1f);
                    timeElapsed += 0.1f;
                }
                
                Debug.Log($"Достигли точки или время вышло для {enemyHealth.name}");
            }
            else
            {
                // Если NavMeshAgent недоступен, просто ждем
                yield return new WaitForSeconds(1f);
                timeElapsed += 1f;
            }
        }
        
        // 8. Сохраняем финальную скорость для выбора типа смерти
        enemyHealth.SetFinalMoveSpeed(finalSpeed);
        
        // 9. НЕ сбрасываем специальные текстуры - они остаются навсегда
        // enemyHealth.ResetSpecialDeathTextures(); // УБРАНО: текстуры остаются навсегда
        
        // 10. Финальное отключение систем (ПЕРЕД сбросом аниматора)
        FinalizeDeathSystems(enemyHealth);
        
        // 11. Завершаем специальную смерть и сбрасываем анимацию
        if (animator != null)
        {
            animator.SetBool("isBurning", false);
            animator.SetFloat("Speed", 0f); // Сбрасываем скорость
            Debug.Log($"Сброшены флаги аниматора: isBurning=false, Speed=0 для {enemyHealth.name}");
        }
        
        Debug.Log($"Огненная смерть завершена для {enemyHealth.name}, финальная скорость: {finalSpeed:F1}");
    }
    
    /// <summary>
    /// Отключает боевые системы врага, но оставляет NavMeshAgent для возможности движения
    /// </summary>
    private void DisableEnemySystems(EnemyHealth enemyHealth)
    {
        var gameObject = enemyHealth.gameObject;
        
        // NavMeshAgent оставляем включенным для возможности движения во время спецсмерти
        // Он будет отключен в конце в методе FinalizeDeathSystems()
        
        // Отключаем все MonoBehaviour компоненты по именам (попробуем найти по общим именам)
        DisableComponentByName(gameObject, "EnemyMovement2");
        DisableComponentByName(gameObject, "EnemyMovement");
        DisableComponentByName(gameObject, "EnemyPatrol");
        DisableComponentByName(gameObject, "EnemyWeapon");  // ВАЖНО: отключаем стрельбу
        DisableComponentByName(gameObject, "LookRegistrator");
        DisableComponentByName(gameObject, "AlertRadius");
        DisableComponentByName(gameObject, "RotateForSearching");
        
        // Попробуем найти стрельбу по разным именам
        DisableComponentByName(gameObject, "EnemyShootingScript");
        DisableComponentByName(gameObject, "EnemyShooting");
        DisableComponentByName(gameObject, "ShootingController");
        
        // Попробуем найти атаку по разным именам  
        DisableComponentByName(gameObject, "EnemyAttack");
        DisableComponentByName(gameObject, "TacticalAttackSystem");
        
        Debug.Log($"Отключены боевые системы для {enemyHealth.name}, NavMeshAgent оставлен активным");
    }
    
    /// <summary>
    /// Финальное отключение всех оставшихся систем перед переходом к обычной смерти
    /// </summary>
    private void FinalizeDeathSystems(EnemyHealth enemyHealth)
    {
        var gameObject = enemyHealth.gameObject;
        
        // Теперь отключаем NavMeshAgent
        var navMeshAgent = gameObject.GetComponent<NavMeshAgent>();
        if (navMeshAgent != null) 
        {
            navMeshAgent.enabled = false;
            Debug.Log($"NavMeshAgent окончательно отключен для {enemyHealth.name}");
        }
    }
    
    /// <summary>
    /// Отключает компонент по имени, если он найден
    /// </summary>
    private void DisableComponentByName(GameObject gameObject, string componentName)
    {
        var component = gameObject.GetComponent(componentName) as MonoBehaviour;
        if (component != null)
        {
            component.enabled = false;
            Debug.Log($"Отключен компонент: {componentName}");
        }
    }
    
    /// <summary>
    /// Находит случайную точку на NavMesh в радиусе от текущей позиции
    /// </summary>
    private Vector3 GetRandomNavMeshPoint(Vector3 origin)
    {
        // Пытаемся найти случайную точку несколько раз
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * navMeshSearchRadius;
            randomDirection += origin;
            
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        
        // Если не удалось найти случайную точку, возвращаем текущую позицию
        Debug.LogWarning($"Не удалось найти случайную точку на NavMesh, остаемся на месте");
        return origin;
    }
    
    /// <summary>
    /// Корутина для спавна огненных следов на земле при движении
    /// </summary>
    private IEnumerator SpawnGroundFireTrails(EnemyHealth enemyHealth, DamageType damageType, float totalDuration, Animator animator)
    {
        float elapsedTime = 0f;
        Vector3 lastTrailPosition = enemyHealth.transform.position;
        
        Debug.Log($"Запуск системы огненных следов для {enemyHealth.name}");
        
        while (elapsedTime < totalDuration)
        {
            // Проверяем скорость из аниматора
            float currentSpeed = 0f;
            if (animator != null)
            {
                currentSpeed = animator.GetFloat("Speed");
            }
            
            // Если враг движется достаточно быстро
            if (currentSpeed > minSpeedForTrail)
            {
                Vector3 currentPosition = enemyHealth.transform.position;
                
                // Проверяем, переместился ли враг достаточно далеко от последнего следа
                float distanceFromLastTrail = Vector3.Distance(currentPosition, lastTrailPosition);
                if (distanceFromLastTrail > minDistanceBetweenTrails)
                {
                    SpawnGroundFireEffect(enemyHealth, damageType, currentPosition);
                    lastTrailPosition = currentPosition;
                }
            }
            
            yield return new WaitForSeconds(trailSpawnInterval);
            elapsedTime += trailSpawnInterval;
        }
        
        Debug.Log($"Система огненных следов завершена для {enemyHealth.name}");
    }
    
    /// <summary>
    /// Спавнит огненный эффект на земле под врагом (использует тот же подход что и CreateDecalOnGround)
    /// </summary>
    private void SpawnGroundFireEffect(EnemyHealth enemyHealth, DamageType damageType, Vector3 position)
    {
        // Получаем эффекты для данного типа урона из EnemyHealth
        var attachments = enemyHealth.GetAttachmentsForDamageType(damageType);
        if (attachments.Length == 0)
        {
            Debug.LogWarning($"Нет эффектов для огненных следов типа {damageType}");
            return;
        }
        
        // ИСПОЛЬЗУЕМ ТОТ ЖЕ ПОДХОД ЧТО И В CreateDecalOnGround:
        RaycastHit hit;
        float heightOffset = 0.1f; // Как в CreateDecalOnGround
        
        // Raycast прямо вниз от позиции врага (как в CreateDecalOnGround)
        if (Physics.Raycast(position, -Vector3.up, out hit))
        {
            // Проверяем тег "Ground" (как в CreateDecalOnGround)
            if (hit.collider.CompareTag("Ground"))
            {
                Debug.Log($"Raycast попал в Ground: {hit.collider.name}, позиция: {hit.point}");
                
                // Выбираем случайный эффект
                var randomAttachment = attachments[UnityEngine.Random.Range(0, attachments.Length)];
                
                // Спавним эффект
                var spawnedEffect = UnityEngine.Object.Instantiate(randomAttachment.attachmentPrefab);
                
                // ТОЧНО ТАК ЖЕ КАК В CreateDecalOnGround:
                float randomYRotation = UnityEngine.Random.Range(0f, 360f);
                Quaternion effectRotation = Quaternion.Euler(0f, randomYRotation, 0f) * Quaternion.FromToRotation(Vector3.up, hit.normal);
                Vector3 effectPosition = hit.point + hit.normal * heightOffset;
                
                spawnedEffect.transform.position = effectPosition;
                spawnedEffect.transform.rotation = effectRotation;
                
                // Небольшой случайный offset (как в CreateDecalOnGround)
                Vector3 randomOffset = new Vector3(
                    UnityEngine.Random.Range(-0.15f, 0.15f), 
                    0, 
                    UnityEngine.Random.Range(-0.15f, 0.15f)
                );
                spawnedEffect.transform.position += randomOffset;
                
                // Устанавливаем время жизни
                float lifetime = randomAttachment.GetRandomLifetime();
                enemyHealth.StartCoroutine(DestroyGroundEffectAfterTime(spawnedEffect, lifetime));
                
                Debug.Log($"Спавнен огненный след на Ground: {randomAttachment.attachmentPrefab.name} в позиции {effectPosition} (время жизни: {lifetime:F1}с)");
            }
            else
            {
                Debug.Log($"Raycast попал не в Ground: {hit.collider.name} (тег: {hit.collider.tag})");
            }
        }
        else
        {
            Debug.LogWarning($"Raycast не попал ни во что под {enemyHealth.name}. Позиция: {position}");
        }
    }
    
    /// <summary>
    /// Корутина для уничтожения огненного следа на земле
    /// </summary>
    private IEnumerator DestroyGroundEffectAfterTime(GameObject effect, float lifetime)
    {
        if (effect == null) yield break;
        
        yield return new WaitForSeconds(lifetime);
        
        if (effect != null)
        {
            Debug.Log($"Уничтожаем огненный след {effect.name} после {lifetime:F1}с жизни");
            UnityEngine.Object.Destroy(effect);
        }
    }
    

}