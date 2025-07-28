using UnityEngine;
using System.Collections; // для IEnumerator
using System.Collections.Generic; // для List

public class BlastProjectile : MonoBehaviour
{
    [Header("Explosion Settings")]
    [Tooltip("Radius of the explosion effect: within this distance damage is full.")]
    public float radius = 5f;

    [Tooltip("Base damage dealt by the explosion at epicenter.")]
    public float damage = 50f;

    [Tooltip("Layers that should be affected by the explosion (e.g. Enemy, Player).")]
    public LayerMask affectedLayers;

    [Tooltip("Optional particle effect prefab to instantiate at explosion center.")]
    public GameObject explosionEffectPrefab;

    [Tooltip("Time (in seconds) before this projectile is destroyed after explosion.")]
    public float destroyDelay = 0.1f;

    [Header("Expanding Sphere Settings")]
    [Tooltip("Reference to the MeshFilter on this GameObject (should be a Sphere).")]
    public MeshFilter sphereMeshFilter;

    [Tooltip("Material to use for the expanding ring (semi-transparent).")]
    public Material explosionRingMaterial;

    [Tooltip("Time (in seconds) over which the sphere expands (and damage falls off).")]
    public float expandDuration = 0.2f;

    [Tooltip("Maximum scale for the sphere (localScale). Determines the farthest damage reach.")]
    public float maxExpandScale = 10f;

    [Tooltip("Exponent for damage falloff curve; higher values => faster drop-off.")]
    public float falloffExponent = 1f;

    [Tooltip("Если включено, враги будут блокировать урон другим врагам позади")]
    public bool enemiesBlockEachOther = false;

    // List of prefabs to spawn on surfaces after explosion.
    [Tooltip("Список префабов, которые будут спавниться на поверхностях после взрыва")]
    public GameObject[] surfacePrefabs;

    // Minimum and maximum count of prefabs to spawn (inclusive).
    [Tooltip("Минимальное количество объектов для спавна")]
    public int minSpawnCount = 1;
    [Tooltip("Максимальное количество объектов для спавна")]
    public int maxSpawnCount = 3;

    // Minimum and maximum lifetime (in seconds) for each spawned prefab.
    [Tooltip("Минимальное время жизни спавненного объекта")]
    public float minPrefabLifeTime = 1f;
    [Tooltip("Максимальное время жизни спавненного объекта")]
    public float maxPrefabLifeTime = 3f;

    

    // LayerMask для поверхностей, на которых может появляться префаб (включает ВСЕ слои, кроме Enemy).
    [Tooltip("Слои, на поверхностях которых будут спавниться префабы (исключая Enemy)")]
    public LayerMask surfaceSpawnLayers;

    private bool isArmed = false;   // флаг «вооружённости» (предотвращаем мгновенный взрыв при спавне)
    private bool hasExploded = false;

    private void Start()
    {
        // Ждём одну FixedUpdate (~0.02 с), чтобы снаряд отлетел и не взорвался сразу у точки спавна
        StartCoroutine(ArmAfterDelay());
    }

    private IEnumerator ArmAfterDelay()
    {
        yield return new WaitForSeconds(0.02f);
        isArmed = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Если ещё не «вооружён» или уже взорвался — ничего не делаем
        if (!isArmed || hasExploded)
            return;

        // Проверяем, принадлежит ли слой столкнувшегося объекта маске affectedLayers
        if (((1 << collision.gameObject.layer) & affectedLayers) == 0)
            return;

        hasExploded = true;
        Explode();
    }

