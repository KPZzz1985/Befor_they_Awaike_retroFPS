using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
using Random = UnityEngine.Random;

public class EnemyWeapon : MonoBehaviour
{
    public int ammoCount;
    public float reloadTime;
    public float range; // Радиус поражения
    public int damagePerBullet;
    public float accuracy;
    public bool isSingleShot;
    public bool isBurstFire;
    public bool isRapidFire;
    public int bulletsPerShot;
    public int shotsPerBurst;
    public bool hasMelee;
    public float meleeRange;
    public int meleeDamage;

    public string resourceTag;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public string currentWeaponPrefabName;
    public bool isDead;

    private bool isWeaponAttached = false;
    private GameObject attachedWeapon;
    private EnemyHealth enemyHealth;
    public string attachedWeaponDisplay;

    public GameObject bulletPrefab;
    public float bulletSpeed = 10f;
    public float bulletDestroyDistance = 100f;
    public GameObject shellPrefab; // Prefab for shell casing ejection
    public bool isReloading;       // flag for reload in progress
    private int maxAmmoCount;
    private Animator animator;

    // Таймер между выстрелами
    public float shotCooldown = 1f; // интервал между выстрелами (можно настроить в инспекторе)
    private float lastShotTime = 0f;

    public bool isShooting = false; // управляется из StrategicSystem

    private EnemyMovement2 enemyMovement;
    private StrategicSystem strategicSystem;
    public GameObject playerObject;

    public GameObject muzzleFlashPrefab;
    public float muzzleFlashDuration = 0.75f;

    public float maxLookAtSpeed = 10f;
    [Header("Audio Settings")]
    [Tooltip("Clip to play when shooting.")]
    public AudioClip shootClip;
    [Tooltip("Volume of the shoot sound (0-1).")]
    [Range(0f,1f)] public float shootVolume = 1f;
    [Tooltip("Minimum distance for 3D sound attenuation.")]
    public float shootMinDistance = 1f;
    [Tooltip("Maximum distance for 3D sound attenuation.")]
    public float shootMaxDistance = 15f;
    private AudioSource shootAudioSource;

