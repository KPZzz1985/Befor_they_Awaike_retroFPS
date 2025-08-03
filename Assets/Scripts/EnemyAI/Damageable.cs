using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Маппинг текстуры на тип урона для специальной смерти
/// </summary>
[System.Serializable]
public class DamageTypeTexture
{
    [Header("Special Death Texture Mapping")]
    [Tooltip("Текстура для применения при специальной смерти")]
    public Texture specialDeathTexture;
    
    [Tooltip("Тип урона для которого применяется эта текстура")]
    public DamageType damageType;
    
    /// <summary>
    /// Проверяет, подходит ли эта текстура для данного типа урона
    /// </summary>
    public bool MatchesDamageType(DamageType targetType)
    {
        return damageType == targetType;
    }
    
    /// <summary>
    /// Проверяет, валидна ли настройка
    /// </summary>
    public bool IsValid()
    {
        return specialDeathTexture != null;
    }
}

/// <summary>
/// Damageable handles per-part damage logic, dismemberment, decal spawning, and relays damage to EnemyHealth.
/// Now supports two types of damage: Generic and Blast. If Blast damage is received, it forwards the type to EnemyHealth.
/// </summary>
public class Damageable : MonoBehaviour
{
    [Header("References")]
    public EnemyHealth enemyHealth;            // Reference to parent EnemyHealth
    public Damageable linkedPart;              // If this part is linked to another part
    public Animator animator;                  // Animator for destruction trigger
    public string destroyTriggerName;          // Trigger name to play on part destruction

    [Header("Health Settings")]
    [SerializeField] private float healthMultiplier = 1.1f;
    [SerializeField] public float partHealth;          // Current health of this part
    [SerializeField] public bool isPartDestroyed;      // Flag indicating if this part is destroyed
    [SerializeField] public float initialPublicHealth; // Parent enemy initial health, for scaling
    [SerializeField] private bool isDead;              // Cached from EnemyHealth

    [Header("Dismemberment Settings")]
    [SerializeField] public GameObject[] destroyedObjects;     // Objects to destroy when part is destroyed
    [SerializeField] public GameObject[] DismemberingObjects;  // Objects to spawn on dismemberment
    [SerializeField] public GameObject attachedObject;         // Object to attach on destruction
    [SerializeField] private float torqueForce = 15f;           // Force applied for random torque when dismembering
    public bool randomPositionSpawn;                            // Whether to spawn at random position within collider

    [Header("Spawned Object Settings")]
    [SerializeField] public float spawnForceMultiplier = 1f;    // Force multiplier for spawned dismembered objects
    [SerializeField] public Vector3 dismemberingForce;          // Base force for spawned dismembered objects
    [SerializeField] public float SpawnObjectsLiftime = 7f;     // Lifetime of spawned objects
    [SerializeField] public int RandomSpawnMin = 3;             // Minimum delay before destroying spawned object
    [SerializeField] public int RandomSpawnMax = 5;             // Maximum delay before destroying spawned object

    [Header("Wound Texture Settings")]
    public SkinnedMeshRenderer[] WoundMeshes;  // Meshes that will change texture on damage
    public Texture[] WoundTextures;            // Array of textures to cycle through on damage
    private int meshToChangeIndex = 0;         // Index of mesh to change next
    private int[] textureIndices;              // Indices of current texture for each mesh
    
    [Header("Special Death Texture Settings")]
    [Tooltip("Маппинг текстур для разных типов специальной смерти")]
    [SerializeField] private DamageTypeTexture[] specialDeathTextures = new DamageTypeTexture[0];
    
    [Header("Debug Info")]
    [Tooltip("Текущий тип специальной смерти (только для отображения)")]
    [SerializeField] private string currentSpecialDeathType = "None";

    private bool spawnAllowed = true;          // To prevent multiple attached object spawns
    private bool hasDismemberedObjectSpawned = false;
    private bool hasAttachedObjectSpawned = false;
    private bool hasSpecialDeathTexture = false;  // Флаг: применена ли специальная текстура смерти
    private Vector3 spawnPosition;             // Computed spawn position for dismembered objects

    private Queue<float> damageQueue = new Queue<float>();  // Queue of incoming damage amounts

    private void Awake()
    {
        healthMultiplier = Mathf.Max(healthMultiplier, 0.1f);
        textureIndices = new int[WoundMeshes.Length];
        SetInitialTextures();
    }

    /// <summary>
    /// Apply initial wound texture to each mesh.
    /// </summary>
    private void SetInitialTextures()
    {
        for (int i = 0; i < WoundMeshes.Length; i++)
        {
            if (WoundTextures.Length > 0 && WoundMeshes[i] != null)
            {
                WoundMeshes[i].materials[0].SetTexture("_MainTex", WoundTextures[0]);
                textureIndices[i] = 0;
            }
        }
    }