    private void Explode()
    {
        Vector3 explosionCenter = transform.position;

        // 1) Спавним префаб VFX, если он указан
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, explosionCenter, Quaternion.identity);
        }

        // 2) Собираем всех коллайдеров внутри maxExpandScale (они могут быть врагами, игроком и т.д.)
        Collider[] colliders = Physics.OverlapSphere(explosionCenter, maxExpandScale, affectedLayers);

        // 3) Если есть визуальный эффект расширяющейся сферы — запускаем корутину.
        //    Иначе — сразу наносим урон одним разом, но с учётом преград.
        if (sphereMeshFilter != null && explosionRingMaterial != null)
        {
            StartCoroutine(ShowExpandingSphereAndDamage(explosionCenter, colliders));
        }
        else
        {
            // Наносим урон сразу: сначала по врагам с видимостью, затем по игроку (без проверки IsEnemyVisible)
            foreach (Collider hitCollider in colliders)
            {
                Transform hitTransform = hitCollider.transform;

                // 1) Проверяем EnemyHealth
                EnemyHealth eh = hitTransform.GetComponentInParent<EnemyHealth>();
                if (eh != null)
                {
                    // Если враг «видим» — бьём
                    if (IsEnemyVisible(hitTransform, explosionCenter))
                    {
                        ApplyFalloffDamage(hitCollider, explosionCenter);
                    }
                    continue;
                }

                // 2) Проверяем Damageable (может это любой другой дамажабельный объект, включая Player)
                Damageable damageable = hitCollider.GetComponent<Damageable>();
                if (damageable != null)
                {
                    //Footnote: Apply damage to any Damageable (including player), no visibility check
                    ApplyFalloffDamage(hitCollider, explosionCenter);
                    continue;
                }

                // 3) Проверяем PlayerHealth (если не через Damageable)
                PlayerHealth ph = hitCollider.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    //Footnote: Player does not need IsEnemyVisible check
                    ApplyFalloffDamage(hitCollider, explosionCenter);
                    continue;
                }

                // 4) Если нужно, можно добавить другие типы целей
            }
        }
        StartCoroutine(SpawnSurfacePrefabs(explosionCenter));

        // 4) Удаляем сам снаряд через destroyDelay
        
    }

    /// <summary>
    /// Spawns random prefabs on actual surfaces (from surfaceSpawnLayers)
    /// within explosion radius immediately (no spawnDelay). Затем ждёт destroyDelay и удаляет сам BlastProjectile.
    /// </summary>
    private IEnumerator SpawnSurfacePrefabs(Vector3 center)
    {
        // 0) Если нет запланированных префабов — сразу выходим
        if (surfacePrefabs == null || surfacePrefabs.Length == 0)
            yield break;

        // 1) Собираем все коллайдеры нужных слоёв в радиусе взрыва
        Collider[] surfaceColliders = Physics.OverlapSphere(center, maxExpandScale, surfaceSpawnLayers);
        if (surfaceColliders == null || surfaceColliders.Length == 0)
        {
            // If no valid surface colliders, wait destroyDelay and destroy this object
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
            yield break;
        }

        // 2) Determine how many objects to spawn (inclusive)
        int totalToSpawn = Random.Range(minSpawnCount, maxSpawnCount + 1);
        int spawnedCount = 0;

        // 3) Limit attempts to avoid infinite loop
        int maxAttempts = totalToSpawn * 5;
        int attempts = 0;

        // 4) Main spawn loop
        while (spawnedCount < totalToSpawn && attempts < maxAttempts)
        {
            attempts++;

            // 4.1) Pick a random collider from those in the explosion radius
            Collider chosenCollider = surfaceColliders[Random.Range(0, surfaceColliders.Length)];

            // 4.2) Choose a random point inside this collider's bounds
            Bounds b = chosenCollider.bounds;
            Vector3 randomPointInBounds = new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                Random.Range(b.min.z, b.max.z)
            );

            // 4.3) Find the closest point on the collider's surface to that random point
            //Footnote: Collider.ClosestPoint returns the closest point on the collider's surface or interior
            Vector3 surfacePoint = chosenCollider.ClosestPoint(randomPointInBounds);

            // 4.4) Ensure that the surface point is still within the explosion radius
            if (Vector3.Distance(center, surfacePoint) > maxExpandScale)
            {
                yield return null;
                continue;
            }

            // 4.5) Compute direction from center to the surface point and distance
            Vector3 dir = (surfacePoint - center).normalized;
            float dist = Vector3.Distance(center, surfacePoint);

            // 4.6) Perform a thin Raycast to get the exact normal and confirm we're hitting the same collider
            if (Physics.Raycast(center, dir, out RaycastHit hitInfo, dist + 0.01f, surfaceSpawnLayers))
            {
                if (hitInfo.collider == chosenCollider)
                {
                    Vector3 spawnPos = hitInfo.point;
                    Vector3 normal = hitInfo.normal;

                    // 4.7) Choose a random prefab and set its rotation according to the surface normal
                    GameObject prefabToSpawn = surfacePrefabs[Random.Range(0, surfacePrefabs.Length)];
                    Quaternion spawnRot = Quaternion.FromToRotation(Vector3.up, normal);

                    // 4.8) Instantiate and immediately set its parent to the collider's transform
                    GameObject inst = Instantiate(prefabToSpawn, spawnPos, spawnRot);
                    inst.transform.SetParent(hitInfo.collider.transform); // make spawned prefab a child of the hit object

                    // 4.9) Schedule self-destruction for the spawned prefab
                    float lifeTime = Random.Range(minPrefabLifeTime, maxPrefabLifeTime);
                    Destroy(inst, lifeTime);

                    spawnedCount++;
                }
            }

            // 4.10) Wait one frame before next attempt
            yield return null;
        }

        // 5) After finishing spawning (or running out of attempts), wait destroyDelay and destroy this object
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }








    /// <summary>
    /// Checks if an enemy (by its root transform containing EnemyHealth) is visible from explosion center,
    /// taking into account the 'enemiesBlockEachOther' flag.
    /// Returns true if the first thing hit by a ray from 'center' towards 'enemyPivot' is exactly this enemy.
    /// </summary>
    private bool IsEnemyVisible(Transform enemyTransform, Vector3 center)
    {
        // 1) Находим компонент EnemyHealth у этого трансформа (может быть вложен)
        EnemyHealth enemyHealthComponent = enemyTransform.GetComponentInParent<EnemyHealth>();
        if (enemyHealthComponent == null)
        {
            // Если у этой трансформации нет EnemyHealth сверху — это не наш враг
            return false;
        }
        Transform enemyRoot = enemyHealthComponent.transform;

        // 2) Вычисляем направление от центра взрыва к пивоту (root-позиции) этого врага
        Vector3 direction = (enemyRoot.position - center).normalized;
        float distanceToEnemy = Vector3.Distance(center, enemyRoot.position);

        // 3) Составляем LayerMask для Raycast с учётом флага enemiesBlockEachOther.
        // Если враги НЕ должны блокировать друг друга, — мы исключаем слой "Enemy" из проверки.
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int raycastMask;
        if (enemiesBlockEachOther)
        {
            // Raycast будет учитывать ВСЕ слои (включая Enemy)
            raycastMask = ~0; // все биты включены
        }
        else
        {
            // Исключаем слой Enemy: все биты, кроме того, что соответствует EnemyLayer
            raycastMask = ~(1 << enemyLayer);
        }

        // 4) Выполняем Raycast с этим маской
        if (Physics.Raycast(center, direction, out RaycastHit hitInfo, distanceToEnemy, raycastMask))
        {
            // Если первым попался объект с EnemyHealth и это тот же корень — враг не скрыт
            EnemyHealth hitEnemy = hitInfo.collider.GetComponentInParent<EnemyHealth>();
            if (hitEnemy != null && hitEnemy.transform == enemyRoot)
            {
                return true;
            }
            // Иначе: либо попали в коллайдер другого врага (если враги блокируют друг друга),
            // либо в любую другую преграду (стена, объект уровня и т. д.) — значит, враг скрыт
            return false;
        }

        // 5) Если raycast ничего не встретил до позиции врага (напр. странные случаи), 
        //    считаем, что враг видим и даём ему урон.
        return true;
    }



    /// <summary>
    /// Coroutine to show expanding sphere visually and apply damage over time
    /// according to exponential falloff from radius to maxExpandScale.
    /// </summary>
    private IEnumerator ShowExpandingSphereAndDamage(Vector3 center, Collider[] colliders)
    {
        // Создаём объект-«кольцо»
        GameObject ring = new GameObject("ExplosionRing");
        ring.transform.position = center;
        ring.transform.rotation = Quaternion.identity;

        // Применяем MeshFilter и материал
        MeshFilter mf = ring.AddComponent<MeshFilter>();
        mf.mesh = sphereMeshFilter.sharedMesh;

        MeshRenderer mr = ring.AddComponent<MeshRenderer>();
        Material matInstance = new Material(explosionRingMaterial);
        mr.material = matInstance;

        // Устанавливаем стартовый масштаб равным radius
        float startScale = radius;
        ring.transform.localScale = Vector3.one * startScale;

        // Запомним исходную прозрачность материала
        float initialAlpha = matInstance.color.a;

        // Массив, чтобы помечать, кто уже получил урон или проверен
        bool[] hasTakenDamage = new bool[colliders.Length];

        float elapsed = 0f;
        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / expandDuration);

            // 1) Визуальное расширение: от radius до maxExpandScale
            float currentScale = Mathf.Lerp(startScale, maxExpandScale, t);
            ring.transform.localScale = Vector3.one * currentScale;

            // 2) Плавное уменьшение прозрачности кольца
            Color c = matInstance.color;
            c.a = Mathf.Lerp(initialAlpha, 0f, t);
            matInstance.color = c;

            // 3) Проходим по каждому коллайдеру, который ещё не получил урон
            for (int i = 0; i < colliders.Length; i++)
            {
                if (hasTakenDamage[i])
                    continue;

                Collider hit = colliders[i];
                if (hit == null)
                    continue;

                // Рассчитываем ближайшую точку к центру
                Vector3 hitPoint = hit.ClosestPoint(center);
                float dist = Vector3.Distance(hitPoint, center);

                // Если попадает в текущий радиус расширяющейся сферы — пора нанести урон
                if (dist <= currentScale)
                {
                    // Проверяем, является ли это врагом
                    Transform hitTransform = hit.transform;
                    EnemyHealth eh = hitTransform.GetComponentInParent<EnemyHealth>();
                    if (eh != null)
                    {
                        // Если лучом видно (прямая не перекрыта) — наносим урон
                        if (IsEnemyVisible(hitTransform, center))
                        {
                            ApplyFalloffDamage(hit, center);
                        }
                        // Если не видно — просто не наносим урон (и больше не проверяем этот коллайдер)
                        hasTakenDamage[i] = true;
                    }
                    else
                    {
                        // Если попался не-враг (Игрок, объект уровня и т.д.), 
                        // оставляем старую логику, если нужно.
                        // Например, можно сразу ApplyFalloffDamage(hit, center) без проверки или вовсе пропустить.
                        hasTakenDamage[i] = true;
                    }
                }
            }

            yield return null;
        }

        // После завершения расширения удаляем ring
        Destroy(ring);
    }


    // BlastProjectile.cs
    private void ApplyFalloffDamage(Collider hit, Vector3 center)
    {
        // 1) Находим точку попадания и дистанцию
        Vector3 hitPoint = hit.ClosestPoint(center);
        float dist = Vector3.Distance(hitPoint, center);

        // 2) Вычисляем финальный урон с экспоненциальным падением
        float finalDamage = 0f;
        if (dist <= radius)
        {
            finalDamage = damage;
        }
        else if (dist >= maxExpandScale)
        {
            finalDamage = 0f;
        }
        else
        {
            float normalized = (dist - radius) / (maxExpandScale - radius);
            normalized = Mathf.Clamp01(normalized);
            float falloff = Mathf.Pow(normalized, falloffExponent);
            finalDamage = Mathf.Lerp(damage, 0f, falloff);
        }

        // 3) Если урон нулевой — выходим сразу и не дергаем TakeDamage
        if (finalDamage <= 0f)
            return;

        // 4) Пробуем нанести Blast-урон через Damageable
        Damageable dmg = hit.GetComponent<Damageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(finalDamage,
                hitPoint,
                (hit.transform.position - center).normalized,
                DamageType.Blast);
            return;
        }

        // 5) Или через EnemyHealth
        EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(finalDamage,
                hitPoint,
                (hit.transform.position - center).normalized,
                DamageType.Blast);
            return;
        }

        // 6) Или через PlayerHealth (целочисленный зачёт)
        PlayerHealth player = hit.GetComponent<PlayerHealth>();
        if (player != null)
        {
            int intDmg = Mathf.RoundToInt(finalDamage);
            player.TakeDamage(intDmg, DamageType.Blast);
        }
    }


    private void OnDrawGizmosSelected()
    {
        // Показываем область «минимального» радиуса красным полупрозрачным шаром
        Gizmos.color = Color.red * new Color(1f, 1f, 1f, 0.2f);
        Gizmos.DrawSphere(transform.position, radius);

        // Показываем максимальный радиус (maxExpandScale) другим цветом
        Gizmos.color = Color.yellow * new Color(1f, 1f, 1f, 0.2f);
        Gizmos.DrawSphere(transform.position, maxExpandScale);
    }
}