    /// <summary>
    /// Resets reload state, allowing shooting/reload after switching weapons.
    /// </summary>
    public void ResetReloadState()
    {
        isReloading = false;
    }

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            Debug.LogError("EnemyHealth component not found on " + gameObject.name);
        }
        else
        {
            enemyHealth.OnDeath += OnEnemyDeath;
        }

        enemyMovement = GetComponent<EnemyMovement2>();
        if (enemyMovement == null)
        {
            Debug.LogError("EnemyMovement component not found on " + gameObject.name);
        }

        strategicSystem = FindObjectOfType<StrategicSystem>();
        if (strategicSystem == null)
        {
            Debug.LogError("StrategicSystem not found in scene!");
        }

        if (!isWeaponAttached)
        {
            AttachWeaponToHolder();
        }

        if (attachedWeapon == null)
        {
            Debug.LogError("Failed to attach weapon during Start. Check if any weapons are available and if the resourceTag is correct.");
        }
        // Load weapon data directly from currentWeaponPrefabName
        animator = GetComponent<Animator>();
        maxAmmoCount = ammoCount;
        // Initialize 3D audio source for shooting
        shootAudioSource = GetComponent<AudioSource>();
        if (shootAudioSource == null) shootAudioSource = gameObject.AddComponent<AudioSource>();
        shootAudioSource.spatialBlend = 1f;
        shootAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        shootAudioSource.minDistance = shootMinDistance;
        shootAudioSource.maxDistance = shootMaxDistance;
        shootAudioSource.playOnAwake = false;
        attachedWeaponDisplay = currentWeaponPrefabName;
        if (!string.IsNullOrEmpty(attachedWeaponDisplay))
            LoadWeaponData(attachedWeaponDisplay);
        else
            Debug.LogWarning($"currentWeaponPrefabName is null or empty on {gameObject.name}, cannot load JSON.");
    }

    void Update()
    {
        if (enemyHealth != null)
        {
            // Враг считается мертвым если isDead=true ИЛИ isHandlingDeath=true
            isDead = enemyHealth.isDead || enemyHealth.isHandlingDeath;
            
            // Prevent any actions during critical damage OR special death
            if (enemyHealth.isCriticalDamage || enemyHealth.isHandlingDeath)
            {
                isShooting = false;
                return;
            }
        }

        if (isDead && attachedWeapon != null)
        {
            attachedWeapon.transform.parent = null;
            attachedWeapon.AddComponent<Rigidbody>();
            attachedWeapon = null;
        }
        else if (isDead)
        {
            return;
        }

        if (!isWeaponAttached)
        {
            return;
        }

        // Получаем разрешение на стрельбу из StrategicSystem
        if (strategicSystem != null)
        {
            var enemyStatus = strategicSystem.enemyStatuses.Find(e => e.enemyObject == gameObject);
            if (enemyStatus != null)
            {
                isShooting = enemyStatus.isShooting;
            }
        }

        // Если isShooting == true и прошло достаточно времени с прошлого выстрела, производим выстрел
        if (isShooting && isWeaponAttached && (Time.time - lastShotTime >= shotCooldown))
        {
            Shoot();
            lastShotTime = Time.time;
        }

        EnemyChecker enemyChecker = GameObject.FindObjectOfType<EnemyChecker>();
        if (enemyChecker != null)
        {
            string newWeaponDisplay = enemyChecker.GetEnemyWeaponName(gameObject);
            if (newWeaponDisplay != attachedWeaponDisplay)
            {
                attachedWeaponDisplay = newWeaponDisplay;
                if (!string.IsNullOrEmpty(attachedWeaponDisplay))
                {
                    LoadWeaponData(attachedWeaponDisplay);
                }
                else
                {
                    Debug.LogWarning("attachedWeaponDisplay is null or empty.");
                }
            }
        }

        if (attachedWeapon != null)
        {
            GameObject muzzlePoint = FindDeepChildByTag(attachedWeapon, "enemyMuzzlePoint");
            if (muzzlePoint != null && playerObject != null)
            {
                RotateMuzzleTowardsPlayer(muzzlePoint.transform, playerObject.transform);
            }
        }
        // Debug ammo and reload
        Debug.Log($"[Debug] {gameObject.name} Update: ammoCount={ammoCount}, isReloading={isReloading}");
        // trigger reload if needed and alive
        if (!isReloading && ammoCount <= 0 && !isDead)
        {
            Debug.Log($"[Debug] {gameObject.name} starting reload");
            ReloadAsync().Forget();
        }
        if (isReloading)
            return;
    }

    void AttachWeaponToHolder()
    {
        Transform weaponHolder = FindDeepChild(this.transform, "weaponHolderEnemy");

        if (weaponHolder == null)
        {
            Debug.LogError("No weaponHolderEnemy found!");
            return;
        }

        int bracketIndex;

        if (weaponHolder.childCount > 0)
        {
            foreach (Transform child in weaponHolder)
            {
                if (child.gameObject.CompareTag(resourceTag))
                {
                    bracketIndex = child.gameObject.name.IndexOf('(');
                    if (bracketIndex != -1)
                    {
                        currentWeaponPrefabName = child.gameObject.name.Substring(0, bracketIndex);
                    }
                    else
                    {
                        currentWeaponPrefabName = child.gameObject.name;
                    }
                    // Assign attachedWeapon for scene-placed weapons
                    attachedWeapon = child.gameObject;
                    isWeaponAttached = true;
                    return;
                }
            }
        }

        GameObject[] weapons = Resources.FindObjectsOfTypeAll<GameObject>();
        var taggedWeapons = new List<GameObject>();

        foreach (var weapon in weapons)
        {
            if (weapon.CompareTag(resourceTag))
                taggedWeapons.Add(weapon);
        }

        if (taggedWeapons.Count == 0)
        {
            Debug.LogError("No weapons found with tag " + resourceTag);
            return;
        }

        GameObject selectedWeapon = taggedWeapons[Random.Range(0, taggedWeapons.Count)];
        GameObject weaponInstance = Instantiate(selectedWeapon, weaponHolder);

        weaponInstance.SetActive(true);
        weaponInstance.transform.localPosition = positionOffset;
        weaponInstance.transform.localEulerAngles = rotationOffset;

        bracketIndex = selectedWeapon.name.IndexOf('(');
        if (bracketIndex != -1)
        {
            currentWeaponPrefabName = selectedWeapon.name.Substring(0, bracketIndex);
        }
        else
        {
            currentWeaponPrefabName = selectedWeapon.name;
        }

        isWeaponAttached = true;
        attachedWeapon = weaponInstance;
    }

    void LoadWeaponData(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            Debug.LogError("Filename is empty or null");
            return;
        }

        string path = Path.Combine(Application.streamingAssetsPath, filename + ".json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            WeaponData weaponData = JsonUtility.FromJson<WeaponData>(json);

            ammoCount = weaponData.ammoCount;
            reloadTime = weaponData.reloadTime;
            range = weaponData.range;
            damagePerBullet = weaponData.damagePerBullet;
            accuracy = weaponData.accuracy;
            isSingleShot = weaponData.isSingleShot;
            isBurstFire = weaponData.isBurstFire;
            isRapidFire = weaponData.isRapidFire;
            bulletsPerShot = weaponData.bulletsPerShot;
            shotsPerBurst = weaponData.shotsPerBurst;
            hasMelee = weaponData.hasMelee;
            meleeRange = weaponData.meleeRange;
            meleeDamage = weaponData.meleeDamage;
            // update max ammo based on loaded data
            maxAmmoCount = ammoCount;
        }
        else
        {
            Debug.LogError("Cannot find file " + path);
        }
    }

    public void Shoot()
    {
        if (isDead || attachedWeapon == null)
            return;
        if (ammoCount <= 0)
            return;

        if (Vector3.Distance(transform.position, playerObject.transform.position) > range)
            return;

        GameObject muzzlePoint = FindDeepChildByTag(attachedWeapon, "enemyMuzzlePoint");
        if (muzzlePoint == null)
        {
            Debug.LogError("muzzlePoint not found");
            return;
        }

        if (muzzleFlashPrefab)
        {
            GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, muzzlePoint.transform.position, muzzlePoint.transform.rotation, muzzlePoint.transform);
            Destroy(muzzleFlash, muzzleFlashDuration);
        }
        // Play shooting sound
        if (shootClip != null && shootAudioSource != null)
            shootAudioSource.PlayOneShot(shootClip, shootVolume);
        GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.transform.position, muzzlePoint.transform.rotation, null);
        if (!bullet)
        {
            Debug.LogError("Failed to instantiate bullet");
            return;
        }

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.SetBulletParameters(bulletSpeed, bulletDestroyDistance, "Player", "LevelObjects");
        bulletScript.damage = damagePerBullet;

        Collider bulletCollider = bullet.GetComponent<Collider>();
        Collider enemyCollider = GetComponent<Collider>();
        if (bulletCollider && enemyCollider)
            Physics.IgnoreCollision(bulletCollider, enemyCollider);

        if (playerObject == null)
        {
            Debug.LogError("Player object is not assigned in EnemyWeapon.");
            return;
        }

        Vector3 direction = (playerObject.transform.position - muzzlePoint.transform.position).normalized;
        direction = ApplyAccuracy(direction);

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb)
            bulletRb.AddForce(direction * bulletSpeed, ForceMode.Impulse);
        else
            Debug.LogError("No Rigidbody found on the bullet prefab.");
        // Decrement ammo and trigger reload when empty
        ammoCount--;
    }

    // Reload logic using UniTask
    private async UniTaskVoid ReloadAsync()
    {
        Debug.Log($"[Debug] {gameObject.name} ReloadAsync started");
        isReloading = true;
        if (animator != null)
            animator.SetBool("isReloading", true);
        // first half delay
        await UniTask.Delay(TimeSpan.FromSeconds(reloadTime * 0.5f));
        // abort on critical or death
        if (enemyHealth != null && (enemyHealth.isCriticalDamage || enemyHealth.isDead))
        {
            if (animator != null) animator.SetBool("isReloading", false);
            isReloading = false;
            return;
        }
        // shell ejection
        if (shellPrefab != null && attachedWeapon != null)
            Instantiate(shellPrefab, attachedWeapon.transform.position, attachedWeapon.transform.rotation);
        // second half delay
        await UniTask.Delay(TimeSpan.FromSeconds(reloadTime * 0.5f));
        // abort on critical or death
        if (enemyHealth != null && (enemyHealth.isCriticalDamage || enemyHealth.isDead))
        {
            if (animator != null) animator.SetBool("isReloading", false);
            isReloading = false;
            return;
        }
        // complete reload
        ammoCount = maxAmmoCount;
        if (animator != null) animator.SetBool("isReloading", false);
        isReloading = false;
    }

    private Vector3 ApplyAccuracy(Vector3 direction)
    {
        float inaccuracyAmount = 1f - accuracy;
        Vector3 inaccuracy = new Vector3(
            Random.Range(-inaccuracyAmount, inaccuracyAmount),
            Random.Range(-inaccuracyAmount, inaccuracyAmount),
            Random.Range(-inaccuracyAmount, inaccuracyAmount)
        );
        return (direction + inaccuracy).normalized;
    }

    private Transform FindDeepChild(Transform parent, string childTag)
    {
        foreach (Transform child in parent)
        {
            if (child.tag == childTag)
                return child;
            Transform result = FindDeepChild(child, childTag);
            if (result != null)
                return result;
        }
        return null;
    }

    private GameObject FindDeepChildByTag(GameObject parent, string childTag)
    {
        foreach (Transform child in parent.transform)
        {
            if (child.CompareTag(childTag))
                return child.gameObject;
            GameObject result = FindDeepChildByTag(child.gameObject, childTag);
            if (result != null)
                return result;
        }
        return null;
    }

    private void RotateMuzzleTowardsPlayer(Transform muzzle, Transform target)
    {
        Vector3 targetPositionWithOffset = target.position + new Vector3(0.25f, -0.25f, 0);
        Vector3 directionToTarget = (targetPositionWithOffset - muzzle.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        float rotationSpeed = Mathf.Lerp(1f, maxLookAtSpeed, accuracy);
        muzzle.rotation = Quaternion.RotateTowards(muzzle.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        Debug.DrawLine(muzzle.position, targetPositionWithOffset, Color.magenta);
    }

    /// <summary>
    /// Detaches the weapon when the enemy dies.
    /// </summary>
    private void OnEnemyDeath(EnemyHealth deadEnemy)
    {
        if (attachedWeapon != null)
        {
            attachedWeapon.transform.parent = null;
            if (attachedWeapon.GetComponent<Rigidbody>() == null)
                attachedWeapon.AddComponent<Rigidbody>();
            attachedWeapon = null;
            isWeaponAttached = false;
        }
    }
}
