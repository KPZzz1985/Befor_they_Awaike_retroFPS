using System.Collections;
using UnityEngine;

public class PM_Shooting : MonoBehaviour
{
    public Transform muzzlePoint;
    public GameObject muzzleFlashPrefab;
    public PM_animations animatorScript;
    public LayerMask raycastLayerMask;
    public RecoilEffect recoilEffect;

    [Header("Weapon Settings")]
    public float damage = 20f;
    public float fireRate = 0.1f;
    public bool burstFire = false;
    public int burstCount = 3;
    public float spread = 1f;
    [Range(0, 1)] public float recoilForce = 0.5f;
    public int maxAmmo = 30;
    public float reloadTime = 2f;

    // --- Projectile Mode ---
    [Header("Projectile Mode Settings")]
    [Tooltip("Если true, оружие будет рождать физический объект (проектиль) вместо рейкаста.")]
    public bool isProjectileMode = false;
    [Tooltip("Префаб снаряда (например, граната или шрапнель).")]
    public GameObject projectilePrefab;
    [Tooltip("Начальная скорость снаряда.")]
    public float projectileSpeed = 20f;
    // --------------------------------------------

    // --- Shotgun Mode ---
    [Header("Shotgun Mode Settings")]
    [Tooltip("Если true, вместо одиночного рейкаста будет запускаться несколько дробин.")]
    public bool isShotgunMode = false;
    public int pelletCount = 5;
    [Tooltip("Если true, в Shotgun Mode перезарядка идёт по одной дробине (патрону) за раз.")]
    public bool isSingleCartridgeReloading = false;
    [Tooltip("Время (в секундах) на досылание одного патрона при попатронной перезарядке.")]
    public float singleReloadTime = 0.5f;
    [Tooltip("Время (в секундах) на анимацию взведения после окончания попатронной перезарядки.")]
    public float armedTime = 0.5f;
    // --------------------------------------------

    private int currentAmmo;
    private bool isReloading = false;
    private bool isReloadingSingle = false;
    private bool reloadInterrupted = false;
    private float nextFireTime = 0f;

    public GameObject shellPrefab;
    public Transform shellEjectionPoint;
    public float shellLifetime = 5f;

    [Tooltip("Сколько секунд ждать после выстрела перед созданием гильзы. 0 = без задержки.")]
    public float shellEjectionDelay = 0f;

    public bool isShooting = false;

    // Эти поля были в исходнике и остаются без изменений:
    [Header("Hit Effects Prefabs (unchanged)")]
    public GameObject levelObjectsPrefab;
    public float levelObjectsLifetime = 2f;
    public GameObject enemyPrefab;
    public float enemyLifetime = 2f;

    // --- Shotgun Damage Falloff ---
    [Header("Shotgun Damage Falloff Settings")]
    [Tooltip("Расстояние (в метрах), до которой дробовик наносит полный урон.")]
    public float falloffStartDistance = 10f;
    [Tooltip("Расстояние (в метрах), после которой урон падает до минимального значения.")]
    public float falloffEndDistance = 25f;
    [Tooltip("Минимальный урон одной дробины за пределами falloffEndDistance.")]
    public float minPelletDamage = 5f;
    [Tooltip("Степень (экспонента) кривой падения урона. 1 = линейно, >1 = более резкий спад.")]
    public float falloffExponent = 1f;
    // --------------------------------------------

    private void Start()
    {
        currentAmmo = maxAmmo;
    }

    private void Update()
    {
        // --- Если сейчас идёт pop-in-one (попатронная) перезарядка, отслеживаем Fire1 для прерывания ---
        if (isReloadingSingle)
        {
            if (Input.GetButtonDown("Fire1"))
                reloadInterrupted = true;
            return;
        }

        // --- Если идёт обычная полная перезарядка — блокируем стрельбу ---
        if (isReloading)
            return;

        // --- Ручная перезарядка клавишей R ---
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
        {
            if (isShotgunMode && isSingleCartridgeReloading)
                StartCoroutine(ReloadSingle());
            else
                StartCoroutine(Reload());
            return;
        }

        // --- Burst-режим ---
        if (burstFire)
        {
            if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
            {
                // Если магазин пуст, ждем нажатия, чтобы запустить перезарядку
                if (currentAmmo <= 0)
                {
                    if (isShotgunMode && isSingleCartridgeReloading)
                        StartCoroutine(ReloadSingle());
                    else
                        StartCoroutine(Reload());
                    return;
                }

                nextFireTime = Time.time + 1f / fireRate;
                StartCoroutine(ShootBurst());
            }
        }
        else
        {
            // --- Обычный одиночный выстрел ---
            if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
            {
                // Если магазин пуст, запускаем перезарядку при попытке выстрела
                if (currentAmmo <= 0)
                {
                    if (isShotgunMode && isSingleCartridgeReloading)
                        StartCoroutine(ReloadSingle());
                    else
                        StartCoroutine(Reload());
                    return;
                }

                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
            }
        }
    }

    private void OnEnable()
    {
        RecoilEffect recoil = FindObjectOfType<RecoilEffect>();
        if (recoil != null)
            recoil.SetPM_Shooting(this);
    }

    private IEnumerator ShootBurst()
    {
        for (int i = 0; i < burstCount; i++)
        {
            Shoot();
            yield return new WaitForSeconds(fireRate);
        }
    }

