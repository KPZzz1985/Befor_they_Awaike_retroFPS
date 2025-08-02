using UnityEngine;
using System.Collections; // ��� IEnumerator
using System.Collections.Generic; // ��� List

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

    [Tooltip("���� ��������, ����� ����� ����������� ���� ������ ������ ������")]
    public bool enemiesBlockEachOther = false;

    // List of prefabs to spawn on surfaces after explosion.
    [Tooltip("������ ��������, ������� ����� ���������� �� ������������ ����� ������")]
    public GameObject[] surfacePrefabs;
    // Optional prefabs to spawn upon explosion, destroyed after destroyDelay seconds
    [Header("Post-Explosion Prefabs")]
    [Tooltip("Optional prefabs to spawn at explosion, will be destroyed after destroyDelay seconds.")]
    public GameObject[] postExplosionPrefabs;
    [Header("Destroy On Explosion")]
    [Tooltip("Child objects or components of the projectile to destroy immediately upon explosion.")]
    public GameObject[] objectsToDestroyOnExplode;

    // Minimum and maximum count of prefabs to spawn (inclusive).
    [Tooltip("    ")]
    public int minSpawnCount = 1;
    [Tooltip("    ")]
    public int maxSpawnCount = 3;

    // Minimum and maximum lifetime (in seconds) for each spawned prefab.
    [Tooltip("    ")]
    public float minPrefabLifeTime = 1f;
    [Tooltip("    ")]
    public float maxPrefabLifeTime = 3f;

    

    // LayerMask  ,      (  ,  Enemy).
    [Tooltip(",       ( Enemy)")]
    public LayerMask surfaceSpawnLayers;

    // Audio settings for explosion sound
    [Header("Audio Settings")]
    [Tooltip("Audio clip to play when explosion occurs.")]
    public AudioClip explosionClip;
    [Tooltip("Volume for the explosion sound (0-1).")]
    [Range(0f,1f)] public float explosionVolume = 1f;
    [Tooltip("Minimum distance for 3D sound attenuation.")]
    public float explosionMinDistance = 1f;
    [Tooltip("Maximum distance for 3D sound attenuation.")]
    public float explosionMaxDistance = 15f;
    [Header("Flight Audio Settings")]
    [Tooltip("Audio clip to play while projectile is flying.")]
    public AudioClip flightClip;
    [Tooltip("Volume for the flight sound (0-1).")]
    [Range(0f,1f)] public float flightVolume = 1f;
    private AudioSource explosionAudioSource;
    private AudioSource flightAudioSource;
    private bool isArmed = false;   //   (    )
    private bool hasExploded = false;

    private void Start()
    {
        //   FixedUpdate (~0.02),        
        StartCoroutine(ArmAfterDelay());
        // Initialize explosion audio source
        explosionAudioSource = GetComponent<AudioSource>();
        if (explosionAudioSource == null) explosionAudioSource = gameObject.AddComponent<AudioSource>();
        explosionAudioSource.spatialBlend = 1f;
        explosionAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        explosionAudioSource.minDistance = explosionMinDistance;
        explosionAudioSource.maxDistance = explosionMaxDistance;
        explosionAudioSource.playOnAwake = false;
        // Initialize flight audio source
        flightAudioSource = gameObject.AddComponent<AudioSource>();
        flightAudioSource.clip = flightClip;
        flightAudioSource.playOnAwake = false;
        flightAudioSource.loop = true;
        flightAudioSource.spatialBlend = 1f;
        flightAudioSource.minDistance = explosionMinDistance;
        flightAudioSource.maxDistance = explosionMaxDistance;
        flightAudioSource.volume = flightVolume;
        if (flightClip != null)
            flightAudioSource.Play();
    }

    private IEnumerator ArmAfterDelay()
    {
        yield return new WaitForSeconds(0.02f);
        isArmed = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //          
        if (!isArmed || hasExploded)
            return;

        // ,       affectedLayers
        if (((1 << collision.gameObject.layer) & affectedLayers) == 0)
            return;

        hasExploded = true;
        Explode();
    }

    private void Explode()
    {
        Vector3 explosionCenter = transform.position;
        // Destroy specified objects that are part of the projectile
        if (objectsToDestroyOnExplode != null)
        {
            foreach (var obj in objectsToDestroyOnExplode)
            {
                if (obj != null)
                    Destroy(obj);
            }
        }
        // 1) Spawn VFX, if any
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, explosionCenter, Quaternion.identity);
        }
        // Stop flight audio
        if (flightAudioSource != null)
            flightAudioSource.Stop();
        // Play explosion sound
        if (explosionClip != null && explosionAudioSource != null)
            explosionAudioSource.PlayOneShot(explosionClip, explosionVolume);

        // 2)     maxExpandScale (   , ..)
        Collider[] colliders = Physics.OverlapSphere(explosionCenter, maxExpandScale, affectedLayers);

        // 3) ���� ���� ���������� ������ ������������� ����� � ��������� ��������.
        //    ����� � ����� ������� ���� ����� �����, �� � ������ �������.
        if (sphereMeshFilter != null && explosionRingMaterial != null)
        {
            StartCoroutine(ShowExpandingSphereAndDamage(explosionCenter, colliders));
        }
        else
        {
            // ������� ���� �����: ������� �� ������ � ����������, ����� �� ������ (��� �������� IsEnemyVisible)
            foreach (Collider hitCollider in colliders)
            {
                Transform hitTransform = hitCollider.transform;

                // 1) ��������� EnemyHealth
                EnemyHealth eh = hitTransform.GetComponentInParent<EnemyHealth>();
                if (eh != null)
                {
                    // ���� ���� ������ � ����
                    if (IsEnemyVisible(hitTransform, explosionCenter))
                    {
                        ApplyFalloffDamage(hitCollider, explosionCenter);
                    }
                    continue;
                }

                // 2) ��������� Damageable (����� ��� ����� ������ ������������� ������, ������� Player)
                Damageable damageable = hitCollider.GetComponent<Damageable>();
                if (damageable != null)
                {
                    //Footnote: Apply damage to any Damageable (including player), no visibility check
                    ApplyFalloffDamage(hitCollider, explosionCenter);
                    continue;
                }

                // 3) ��������� PlayerHealth (���� �� ����� Damageable)
                PlayerHealth ph = hitCollider.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    //Footnote: Player does not need IsEnemyVisible check
                    ApplyFalloffDamage(hitCollider, explosionCenter);
                    continue;
                }

                // 4) ���� �����, ����� �������� ������ ���� �����
            }
        }
        StartCoroutine(SpawnSurfacePrefabs(explosionCenter));
        // spawn additional post-explosion prefabs
        if (postExplosionPrefabs != null)
        {
            foreach (var prefab in postExplosionPrefabs)
            {
                if (prefab != null)
                {
                    GameObject obj = Instantiate(prefab, explosionCenter, Quaternion.identity);
                    // Schedule destruction after delay
                    StartCoroutine(DestroyPostPrefab(obj));
                }
            }
        }
    }

    /// <summary>
    /// Spawns random prefabs on actual surfaces (from surfaceSpawnLayers)
    /// within explosion radius immediately (no spawnDelay). ����� ��� destroyDelay � ������� ��� BlastProjectile.
    /// </summary>
    private IEnumerator SpawnSurfacePrefabs(Vector3 center)
    {
        // 0) ���� ��� ��������������� �������� � ����� �������
        if (surfacePrefabs == null || surfacePrefabs.Length == 0)
            yield break;

        // 1) �������� ��� ���������� ������ ���� � ������� ������
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
        // 1) ������� ��������� EnemyHealth � ����� ���������� (����� ���� ������)
        EnemyHealth enemyHealthComponent = enemyTransform.GetComponentInParent<EnemyHealth>();
        if (enemyHealthComponent == null)
        {
            // ���� � ���� ������������� ��� EnemyHealth ������ � ��� �� ��� ����
            return false;
        }
        Transform enemyRoot = enemyHealthComponent.transform;

        // 2) ��������� ����������� �� ������ ������ � ������ (root-�������) ����� �����
        Vector3 direction = (enemyRoot.position - center).normalized;
        float distanceToEnemy = Vector3.Distance(center, enemyRoot.position);

        // 3) ���������� LayerMask ��� Raycast � ������ ����� enemiesBlockEachOther.
        // ���� ����� �� ������ ����������� ���� �����, � �� ��������� ���� "Enemy" �� ��������.
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int raycastMask;
        if (enemiesBlockEachOther)
        {
            // Raycast ����� ��������� ��� ���� (������� Enemy)
            raycastMask = ~0; // ��� ���� ��������
        }
        else
        {
            // ��������� ���� Enemy: ��� ����, ����� ����, ��� ������������� EnemyLayer
            raycastMask = ~(1 << enemyLayer);
        }

        // 4) ��������� Raycast � ���� ������
        if (Physics.Raycast(center, direction, out RaycastHit hitInfo, distanceToEnemy, raycastMask))
        {
            // ���� ������ ������� ������ � EnemyHealth � ��� ��� �� ������ � ���� �� �����
            EnemyHealth hitEnemy = hitInfo.collider.GetComponentInParent<EnemyHealth>();
            if (hitEnemy != null && hitEnemy.transform == enemyRoot)
            {
                return true;
            }
            // �����: ���� ������ � ��������� ������� ����� (���� ����� ��������� ���� �����),
            // ���� � ����� ������ �������� (�����, ������ ������ � �. �.) � ������, ���� �����
            return false;
        }

        // 5) ���� raycast ������ �� �������� �� ������� ����� (����. �������� ������), 
        //    �������, ��� ���� ����� � ��� ��� ����.
        return true;
    }



    /// <summary>
    /// Coroutine to show expanding sphere visually and apply damage over time
    /// according to exponential falloff from radius to maxExpandScale.
    /// </summary>
    private IEnumerator ShowExpandingSphereAndDamage(Vector3 center, Collider[] colliders)
    {
        // ������ ������-�������
        GameObject ring = new GameObject("ExplosionRing");
        ring.transform.position = center;
        ring.transform.rotation = Quaternion.identity;

        // ��������� MeshFilter � ��������
        MeshFilter mf = ring.AddComponent<MeshFilter>();
        mf.mesh = sphereMeshFilter.sharedMesh;

        MeshRenderer mr = ring.AddComponent<MeshRenderer>();
        Material matInstance = new Material(explosionRingMaterial);
        mr.material = matInstance;

        // ������������� ��������� ������� ������ radius
        float startScale = radius;
        ring.transform.localScale = Vector3.one * startScale;

        // �������� �������� ������������ ���������
        float initialAlpha = matInstance.color.a;

        // ������, ����� ��������, ��� ��� ������� ���� ��� ��������
        bool[] hasTakenDamage = new bool[colliders.Length];

        float elapsed = 0f;
        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / expandDuration);

            // 1) ���������� ����������: �� radius �� maxExpandScale
            float currentScale = Mathf.Lerp(startScale, maxExpandScale, t);
            ring.transform.localScale = Vector3.one * currentScale;

            // 2) ������� ���������� ������������ ������
            Color c = matInstance.color;
            c.a = Mathf.Lerp(initialAlpha, 0f, t);
            matInstance.color = c;

            // 3) �������� �� ������� ����������, ������� ��� �� ������� ����
            for (int i = 0; i < colliders.Length; i++)
            {
                if (hasTakenDamage[i])
                    continue;

                Collider hit = colliders[i];
                if (hit == null)
                    continue;

                // ������������ ��������� ����� � ������
                Vector3 hitPoint = hit.ClosestPoint(center);
                float dist = Vector3.Distance(hitPoint, center);

                // ���� �������� � ������� ������ ������������� ����� � ���� ������� ����
                if (dist <= currentScale)
                {
                    // ���������, �������� �� ��� ������
                    Transform hitTransform = hit.transform;
                    EnemyHealth eh = hitTransform.GetComponentInParent<EnemyHealth>();
                    if (eh != null)
                    {
                        // ���� ����� ����� (������ �� ���������) � ������� ����
                        if (IsEnemyVisible(hitTransform, center))
                        {
                            ApplyFalloffDamage(hit, center);
                        }
                        // ���� �� ����� � ������ �� ������� ���� (� ������ �� ��������� ���� ���������)
                        hasTakenDamage[i] = true;
                    }
                    else
                    {
                        // apply damage to any Damageable component
                        Damageable damageable = hit.GetComponent<Damageable>();
                        if (damageable != null)
                        {
                            ApplyFalloffDamage(hit, center);
                        }
                        else
                        {
                            // apply damage to player health if present
                            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                            if (ph != null)
                                ApplyFalloffDamage(hit, center);
                        }
                        hasTakenDamage[i] = true;
                    }
                }
            }

            yield return null;
        }

        // ����� ���������� ���������� ������� ring
        Destroy(ring);
    }


    // BlastProjectile.cs
    private void ApplyFalloffDamage(Collider hit, Vector3 center)
    {
        // 1) ������� ����� ��������� � ���������
        Vector3 hitPoint = hit.ClosestPoint(center);
        float dist = Vector3.Distance(hitPoint, center);

        // 2) ��������� ��������� ���� � ���������������� ��������
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

        // 3) ���� ���� ������� � ������� ����� � �� ������� TakeDamage
        if (finalDamage <= 0f)
            return;

        // 4) ������� ������� Blast-���� ����� Damageable
        Damageable dmg = hit.GetComponent<Damageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(finalDamage,
                hitPoint,
                (hit.transform.position - center).normalized,
                DamageType.Blast);
            return;
        }

        // 5) ��� ����� EnemyHealth
        EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            enemy.TakeDamage(finalDamage,
                hitPoint,
                (hit.transform.position - center).normalized,
                DamageType.Blast);
            return;
        }

        // 6) ��� ����� PlayerHealth (������������� �����)
        PlayerHealth player = hit.GetComponent<PlayerHealth>();
        if (player != null)
        {
            int intDmg = Mathf.RoundToInt(finalDamage);
            player.TakeDamage(intDmg, DamageType.Blast);
        }
    }


    private void OnDrawGizmosSelected()
    {
        // ���������� ������� ������������� ������� ������� �������������� �����
        Gizmos.color = Color.red * new Color(1f, 1f, 1f, 0.2f);
        Gizmos.DrawSphere(transform.position, radius);

        // ���������� ������������ ������ (maxExpandScale) ������ ������
        Gizmos.color = Color.yellow * new Color(1f, 1f, 1f, 0.2f);
        Gizmos.DrawSphere(transform.position, maxExpandScale);
    }

    // Coroutine to destroy a spawned post-explosion prefab after destroyDelay seconds
    private IEnumerator DestroyPostPrefab(GameObject obj)
    {
        yield return new WaitForSeconds(destroyDelay);
        if (obj != null)
            Destroy(obj);
    }
}
