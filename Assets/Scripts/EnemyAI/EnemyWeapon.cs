using System.IO;
using UnityEngine;
using System.Collections.Generic;

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

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            Debug.LogError("EnemyHealth component not found on " + gameObject.name);
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
        else
        {
            EnemyChecker enemyChecker = GameObject.FindObjectOfType<EnemyChecker>();
            if (enemyChecker != null)
            {
                attachedWeaponDisplay = enemyChecker.GetEnemyWeaponName(gameObject);
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
    }

    void Update()
    {
        if (enemyHealth != null)
        {
            isDead = enemyHealth.isDead;
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
        }
        else
        {
            Debug.LogError("Cannot find file " + path);
        }
    }

    void Shoot()
    {
        if (isDead || attachedWeapon == null)
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
}