    private void Shoot()
    {
        isShooting = true;
        StartCoroutine(StopShooting());

        currentAmmo--;
        animatorScript.animator.SetTrigger("isShoot");

        // 1) Projectile Mode
        if (isProjectileMode && projectilePrefab != null)
        {
            if (isShotgunMode)
            {
                for (int i = 0; i < pelletCount; i++)
                    SpawnProjectileWithSpread();
            }
            else
            {
                SpawnProjectile();
            }
        }
        // 2) Raycast Mode (обычный или Shotgun)
        else
        {
            if (isShotgunMode)
            {
                for (int i = 0; i < pelletCount; i++)
                    ShootPellet();
            }
            else
            {
                PerformRaycast(muzzlePoint.forward);
            }
        }

        // Отдача камеры
        if (recoilEffect != null)
            recoilEffect.ApplyRecoil();
        else
            Debug.LogWarning("RecoilEffect не найден.");

        // Создаём muzzle flash как дочерний объект у muzzlePoint
        GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, muzzlePoint);
        muzzleFlash.transform.localPosition = Vector3.zero;
        muzzleFlash.transform.localRotation = Quaternion.identity;
        Destroy(muzzleFlash, 0.1f);

        // Отлёт гильзы
        StartCoroutine(EjectShellDelayed());
    }

    private void SpawnProjectile()
    {
        GameObject proj = Instantiate(projectilePrefab, muzzlePoint.position, muzzlePoint.rotation);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = muzzlePoint.forward * projectileSpeed;
        // Если нужен урон в скрипте префаба — передайте его здесь
    }

    private void SpawnProjectileWithSpread()
    {
        Vector3 spreadDir = muzzlePoint.forward + Random.insideUnitSphere * spread;
        Quaternion rot = Quaternion.LookRotation(spreadDir.normalized, muzzlePoint.up);
        GameObject proj = Instantiate(projectilePrefab, muzzlePoint.position, rot);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = spreadDir.normalized * projectileSpeed;
        // И здесь можно передавать damage в скрипт префаба
    }

    private void ShootPellet()
    {
        Vector3 pelletDirection = muzzlePoint.forward + Random.insideUnitSphere * spread;
        PerformRaycast(pelletDirection);
    }

    private void PerformRaycast(Vector3 direction)
    {
        if (Physics.Raycast(muzzlePoint.position, direction, out RaycastHit hit, Mathf.Infinity, raycastLayerMask))
        {
            Damageable damageable = hit.transform.GetComponent<Damageable>();
            if (damageable != null)
            {
                Vector3 hitDirection = hit.point - muzzlePoint.position;
                float pelletDamage = damage;
                if (isShotgunMode)
                {
                    float dist = hit.distance;
                    if (dist <= falloffStartDistance)
                        pelletDamage = damage;
                    else if (dist >= falloffEndDistance)
                        pelletDamage = minPelletDamage;
                    else
                    {
                        float t = (dist - falloffStartDistance) / (falloffEndDistance - falloffStartDistance);
                        t = Mathf.Clamp01(t);
                        float tExp = Mathf.Pow(t, falloffExponent);
                        pelletDamage = Mathf.Lerp(damage, minPelletDamage, tExp);
                    }
                }
                damageable.TakeDamage(pelletDamage, hit.point, hitDirection);
            }

            if (hit.transform.CompareTag("LevelObjects") ||
                hit.transform.CompareTag("Ground") ||
                hit.transform.CompareTag("Wall"))
            {
                GameObject tempPrefab = Instantiate(levelObjectsPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(tempPrefab, levelObjectsLifetime);
            }
            else if (hit.transform.CompareTag("EnemyColiderPart"))
            {
                GameObject tempPrefab = Instantiate(enemyPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(tempPrefab, enemyLifetime);
            }
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        animatorScript.animator.SetTrigger("isReloading");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
    }

    private IEnumerator ReloadSingle()
    {
        isReloadingSingle = true;
        reloadInterrupted = false;

        int shellsNeeded = maxAmmo - currentAmmo;
        for (int i = 0; i < shellsNeeded; i++)
        {
            // Если игрок прервал попатронную перезарядку — выходим
            if (reloadInterrupted)
                break;

            // Проигрываем триггер «зарядил один патрон»
            animatorScript.animator.SetTrigger("isReloadingSingle");

            // Ждем singleReloadTime без проверок Input.GetButtonDown
            float timer = 0f;
            while (timer < singleReloadTime)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // Вставляем один патрон
            currentAmmo++;
            if (currentAmmo >= maxAmmo)
                break;
        }

        // После последнего досылания патрона — анимация взведения
        animatorScript.animator.SetTrigger("isArmed");
        float armedTimer = 0f;
        while (armedTimer < armedTime)
        {
            armedTimer += Time.deltaTime;
            yield return null;
        }

        isReloadingSingle = false;
    }

    private IEnumerator EjectShellDelayed()
    {
        if (shellEjectionDelay > 0f)
            yield return new WaitForSeconds(shellEjectionDelay);

        GameObject shell = Instantiate(shellPrefab, shellEjectionPoint.position, shellEjectionPoint.rotation);
        Rigidbody rb = shell.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(
                shellEjectionPoint.forward * Random.Range(100f, 200f) +
                shellEjectionPoint.up * Random.Range(100f, 200f)
            );
        Destroy(shell, shellLifetime);
    }

    private IEnumerator StopShooting()
    {
        yield return new WaitForSeconds(fireRate);
        isShooting = false;
    }
}
