using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using System;

public class EnemyHealth : MonoBehaviour
{
    [Header("Damage Settings")]
    public bool isDamage = false;

    public event Action<bool> OnCriticalDamageChanged;
    private bool _isCriticalDamage;
    public bool isCriticalDamage
    {
        get => _isCriticalDamage;
        set
        {
            if (_isCriticalDamage != value)
            {
                _isCriticalDamage = value;
                OnCriticalDamageChanged?.Invoke(value);
            }
        }
    }

    [SerializeField] private float damageCooldown = 2.0f;  // используется при накоплении урона, но не для таймера в Update()
    private float damageCounter = 0f;
    private const float damageThreshold = 100f;
    [SerializeField] [Range(1, 100)] private int criticalDamageChance = 5; // Chance of critical damage in percent
    [SerializeField] private float criticalStanceTime = 5f; // Duration of critical damage stance

    [Header("Random Critical State Generator")]
    [SerializeField] public int minNumber = 1;
    [SerializeField] public int maxNumber = 2;
    [SerializeField] public int StateGenerator;

    [Header("Death State")]
    public bool isDead = false;
    public bool isHandlingDeath = false;  // Враг считается мертвым для систем, но проигрывает спецсмерть
    [SerializeField] private string currentDeathType = "None";  // Тип урона, вызвавший спецсмерть (для дебага)
    
    [Header("Special Death System")]
    private DeathHandlerManager deathHandlerManager;
    
    [Header("Blood Trail Settings")]
    [SerializeField] private float bloodTrailInterval = 1.0f;  // Интервал между декалями крови (в секундах)
    private float lastBloodTrailTime = 0f;  // Время последней декали крови
    // ПРИМЕЧАНИЕ: Все декали отключены во время isHandlingDeath=true для чистой спецсмерти
    
    [Header("Special Death Final Speed")]
    private float finalMoveSpeed = 0f;  // Финальная скорость движения при специальной смерти
    private bool hadSpecialDeath = false;  // Флаг: была ли специальная смерть
    
    [Header("Special Death Attachments")]
    [Tooltip("Список эффектов которые можно спавнить на ригидбоди при специальной смерти")]
    [SerializeField] private DeathAttachment[] handleDeathAttachments = new DeathAttachment[0];
    
    [Header("Attachment Spawn Settings")]
    [Tooltip("Минимальное количество эффектов на каждый ригидбоди")]
    [SerializeField] private int minAttachmentsPerRigidbody = 3;
    
    [Tooltip("Максимальное количество эффектов на каждый ригидбоди")]
    [SerializeField] private int maxAttachmentsPerRigidbody = 5;

    [Header("Decal System")]
    public SimpleDecalContainer decalContainer;
    public SimpleDecalContainer levelObjectDecalContainer; // For decals on walls/objects
    public SimpleDecalContainer bloodPuddleDecalContainer; // For blood puddle
    public GameObject bloodPuddleParentObject; // Parent for blood puddle
    private Vector3 lastDecalPosition;
    private float decalSpawnDistance = 0.1f;

    [Header("Blood Puddle Settings")]
    public float minPuddleGrowthRate = 0.1f;
    public float maxPuddleGrowthRate = 0.5f;
    public float bloodPuddleMinScale = 0.5f;
    public float bloodPuddleMaxScale = 2.0f;