    /// <summary>
    /// Generic damage method: enqueues damage and processes in coroutine.
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isPartDestroyed) return;

        damageQueue.Enqueue(amount);

        if (damageQueue.Count == 1)
        {
            StartCoroutine(ProcessDamageQueue(hitPoint, hitDirection));
        }
    }

    /// <summary>
    /// Blast damage overload: enqueues damage and processes in coroutine, forwarding DamageType.
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, DamageType damageType)
    {
        if (isPartDestroyed) return;

        damageQueue.Enqueue(amount);

        if (damageQueue.Count == 1)
        {
            StartCoroutine(ProcessDamageQueueWithType(hitPoint, hitDirection, damageType));
        }
    }

    /// <summary>
    /// Processes queued Generic damage, one per frame.
    /// </summary>
    private IEnumerator ProcessDamageQueue(Vector3 hitPoint, Vector3 hitDirection)
    {
        while (damageQueue.Count > 0)
        {
            float damageAmount = damageQueue.Dequeue();
            ApplyDamage(damageAmount, hitPoint, hitDirection);
            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// Processes queued damage with DamageType, one per frame.
    /// </summary>
    private IEnumerator ProcessDamageQueueWithType(Vector3 hitPoint, Vector3 hitDirection, DamageType damageType)
    {
        while (damageQueue.Count > 0)
        {
            float damageAmount = damageQueue.Dequeue();
            ApplyDamageWithType(damageAmount, hitPoint, hitDirection, damageType);
            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// Applies Generic damage: reduces partHealth, forwards to EnemyHealth, and updates wound texture.
    /// </summary>
    private void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        partHealth -= amount;

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(amount, hitPoint, hitDirection);
        }

        ChangeWoundTextureOnDamage();
    }

    /// <summary>
    /// Applies damage with DamageType: reduces partHealth, forwards to EnemyHealth with type, and updates wound texture.
    /// </summary>
    private void ApplyDamageWithType(float amount, Vector3 hitPoint, Vector3 hitDirection, DamageType damageType)
    {
        partHealth -= amount;

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(amount, hitPoint, hitDirection, damageType);
        }

        ChangeWoundTextureOnDamage();
    }

    /// <summary>
    /// Cycles wound textures on each hit.
    /// НЕ меняет текстуры если уже применена специальная текстура смерти.
    /// </summary>
    private void ChangeWoundTextureOnDamage()
    {
        // БЛОКИРУЕМ смену текстур если уже применена специальная текстура смерти
        if (hasSpecialDeathTexture)
        {
            Debug.Log($"ChangeWoundTextureOnDamage заблокирован на {name} - уже применена специальная текстура смерти ({currentSpecialDeathType})");
            return;
        }
        
        if (WoundMeshes == null || WoundMeshes.Length == 0) return;

        if (meshToChangeIndex >= WoundMeshes.Length)
        {
            meshToChangeIndex = 0;
        }

        SkinnedMeshRenderer meshRenderer = WoundMeshes[meshToChangeIndex];
        if (meshRenderer == null) return;

        if (textureIndices == null || textureIndices.Length <= meshToChangeIndex) return;

        if (WoundTextures.Length > textureIndices[meshToChangeIndex] + 1)
        {
            textureIndices[meshToChangeIndex]++;
            if (meshRenderer.materials != null && meshRenderer.materials.Length > 0)
            {
                meshRenderer.materials[0].SetTexture("_MainTex", WoundTextures[textureIndices[meshToChangeIndex]]);
            }
        }

        meshToChangeIndex = (meshToChangeIndex + 1) % WoundMeshes.Length;
    }
    
    /// <summary>
    /// Применяет специальную текстуру смерти для данного типа урона
    /// </summary>
    public void ApplySpecialDeathTexture(DamageType damageType)
    {
        // Ищем подходящую текстуру для типа урона
        DamageTypeTexture matchingTexture = null;
        foreach (var textureMapping in specialDeathTextures)
        {
            if (textureMapping != null && textureMapping.IsValid() && textureMapping.MatchesDamageType(damageType))
            {
                matchingTexture = textureMapping;
                break;
            }
        }
        
        if (matchingTexture == null)
        {
            Debug.LogWarning($"Не найдена специальная текстура для типа урона {damageType} на {name}");
            return;
        }
        
        // Применяем текстуру ко всем мешам
        int appliedCount = 0;
        for (int i = 0; i < WoundMeshes.Length; i++)
        {
            if (WoundMeshes[i] != null && WoundMeshes[i].materials != null && WoundMeshes[i].materials.Length > 0)
            {
                WoundMeshes[i].materials[0].SetTexture("_MainTex", matchingTexture.specialDeathTexture);
                appliedCount++;
            }
        }
        
        // Обновляем дебаг информацию и устанавливаем флаг защиты
        currentSpecialDeathType = damageType.ToString();
        hasSpecialDeathTexture = true;  // БЛОКИРУЕМ дальнейшие смены текстур НАВСЕГДА
        
        Debug.Log($"Применена НАВСЕГДА специальная текстура {matchingTexture.specialDeathTexture.name} для типа {damageType} на {appliedCount} мешей компонента {name}. Обычные текстуры больше не будут меняться!");
    }
    
    /// <summary>
    /// Сбрасывает специальную текстуру обратно к начальной (МЕТОД ОСТАВЛЕН ДЛЯ СОВМЕСТИМОСТИ, НО НЕ ИСПОЛЬЗУЕТСЯ)
    /// </summary>
    public void ResetToInitialTextures()
    {
        SetInitialTextures();
        currentSpecialDeathType = "None";
        hasSpecialDeathTexture = false;  // Сбрасываем флаг защиты (НО ЭТОТ МЕТОД НЕ ДОЛЖЕН ВЫЗЫВАТЬСЯ)
        Debug.Log($"Сброшены текстуры к начальным на {name} (НО ЭТОГО НЕ ДОЛЖНО ПРОИСХОДИТЬ - ТЕКСТУРЫ ДОЛЖНЫ ОСТАТЬСЯ НАВСЕГДА)");
    }

    private void Update()
    {
        if (enemyHealth != null)
        {
            isDead = enemyHealth.isDead;
            initialPublicHealth = enemyHealth.initialHealth;

            if (partHealth == 0)
            {
                partHealth = initialPublicHealth * healthMultiplier;
            }
        }

        isPartDestroyed = partHealth <= 0 || (linkedPart != null && linkedPart.isPartDestroyed);
        if (isPartDestroyed)
        {
            SetSpawnPosition();
        }

        PartDestroyed();
    }

    /// <summary>
    /// Determines spawn position for dismembered objects: random within collider or at part's position.
    /// </summary>
    private void SetSpawnPosition()
    {
        if (randomPositionSpawn)
        {
            Collider col = GetComponent<Collider>();
            spawnPosition = col != null
                ? new Vector3(
                    Random.Range(col.bounds.min.x, col.bounds.max.x),
                    Random.Range(col.bounds.min.y, col.bounds.max.y),
                    Random.Range(col.bounds.min.z, col.bounds.max.z)
                  )
                : transform.position;
        }
        else
        {
            spawnPosition = transform.position;
        }
    }

    /// <summary>
    /// Handles actual destruction, dismemberment, spawning of debris, and triggering animator.
    /// </summary>
    private void PartDestroyed()
    {
        if (!isPartDestroyed) return;

        // Destroy designated objects
        foreach (GameObject obj in destroyedObjects)
        {
            if (obj != null) Destroy(obj);
        }

        // Spawn dismembering objects once
        if (!hasDismemberedObjectSpawned && DismemberingObjects != null)
        {
            foreach (GameObject dismemberingObject in DismemberingObjects)
            {
                if (dismemberingObject != null)
                {
                    SpawnDismemberingObject(dismemberingObject);
                }
            }
            hasDismemberedObjectSpawned = true;
        }

        // Spawn attached object once when partHealth < 0
        if (attachedObject != null && partHealth < 0 && spawnAllowed && !hasAttachedObjectSpawned)
        {
            SpawnAttachedObject();
            hasAttachedObjectSpawned = true;
        }

        // Trigger animator if exists
        if (animator != null)
        {
            animator.SetTrigger(destroyTriggerName);
        }

        // Enable DecalTrailCreator if present
        DecalTrailCreator decalTrailCreator = GetComponent<DecalTrailCreator>();
        if (decalTrailCreator != null && !decalTrailCreator.enabled)
        {
            decalTrailCreator.enabled = true;
            Debug.Log("DecalTrailCreator enabled due to part destruction.");
        }
    }

    /// <summary>
    /// Instantiates a dismembering piece and applies force and random torque.
    /// </summary>
    private void SpawnDismemberingObject(GameObject objectToSpawn)
    {
        Quaternion spawnRotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
        GameObject spawnedObject = Instantiate(objectToSpawn, spawnPosition, spawnRotation);
        Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(dismemberingForce * spawnForceMultiplier, ForceMode.Impulse);
            StartCoroutine(AddRandomTorque(rb));
        }
    }

    /// <summary>
    /// Adds random torque to a Rigidbody after a random delay between 0.5 and 1 second.
    /// </summary>
    private IEnumerator AddRandomTorque(Rigidbody rb)
    {
        float delay = Random.Range(0.5f, 1f);
        yield return new WaitForSeconds(delay);

        Vector3 randomTorqueDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;

        Vector3 randomTorque = randomTorqueDirection * torqueForce;
        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }

    /// <summary>
    /// Spawns an attached object (e.g., gore effect) at the part's position and schedules its destruction.
    /// </summary>
    private void SpawnAttachedObject()
    {
        spawnAllowed = false;

        GameObject spawnedObject = Instantiate(attachedObject, transform.position, Quaternion.identity, transform);
        spawnedObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        StartCoroutine(DestroySpawnedObject(spawnedObject));
        spawnAllowed = true;
    }

    /// <summary>
    /// Destroys a spawned object after a random duration between RandomSpawnMin and RandomSpawnMax.
    /// </summary>
    private IEnumerator DestroySpawnedObject(GameObject objectToDestroy)
    {
        yield return new WaitForSeconds(Random.Range(RandomSpawnMin, RandomSpawnMax));
        if (objectToDestroy != null)
        {
            Destroy(objectToDestroy);
        }
    }
}