    [Header("Health Settings")]
    [SerializeField] public float minHealth = 50f;
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] public float HealthPercentage;

    public delegate void TakeDamageDelegate(EnemyHealth enemyHealth);
    public event TakeDamageDelegate OnTakeDamage;

    public float health;
    public float initialHealth;

    [Header("Low Health Settings")]
    [SerializeField] private float lowHealthThreshold = 30f;

    [Header("Components to Disable on Death")]
    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private MonoBehaviour enemyShootingScript;
    [SerializeField] private MonoBehaviour enemyPatrol;
    [SerializeField] private MonoBehaviour enemyAttack;
    [SerializeField] private MonoBehaviour enemyMovement;
    [SerializeField] private MonoBehaviour alertRadius;
    [SerializeField] private MonoBehaviour rotateForSearching;
    [SerializeField] private MonoBehaviour lookRegistrator;
    [SerializeField] private GameObject ragdollObject; // Root of ragdoll

    [Header("Ragdoll Skinned Mesh Renderers")]
    [SerializeField] private SkinnedMeshRenderer[] skinnedMeshRenderers;

    [Header("Ragdoll Transition Settings")]
    [SerializeField] private float transitionDuration = 1f; // Duration for transition into ragdoll

    [Header("Death Check Sphere Settings")]
    [SerializeField] private float checkSphereRadius = 1f;

    [Header("Ragdoll Rigidbody Masses")]
    [SerializeField] private Rigidbody[] rigidbodies;

    [Header("Current Animation")]
    [SerializeField] public string currentAnimation;

    [Header("Ragdoll Impulse Settings")]
    [SerializeField] private float ragdollImpulseForce = 5f; // Impulse force for a normal hit
    [SerializeField] private Vector3 impulseAdjustment = Vector3.zero; // Adjustment vector for a normal hit
    [SerializeField] private float torqueForce = 10f; // Torque for a normal hit
    [SerializeField] private float ForceRagdoll = 100f; // Force for alternative ragdoll

    [Header("Ragdoll Settings")]
    [Tooltip("Reference to the pelvis Rigidbody (used to apply blast impulse)")]
    [SerializeField] private Rigidbody pelvisRigidbody;

    [Tooltip("Minimum upward force applied to pelvis during blast")]
    [SerializeField] private float blastUpwardForceMin = 5f;

    [Tooltip("Maximum upward force applied to pelvis during blast")]
    [SerializeField] private float blastUpwardForceMax = 15f;

    [Tooltip("Minimum outward force from blast center applied to pelvis")]
    [SerializeField] private float blastOutwardForceMin = 10f;

    [Tooltip("Maximum outward force from blast center applied to pelvis")]
    [SerializeField] private float blastOutwardForceMax = 25f;

    [Tooltip("Minimum torque force around Y-axis applied during blast")]
    [SerializeField] private float blastTorqueForceMin = 5f;

    [Tooltip("Maximum torque force around Y-axis applied during blast")]
    [SerializeField] private float blastTorqueForceMax = 20f;

    [Header("Level Object Decal Settings")]
    [SerializeField] private float levelObjectDistance = 10f;      // Max distance for level-object decals
    [SerializeField] private float decalYOffset = 0.02f;           // Y-offset for decals

    public int MinCritState = 0;
    public int MaxCritState = 6;

    public Animator animator;

    public event Action<EnemyHealth> OnDeath;

    private Vector3 lastHitPoint;
    private Vector3 lastHitDirection;

    [Header("Debug Flags")]
    public bool groundLayerDetected = false;
    public bool groundTagDetected = false;

    private void Start()
    {
        health = UnityEngine.Random.Range(minHealth, maxHealth);
        initialHealth = health;
        lastDecalPosition = transform.position;
        UpdateHealthPercentage();

        skinnedMeshRenderers = ragdollObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        rigidbodies = ragdollObject.GetComponentsInChildren<Rigidbody>();

        animator = GetComponent<Animator>();

        Debug.Log($"Decal Container: {decalContainer != null}");
        Debug.Log($"Level Object Decal Container: {levelObjectDecalContainer != null}");

        // Инициализируем систему специальных смертей
        InitializeDeathHandlerSystem();

        // Запускаем корутину случайных стейтов (аналогично старому скрипту)
        StartCoroutine(RandomStateGenerator());
    }
    
    /// <summary>
    /// Инициализирует систему обработки специальных смертей
    /// </summary>
    private void InitializeDeathHandlerSystem()
    {
        // Добавляем DeathHandlerManager как компонент, если его еще нет
        deathHandlerManager = GetComponent<DeathHandlerManager>();
        if (deathHandlerManager == null)
        {
            deathHandlerManager = gameObject.AddComponent<DeathHandlerManager>();
        }
    }

    /// <summary>
    /// Заменяем старый Update() с таймером на логику из EnemyHealth_OLD:
    /// - Генерация RndCritState
    /// - Если isDamage == true → HandleHighDamage()
    /// - Иначе, если isCriticalDamage == true → ActivateCriticalDamage()
    /// - Спавн кровавых декалей, когда здоровье упало ниже порога
    /// </summary>
    private void Update()
    {
        // Генерируем случайный RndCritState каждую итерацию (как в старом скрипте)
        //StateGenerator = UnityEngine.Random.Range(minNumber, maxNumber + 1);
        //animator.SetInteger("RndCritState", StateGenerator);
        

        if (isDamage)
        {
            HandleHighDamage();           // из старого EnemyHealth_OLD
        }
        else if (isCriticalDamage)
        {
            ActivateCriticalDamage();
        }

        // Если здоровье ниже порога И враг не мертв И прошло достаточно времени, спавним декаль
        if (IsLowOnHealth() && !isDead && !isHandlingDeath && 
            Vector3.Distance(transform.position, lastDecalPosition) > decalSpawnDistance &&
            Time.time >= lastBloodTrailTime + bloodTrailInterval)
        {
            CreateBloodDecalOnGround();
            lastDecalPosition = transform.position;
            lastBloodTrailTime = Time.time;
        }
        
        // Декали агонии отключены - никаких декалей во время isHandlingDeath
    }

    // Перегрузки TakeDamage остаются без изменений (как в новом скрипте)
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position, Vector3.zero, DamageType.Generic);
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        TakeDamage(damage, hitPoint, hitDirection, DamageType.Generic);
    }

    /// <summary>
    /// Весь новый функционал TakeDamage сохраняется: 
    /// - Blast-урон переводит сразу в рэгдолл
    /// - Создание декалей
    /// - Вычитание health
    /// - Проверка на смерть
    /// - Накопление damageCounter → проверка порога → вызов ActivateCriticalDamage или HandleHighDamage
    /// </summary>
    // EnemyHealth.cs
    public void TakeDamage(float damage,
                           Vector3 hitPoint,
                           Vector3 hitDirection,
                           DamageType damageType)
    {
        // Мертвые враги не получают урон (но isHandlingDeath могут для статус-эффектов)
        if (isDead)
            return;

        // Игнорируем Blast-вызов, если damage == 0
        if (damageType == DamageType.Blast && damage <= 0f)
            return;

        // Если Blast и damage > 0 → мгновенная смерть
        if (damageType == DamageType.Blast)
        {
            HandleBlastDeath(hitPoint);
            return;
        }

        // Обычный урон: создаём декали (только если не в процессе специальной смерти)
        if (!isHandlingDeath)
        {
            CreateDecalOnGround();
            Debug.Log($"Level Object Decal Container in TakeDamage: {levelObjectDecalContainer != null}");
            CreateDecalOnLevelObject(hitPoint, hitDirection);
        }

        // Вычитаем HP, обновляем и вызываем события
        health -= damage;
        UpdateHealthPercentage();
        OnTakeDamage?.Invoke(this);

        lastHitPoint = hitPoint;
        lastHitDirection = hitDirection.normalized;

        // Проверяем смерть
        if (health <= 0f)
        {
            HandleDeath(damageType, hitPoint, hitDirection);
        }

        // Накопление урона и критические эффекты (только для живых врагов)
        if (!isDead && !isHandlingDeath)
        {
            damageCounter += damage;
            if (damageCounter >= damageThreshold)
            {
                if (UnityEngine.Random.Range(1, 101) <= criticalDamageChance)
                    ActivateCriticalDamage();
                else if (!isDamage)
                    HandleHighDamage();
                damageCounter = 0f;
            }
        }
    }

    private void HandleBlastDeath(Vector3 blastOrigin)
    {
        isDead = true;
        TriggerDeathEvent();

        // Отключаем все контроллеры и анимацию
        capsuleCollider.enabled = false;
        enemyShootingScript.enabled = false;
        enemyAttack.enabled = false;
        enemyMovement.enabled = false;
        alertRadius.enabled = false;
        rotateForSearching.enabled = false;
        lookRegistrator.enabled = false;
        animator.enabled = false;
        navMeshAgent.enabled = false;

        // Включаем физику рэгдолла
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = false;
        }

        // Если pelvis не назначен, пытаемся найти первый Rigidbody в ragdollObject
        if (pelvisRigidbody == null)
        {
            pelvisRigidbody = ragdollObject.GetComponentInChildren<Rigidbody>();
            Debug.LogWarning("Pelvis Rigidbody was not assigned in Inspector. Using first found Rigidbody instead.");
        }

        if (pelvisRigidbody != null)
        {
            // Считаем направление от центра взрыва к тазу
            Vector3 outwardDir = (pelvisRigidbody.position - blastOrigin).normalized;

            // Случайная величина силы вверх
            float randomUpward = UnityEngine.Random.Range(blastUpwardForceMin, blastUpwardForceMax);
            Vector3 upImpulse = Vector3.up * randomUpward;
            pelvisRigidbody.AddForce(upImpulse, ForceMode.Impulse);

            // Случайная величина силы от центра
            float randomOutward = UnityEngine.Random.Range(blastOutwardForceMin, blastOutwardForceMax);
            Vector3 outImpulse = outwardDir * randomOutward;
            pelvisRigidbody.AddForce(outImpulse, ForceMode.Impulse);

            // Случайная величина торка (±)
            float randomTorqueMagnitude = UnityEngine.Random.Range(blastTorqueForceMin, blastTorqueForceMax);
            float randomSign = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            Vector3 torque = Vector3.up * (randomSign * randomTorqueMagnitude);
            pelvisRigidbody.AddTorque(torque, ForceMode.Impulse);
        }
        else
        {
            Debug.LogError("Pelvis Rigidbody is null, cannot apply blast impulse.");
        }

        // Создаём лужу крови через корутину
        StartCoroutine(CreateBloodPuddle());
    }

    /// <summary>
    /// Из старого EnemyHealth_OLD: 
    /// - Устанавливает флаг isDamage 
    /// - Отключает движение один раз 
    /// - Включает анимацию «isDamage» 
    /// (нет никакого таймера в Update(), поэтому «залипания» не будет)
    /// </summary>
    private void HandleHighDamage()
    {
        isDamage = true;                            // enter high-damage state (one-time flag)
        damageCounter = damageCooldown;             // сохраняем для совместимости, но не используем в Update()

        // Disable movement/animation-related scripts once when high damage occurs.
        if (enemyMovement != null)
            enemyMovement.enabled = false;          // disable movement component

        // Trigger the “isDamage” animation in Animator.
        if (animator != null)
            animator.SetBool("isDamage", true);     // set Animator flag for damage
    }

    /// <summary>
    /// Новый метод для критического урона: 
    /// отключает нужные компоненты, устанавливает флаг Animator, ждёт критический тайм, сбрасывает флаг
    /// (код из нового EnemyHealth.cs без изменений)
    /// </summary>
    private void ActivateCriticalDamage()
    {
        if (!isCriticalDamage) // Проверяем, не активирован ли уже критический урон
        {
            isCriticalDamage = true;
            int RndCritState;
            RndCritState = UnityEngine.Random.Range(MinCritState, MaxCritState);
            animator.SetInteger("RndCritState", RndCritState);
        }

        // Отключаем компоненты врага
        enemyMovement.enabled = false;
        enemyPatrol.enabled = false;
        lookRegistrator.enabled = false;
        
        // NavMeshAgent НЕ отключаем если враг в процессе специальной смерти
        if (!isHandlingDeath)
        {
            navMeshAgent.enabled = false;
        }

        // Устанавливаем флаг критического урона
        animator.SetBool("isCriticalDamage", true);
        

        // Запускаем сброс критического урона
        StartCoroutine(ResetCriticalDamage());
    }

    private IEnumerator ResetCriticalDamage()
    {
        yield return new WaitForSeconds(criticalStanceTime);
        isCriticalDamage = false;
        if (!isDead)
        {
            enemyMovement.enabled = true;
            lookRegistrator.enabled = true;
            navMeshAgent.enabled = true;
        }
        animator.SetBool("isCriticalDamage", false);
    }

    private void UpdateHealthPercentage()
    {
        HealthPercentage = (health / initialHealth) * 100f;
        HealthPercentage = Mathf.Clamp(HealthPercentage, 0f, 100f);
    }
    
    /// <summary>
    /// Обрабатывает смерть врага - выбирает специальную или обычную смерть
    /// </summary>
    /// <param name="damageType">Тип урона, который убил врага</param>
    /// <param name="hitPoint">Точка попадания</param>
    /// <param name="hitDirection">Направление удара</param>
    private void HandleDeath(DamageType damageType, Vector3 hitPoint, Vector3 hitDirection)
    {
        // Если уже мертв или уже обрабатываем смерть, игнорируем
        if (isDead || isHandlingDeath)
            return;
            
        Debug.Log($"HandleDeath вызван для {name} с типом урона: {damageType}");
        
        // Устанавливаем тип смерти для дебага
        SetDeathType(damageType);
        
        // Передаем обработку в DeathHandlerManager
        if (deathHandlerManager != null)
        {
            deathHandlerManager.HandleEnemyDeath(this, damageType, hitPoint, hitDirection);
        }
        else
        {
            Debug.LogWarning($"DeathHandlerManager не найден для {name}, используем обычную смерть");
            // Fallback - обычная смерть (сбрасываем финальную скорость)
            ResetFinalMoveSpeed();
            isDead = true;
            TriggerDeathEvent();
            
            if (UnityEngine.Random.value > 0.5f) DieAlternative();
            else Die();
        }
    }
    
    /// <summary>
    /// Публичный метод для вызова события смерти (для DeathHandlerManager)
    /// </summary>
    public void TriggerDeathEvent()
    {
        OnDeath?.Invoke(this);
    }
    
    /// <summary>
    /// Устанавливает тип урона для дебага (отображается в инспекторе)
    /// </summary>
    public void SetDeathType(DamageType damageType)
    {
        currentDeathType = damageType.ToString();
    }
    
    /// <summary>
    /// Устанавливает финальную скорость движения при специальной смерти
    /// </summary>
    public void SetFinalMoveSpeed(float speed)
    {
        finalMoveSpeed = speed;
        hadSpecialDeath = true;  // Помечаем что была специальная смерть
        Debug.Log($"Установлена финальная скорость {speed:F1} для {name} (специальная смерть)");
    }
    
    /// <summary>
    /// Сбрасывает финальную скорость (для обычной смерти без спецэффектов)
    /// </summary>
    private void ResetFinalMoveSpeed()
    {
        finalMoveSpeed = 0f;
        hadSpecialDeath = false;  // Сбрасываем флаг специальной смерти
    }
    
    /// <summary>
    /// Получает эффекты для специальной смерти по типу урона
    /// </summary>
    public DeathAttachment[] GetAttachmentsForDamageType(DamageType damageType)
    {
        var filtered = new System.Collections.Generic.List<DeathAttachment>();
        
        foreach (var attachment in handleDeathAttachments)
        {
            if (attachment != null && attachment.IsValid() && attachment.MatchesDamageType(damageType))
            {
                filtered.Add(attachment);
            }
        }
        
        return filtered.ToArray();
    }
    
    /// <summary>
    /// Спавнит эффекты специальной смерти на все ригидбоди врага постепенно
    /// </summary>
    public void SpawnDeathAttachments(DamageType damageType)
    {
        // Фильтруем эффекты по типу урона
        var attachments = GetAttachmentsForDamageType(damageType);
        if (attachments.Length == 0)
        {
            Debug.LogWarning($"Не найдены эффекты для типа урона {damageType} у {name}");
            return;
        }
        
        Debug.Log($"Спавним эффекты специальной смерти на {rigidbodies.Length} ригидбоди для {name}");
        
        // Запускаем постепенный спавн эффектов
        StartCoroutine(SpawnAttachmentsGradually(attachments));
    }
    
    /// <summary>
    /// Корутина для постепенного спавна эффектов в течение 0.75 секунд
    /// </summary>
    private IEnumerator SpawnAttachmentsGradually(DeathAttachment[] attachments)
    {
        float spawnDuration = 0.75f;
        float elapsedTime = 0f;
        
        // Собираем все возможные спавны заранее
        var allSpawns = new System.Collections.Generic.List<System.Tuple<Rigidbody, DeathAttachment>>();
        
        foreach (var rigidbody in rigidbodies)
        {
            if (rigidbody == null) continue;
            
            // Проверяем есть ли Damageable компонент
            var damageable = rigidbody.GetComponent<Damageable>();
            if (damageable == null)
            {
                Debug.LogWarning($"Ригидбоди {rigidbody.name} не содержит Damageable компонент");
                continue;
            }
            
            // Определяем количество эффектов для спавна
            int attachmentCount = UnityEngine.Random.Range(minAttachmentsPerRigidbody, maxAttachmentsPerRigidbody + 1);
            
            // Добавляем в список спавнов
            for (int i = 0; i < attachmentCount; i++)
            {
                var randomAttachment = attachments[UnityEngine.Random.Range(0, attachments.Length)];
                allSpawns.Add(new System.Tuple<Rigidbody, DeathAttachment>(rigidbody, randomAttachment));
            }
        }
        
        Debug.Log($"Всего запланировано {allSpawns.Count} эффектов для постепенного спавна");
        
        // Спавним эффекты постепенно
        int totalSpawns = allSpawns.Count;
        for (int i = 0; i < totalSpawns; i++)
        {
            var spawn = allSpawns[i];
            SpawnSingleAttachment(spawn.Item2, spawn.Item1);
            
            // Ждем перед следующим спавном
            float waitTime = spawnDuration / totalSpawns;
            yield return new WaitForSeconds(waitTime);
            elapsedTime += waitTime;
        }
        
        Debug.Log($"Завершен постепенный спавн за {elapsedTime:F2} секунд");
    }
    
    /// <summary>
    /// Спавнит один эффект на ригидбоди с улучшенным позиционированием и контролем времени жизни
    /// </summary>
    private void SpawnSingleAttachment(DeathAttachment attachment, Rigidbody rigidbody)
    {
        if (attachment == null || !attachment.IsValid() || rigidbody == null)
            return;
            
        // Спавним префаб
        var spawnedEffect = Instantiate(attachment.attachmentPrefab, rigidbody.transform);
        
        // Улучшенное рандомное позиционирование по всему объему коллайдера
        var collider = rigidbody.GetComponent<Collider>();
        if (collider != null)
        {
            // Используем bounds коллайдера для более широкого распределения
            Bounds bounds = collider.bounds;
            Vector3 randomPosition = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );
            
            // Конвертируем в локальные координаты
            spawnedEffect.transform.position = randomPosition;
        }
        
        // Устанавливаем случайный поворот
        spawnedEffect.transform.rotation = UnityEngine.Random.rotation;
        
        // Контролируем время жизни эффекта
        float lifetime = attachment.GetRandomLifetime();
        StartCoroutine(DestroyEffectAfterTime(spawnedEffect, lifetime));
        
        Debug.Log($"Заспавнен эффект {attachment.attachmentPrefab.name} на {rigidbody.name} (время жизни: {lifetime:F1}с) для {name}");
    }
    
    /// <summary>
    /// Корутина для уничтожения эффекта через заданное время
    /// </summary>
    private IEnumerator DestroyEffectAfterTime(GameObject effect, float lifetime)
    {
        if (effect == null) yield break;
        
        yield return new WaitForSeconds(lifetime);
        
        if (effect != null)
        {
            Debug.Log($"Уничтожаем эффект {effect.name} после {lifetime:F1}с жизни");
            Destroy(effect);
        }
    }

    public void Die()
    {
        isDead = true;

        if (AreLevelObjectsNearby())
            transitionDuration = 1f;

        // Отключаем компоненты
        capsuleCollider.enabled = false;
        enemyShootingScript.enabled = false;
        enemyAttack.enabled = false;
        enemyMovement.enabled = false;
        alertRadius.enabled = false;
        rotateForSearching.enabled = false;
        lookRegistrator.enabled = false;
        
        // Логика зависит от типа смерти
        if (hadSpecialDeath)
        {
            // СПЕЦИАЛЬНАЯ СМЕРТЬ: используем finalMoveSpeed
            if (finalMoveSpeed > 0f)
            {
                // Движение было - случайно выбираем RndState 5 или 9
                int randomDeathState = UnityEngine.Random.value > 0.5f ? 5 : 9;
                animator.SetInteger("RndState", randomDeathState);
                animator.SetBool("isDead", true);
                Debug.Log($"Специальная смерть в движении: RndState = {randomDeathState} для {name}");
            }
            else
            {
                // Специальная смерть без движения
                animator.SetBool("isDead", true);
                Debug.Log($"Специальная смерть без движения: обычный регдолл для {name}");
            }
        }
        else
        {
            // ОБЫЧНАЯ СМЕРТЬ: всегда применяем импульс как раньше
            animator.SetBool("isDead", true);
            Debug.Log($"Обычная смерть: стандартный импульс регдолла для {name}");
        }
        
        navMeshAgent.enabled = false;

        StartCoroutine(TransitionToRagdoll());
        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb.gameObject.GetComponent<RagdollCollisionDetector>() == null)
                rb.gameObject.AddComponent<RagdollCollisionDetector>();
            rb.isKinematic = false;
        }

        // Логика импульса для Die() зависит от типа смерти
        if (hadSpecialDeath)
        {
            // Специальная смерть: применяем импульс только если скорость была > 0
            if (finalMoveSpeed > 0f)
            {
                // Для Die() используется TransitionToRagdoll(), поэтому импульс будет применен там
                Debug.Log($"Die(): Специальная смерть в движении, импульс через TransitionToRagdoll()");
            }
            else
            {
                Debug.Log($"Die(): Специальная смерть без движения, без импульса");
            }
        }
        else
        {
            // Обычная смерть: для Die() импульс применяется через TransitionToRagdoll()
            Debug.Log($"Die(): Обычная смерть, импульс через TransitionToRagdoll()");
        }

        StartCoroutine(CreateBloodPuddle());
    }

    public void DieAlternative()
    {
        isDead = true;

        capsuleCollider.enabled = false;
        enemyShootingScript.enabled = false;
        enemyAttack.enabled = false;
        enemyMovement.enabled = false;
        alertRadius.enabled = false;
        rotateForSearching.enabled = false;
        lookRegistrator.enabled = false;
        
        // Логика зависит от типа смерти
        if (hadSpecialDeath)
        {
            // СПЕЦИАЛЬНАЯ СМЕРТЬ: используем finalMoveSpeed
            if (finalMoveSpeed > 0f)
            {
                // Движение было - случайно выбираем RndState 5 или 9
                int randomDeathState = UnityEngine.Random.value > 0.5f ? 5 : 9;
                animator.SetInteger("RndState", randomDeathState);
                animator.SetBool("isDead", true);
                Debug.Log($"Альтернативная специальная смерть в движении: RndState = {randomDeathState} для {name}");
            }
            else
            {
                // Специальная смерть без движения
                Debug.Log($"Альтернативная специальная смерть без движения: обычный регдолл для {name}");
            }
        }
        else
        {
            // ОБЫЧНАЯ СМЕРТЬ: всегда применяем импульс как раньше
            Debug.Log($"Альтернативная обычная смерть: стандартный импульс регдолла для {name}");
        }
        
        // NavMeshAgent отключается только при финальной смерти (не при специальной)
        navMeshAgent.enabled = false;
        animator.enabled = false;

        foreach (Rigidbody rb in rigidbodies)
            rb.isKinematic = false;

        // Логика импульса зависит от типа смерти
        if (hadSpecialDeath)
        {
            // Специальная смерть: применяем импульс только если скорость была > 0
            if (finalMoveSpeed > 0f)
            {
                ragdollImpulseForce = ForceRagdoll;
                ApplyImpulse();
                Debug.Log($"Применен импульс для специальной смерти в движении: {finalMoveSpeed:F1}");
            }
        }
        else
        {
            // Обычная смерть: всегда применяем импульс (как было раньше)
            ragdollImpulseForce = ForceRagdoll;
            ApplyImpulse();
            Debug.Log($"Применен стандартный импульс для обычной смерти");
        }

        StartCoroutine(CreateBloodPuddle());
    }

    private void ApplyImpulse()
    {
        Vector3 impulseDirection = (lastHitDirection + impulseAdjustment).normalized;

        foreach (Rigidbody rb in rigidbodies)
        {
            float distance = Vector3.Distance(rb.transform.position, lastHitPoint);
            float impulseScale = 1f / (1f + distance);
            Vector3 force = impulseDirection * ragdollImpulseForce * impulseScale;
            rb.AddForceAtPosition(force, lastHitPoint, ForceMode.Impulse);
            Debug.Log($"Applying force {force} to {rb.name} due to hit at {lastHitPoint}");

            // Apply torque randomly along X or Z
            Vector3 torqueDirection = UnityEngine.Random.value > 0.5f ? Vector3.right : Vector3.forward;
            rb.AddTorque(torqueDirection * torqueForce, ForceMode.Impulse);
        }
    }

    private void CreateBloodDecalOnGround()
    {
        // Запрещаем декали во время специальной смерти
        if (isHandlingDeath) return;
        
        if (decalContainer == null) return;

        RaycastHit hit;
        float heightOffset = 0.1f;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                float randomYRotation = UnityEngine.Random.Range(0f, 360f);
                Quaternion decalRotation = Quaternion.Euler(0f, randomYRotation, 0f) * Quaternion.FromToRotation(Vector3.up, hit.normal);
                Vector3 decalPosition = hit.point + hit.normal * heightOffset;
                GameObject decal = decalContainer.CreateRandomDecal(decalPosition, decalRotation);
                if (decal != null)
                {
                    float randomScaleFactor = UnityEngine.Random.Range(0.15f, 0.5f);
                    decal.transform.localScale *= randomScaleFactor;
                    Destroy(decal, 120f);

                    Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), 0, UnityEngine.Random.Range(-0.1f, 0.1f));
                    decal.transform.position += randomOffset;

                    Destroy(decal, 30f);
                }
            }
        }
    }

    private void CreateDecalOnGround()
    {
        // Запрещаем декали во время специальной смерти
        if (isHandlingDeath) return;
        
        if (decalContainer == null) return;

        RaycastHit hit;
        float heightOffset = 0.1f;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                float randomYRotation = UnityEngine.Random.Range(0f, 360f);
                Quaternion decalRotation = Quaternion.Euler(0f, randomYRotation, 0f) * Quaternion.FromToRotation(Vector3.up, hit.normal);
                Vector3 decalPosition = hit.point + hit.normal * heightOffset;
                GameObject decal = decalContainer.CreateRandomDecal(decalPosition, decalRotation);

                if (decal != null)
                {
                    float randomScaleFactor = UnityEngine.Random.Range(0.25f, 1.5f);
                    decal.transform.localScale *= randomScaleFactor;

                    Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), 0, UnityEngine.Random.Range(-0.25f, 0.25f));
                    decal.transform.position += randomOffset;
                    Destroy(decal, 90f);
                }
            }
            else
            {
                Debug.Log("Нет Ground");
            }
        }
    }

    private void CreateDecalOnLevelObject(Vector3 hitPoint, Vector3 hitDirection)
    {
        // Запрещаем декали во время специальной смерти
        if (isHandlingDeath) return;
        
        if (levelObjectDecalContainer == null)
        {
            Debug.Log("levelObjectDecalContainer is not assigned");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(hitPoint, hitDirection, out hit, levelObjectDistance))
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            int wallsLayer = LayerMask.NameToLayer("LevelWalls");

            if (hit.collider.gameObject.layer == groundLayer || hit.collider.gameObject.layer == wallsLayer)
            {
                PlaceDecal(hit);
            }
            else
            {
                Vector3 exitPoint = hit.point + hitDirection * 0.1f;
                if (Physics.Raycast(exitPoint, hitDirection, out hit, levelObjectDistance))
                {
                    if (hit.collider.gameObject.layer == groundLayer || hit.collider.gameObject.layer == wallsLayer)
                    {
                        PlaceDecal(hit);
                    }
                }
            }
        }
    }

    private void PlaceDecal(RaycastHit hit)
    {
        Debug.Log($"Placing decal at position: {hit.point}, Normal: {hit.normal}");
        float randomYRotation = UnityEngine.Random.Range(0f, 360f);
        Quaternion decalRotation = Quaternion.LookRotation(-hit.normal) * Quaternion.Euler(-90f, 0f, 0f);
        Vector3 decalPosition = hit.point + hit.normal * decalYOffset;

        GameObject decal = levelObjectDecalContainer.CreateRandomDecal(decalPosition, decalRotation);
        if (decal != null)
        {
            float randomScaleFactor = UnityEngine.Random.Range(1f, 1.5f);
            decal.transform.localScale *= randomScaleFactor;
            Destroy(decal, 90f);
        }
        else
        {
            Debug.Log("Failed to create decal");
        }
    }

    private IEnumerator CreateBloodPuddle()
    {
        yield return new WaitForSeconds(2f); // Ждем 2 секунды

        // Запрещаем лужи крови во время специальной смерти
        if (isHandlingDeath) yield break;

        if (bloodPuddleDecalContainer == null || bloodPuddleParentObject == null)
            yield break;

        RaycastHit hit;
        if (Physics.Raycast(bloodPuddleParentObject.transform.position, -Vector3.up, out hit))
        {
            if (hit.collider.CompareTag("Ground"))
            {
                float randomYRotation = UnityEngine.Random.Range(0f, 360f);
                Quaternion decalRotation = Quaternion.Euler(0f, randomYRotation, 0f) * Quaternion.FromToRotation(Vector3.up, hit.normal);
                Vector3 decalPosition = hit.point + hit.normal * decalYOffset;

                GameObject bloodPuddle = bloodPuddleDecalContainer.CreateRandomDecal(decalPosition, decalRotation);
                if (bloodPuddle != null)
                {
                    float targetScale = UnityEngine.Random.Range(bloodPuddleMinScale, bloodPuddleMaxScale);
                    float growthRate = UnityEngine.Random.Range(minPuddleGrowthRate, maxPuddleGrowthRate);
                    StartCoroutine(ScaleDecalOverTime(bloodPuddle, targetScale, growthRate));
                }
            }
        }
    }

    private IEnumerator ScaleDecalOverTime(GameObject decal, float targetScale, float initialGrowthRate)
    {
        Vector3 initialScale = Vector3.zero;
        Vector3 finalScale = new Vector3(targetScale, targetScale, targetScale);

        float progress = 0f;
        while (progress < 1f)
        {
            float currentGrowthRate = initialGrowthRate * (1f - progress);
            progress += Time.deltaTime * currentGrowthRate;

            decal.transform.localScale = Vector3.Lerp(initialScale, finalScale, progress);
            yield return null;
        }
    }

    public void DisableAnimatorImmediately()
    {
        animator.enabled = false;
    }

    private IEnumerator TransitionToRagdoll()
    {
        bool collisionDetected = false;

        Action<Vector3> onCollision = (contactPoint) =>
        {
            collisionDetected = true;
            DisableAnimatorImmediately();

            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = false;
                Vector3 direction = (rb.transform.position - contactPoint).normalized;
                rb.AddForce(direction * ragdollImpulseForce, ForceMode.Impulse);
            }
        };

        foreach (Rigidbody rb in rigidbodies)
        {
            var collisionDetector = rb.gameObject.GetComponent<RagdollCollisionDetector>();
            if (collisionDetector == null)
                collisionDetector = rb.gameObject.AddComponent<RagdollCollisionDetector>();
            collisionDetector.OnCollisionDetected += onCollision;
        }

        float transitionProgress = 0f;
        while (transitionProgress < 1f && !collisionDetected)
        {
            transitionProgress += Time.deltaTime / transitionDuration;
            yield return null;
        }

        foreach (Rigidbody rb in rigidbodies)
        {
            var collisionDetector = rb.gameObject.GetComponent<RagdollCollisionDetector>();
            if (collisionDetector != null)
                collisionDetector.OnCollisionDetected -= onCollision;
        }

        if (!collisionDetected)
        {
            animator.enabled = false;
            foreach (var rb in rigidbodies)
                rb.isKinematic = false;
            
            // Для обычной смерти всегда применяем импульс даже без столкновения
            if (!hadSpecialDeath)
            {
                // Применяем стандартный импульс для обычной смерти
                ApplyImpulse();
                Debug.Log($"TransitionToRagdoll: Применен стандартный импульс для обычной смерти");
            }
            else
            {
                // Для специальной смерти применяем импульс только если была скорость
                if (finalMoveSpeed > 0f)
                {
                    ApplyImpulse();
                    Debug.Log($"TransitionToRagdoll: Применен импульс для специальной смерти в движении: {finalMoveSpeed:F1}");
                }
                else
                {
                    Debug.Log($"TransitionToRagdoll: Специальная смерть без движения, импульс не применен");
                }
            }
        }
    }

    private void EnableRagdoll()
    {
        animator.enabled = false;

        foreach (Rigidbody rb in ragdollObject.GetComponentsInChildren<Rigidbody>())
            rb.isKinematic = false;
    }

    public bool IsLowOnHealth()
    {
        return health <= initialHealth * 0.3f;
    }

    public void UpdateCurrentAnimation(string animation)
    {
        currentAnimation = animation;
    }

    public int GenerateRandomNumber()
    {
        return UnityEngine.Random.Range(minNumber, maxNumber);
    }

    /// <summary>
    /// Корутина из старого EnemyHealth_OLD: 
    /// рандомно меняем RndState раз в 0.5 секунды, 
    /// но только если враг жив и не в состоянии урона.
    /// </summary>
    private IEnumerator RandomStateGenerator()
    {
        while (true)
        {
            if (!isDead && !isDamage && !isCriticalDamage)
            {
                StateGenerator = UnityEngine.Random.Range(minNumber, maxNumber + 1);
                animator.SetInteger("RndState", StateGenerator);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// Применяет специальные текстуры смерти на все Damageable компоненты
    /// </summary>
    public void ApplySpecialDeathTextures(DamageType damageType)
    {
        if (rigidbodies == null || rigidbodies.Length == 0)
        {
            Debug.LogWarning($"Нет ригидбоди для применения специальных текстур у {name}");
            return;
        }
        
        int appliedCount = 0;
        foreach (var rigidbody in rigidbodies)
        {
            if (rigidbody == null) continue;
            
            // Получаем Damageable компонент
            var damageable = rigidbody.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.ApplySpecialDeathTexture(damageType);
                appliedCount++;
            }
        }
        
        Debug.Log($"Применены специальные текстуры типа {damageType} на {appliedCount} Damageable компонентов у {name}");
    }
    
    /// <summary>
    /// Сбрасывает специальные текстуры обратно к начальным на всех Damageable (МЕТОД НЕ ИСПОЛЬЗУЕТСЯ - ТЕКСТУРЫ ОСТАЮТСЯ НАВСЕГДА)
    /// </summary>
    public void ResetSpecialDeathTextures()
    {
        Debug.LogWarning($"ResetSpecialDeathTextures вызван для {name}, но текстуры специальной смерти должны оставаться НАВСЕГДА!");
        
        // ЗАКОММЕНТИРОВАНО: текстуры остаются навсегда
        /*
        if (rigidbodies == null || rigidbodies.Length == 0)
            return;
            
        foreach (var rigidbody in rigidbodies)
        {
            if (rigidbody == null) continue;
            
            var damageable = rigidbody.GetComponent<Damageable>();
            if (damageable != null)
            {
                damageable.ResetToInitialTextures();
            }
        }
        
        Debug.Log($"Сброшены специальные текстуры к начальным у {name}");
        */
    }

    private bool AreLevelObjectsNearby()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        groundLayerDetected = false;
        groundTagDetected = false;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, checkSphereRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider != null && hitCollider.enabled)
            {
                if (hitCollider.gameObject.layer == groundLayer)
                {
                    groundLayerDetected = true;
                    if (!hitCollider.CompareTag("Ground"))
                    {
                        Debug.Log($"Nearby object on Ground layer with valid tag found: {hitCollider.tag}");
                        return true;
                    }
                    else
                    {
                        groundTagDetected = true;
                    }
                }
            }
        }
        Debug.Log("No nearby objects on Ground layer with valid tags found.");
        return false;
    }
}
