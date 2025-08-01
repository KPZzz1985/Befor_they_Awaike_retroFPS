using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemyMovement2 : MonoBehaviour
{
    public NavMeshAgent navMeshAgent;
    public Transform player;
    public EnemyWeapon enemyWeapon;
    public CoverFormation coverFormation;
    public EnemyHealth enemyHealth;
    public LookRegistrator lookRegistrator;
    public StrategicSystem strategicSystem;

    public bool PlayerSeen;
    private EnemyHealth _enemyHealth;
    public float HealthPercentage;

    private List<CoverFormation.CoverPoint> coverPoints;
    // Буфер для кандидатов, чтобы не создавать новый список каждый раз
    private List<CoverFormation.CoverPoint> candidatePointsBuffer = new List<CoverFormation.CoverPoint>();

    // Для кеширования текущей цели укрытия и запоминания последней
    private CoverFormation.CoverPoint currentCoverTarget;
    private CoverFormation.CoverPoint lastCoverPoint;

    [SerializeField] public bool isEngage;

    [Header("Covering Settings")]
    public float minSpeed;
    public float maxSpeed;
    public float minWaitTime;
    public float maxWaitTime;
    public float arrivalRadius = 0.5f;

    [Header("Covering Selection")]
    public float coverSelectionRangeMultiplier = 1.5f;

    [Header("Cover Evaluation Weights")]
    public float weightDistance = 1.0f;
    public float weightOccupancy = 10.0f;
    public float weightAngle = 0.5f;

    [Header("Cover Occupancy")]
    public float coverOccupancyRadius = 2.0f;

    [Header("Mid-Route Switching")]
    [Range(0, 100)]
    public int midRouteSwitchChance = 5; // шанс переключения в процентах
    public float midRouteSwitchCooldown = 3.0f; // минимум секунд между переключениями
    private float lastMidRouteSwitchTime = 0f;

    [Header("Ambush Settings")]
    [Range(0, 100)]
    public int ambushChance = 50; // шанс остаться в укрытии, если игрок не виден
    private bool isAmbush = false;

    [Header("Push Settings")]
    [Range(0, 100)]
    public int pushChance = 50; // шанс перейти в режим push из ambush
    private bool isPush = false;
    private bool pushDecisionRunning = false;

    [Header("Retreat Settings")]
    public float minPlayerDistance = 3.5f;  // минимальное расстояние до игрока
    public float retreatDistance = 5f;      // расстояние отступления
    private bool isRetreating = false;
    private Vector3 currentRetreatPoint;

    public bool isCover;
    public bool isSit;
    public bool isWaiting;

    public Animator animator;
    public bool IntruderAlert;

    [Header("Engage Settings")]
    public float minEngageTime;
    public float maxEngageTime;

    [Header("Engage Randomization")]
    [Range(0, 100)]
    public int engageChance = 5;

    public float smoothTime = 0.1f;

    [Header("Sitting Settings")]
    [Range(0, 100)]
    public int sitChance = 5;
    public float minSitTime = 3f;
    public float maxSitTime = 6f;

    private float smoothMoveX;
    private float smoothMoveY;

    private int ammoCount;
    private float reloadTime;
    private float range;
    private int damagePerBullet;
    private float accuracy;
    private bool isSingleShot;
    private bool isBurstFire;
    private bool isRapidFire;
    private int bulletsPerShot;
    private int shotsPerBurst;
    private bool hasMelee;
    private float meleeRange;
    private int meleeDamage;
    private float initialEnemyHealth;

    private void Start()
    {
        EnemyPatrol enemyPatrol = GetComponent<EnemyPatrol>();
        if (enemyPatrol != null)
            enemyPatrol.enabled = false;
        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();

        isCover = true;
        isWaiting = false;
        isSit = false;
        animator = GetComponent<Animator>();
        InitializeWeaponProperties();

        if (coverFormation != null)
            coverPoints = coverFormation.GetCoverPoints();

        IntruderAlert = true;
        animator.SetBool("IntruderAlert", true);

        _enemyHealth = GetComponent<EnemyHealth>();
        if (_enemyHealth != null)
            _enemyHealth.OnCriticalDamageChanged += HandleCriticalDamageChanged;

        initialEnemyHealth = enemyHealth.HealthPercentage;

        StartCoroutine(RandomSitRoutine());
        strategicSystem = FindObjectOfType<StrategicSystem>();
    }

    private void OnDestroy()
    {
        if (_enemyHealth != null)
            _enemyHealth.OnCriticalDamageChanged -= HandleCriticalDamageChanged;
    }

    private void HandleCriticalDamageChanged(bool isCritical)
    {
        isEngage = !isCritical;
    }

    private void Update()
    {
        PlayerSeen = lookRegistrator.PlayerSeen;
        isEngage = PlayerSeen;
    }

    private void LateUpdate()
    {
        // Если агент не на NavMesh – пытаемся вернуть его
        if (!navMeshAgent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                navMeshAgent.Warp(hit.position);
            if (!navMeshAgent.isOnNavMesh)
                return;
        }

        // Если в режиме ambush, запускаем переход к push (если не запущено)
        if (isAmbush && !PlayerSeen && !pushDecisionRunning)
        {
            StartCoroutine(AmbushPushTransition());
        }

        // Если в режиме ambush, обрабатываем его (остановка)
        if (isAmbush && !PlayerSeen)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            animator.SetFloat("Speed", 0f);
            return;
        }
        if (isAmbush && PlayerSeen)
        {
            isAmbush = false;
            navMeshAgent.isStopped = false;
            navMeshAgent.updateRotation = true;
        }

        // Если в режиме push, враг действует по обычной логике укрытия, но цель уже выбрана
        if (isPush)
        {
            // Если враг достиг точки push, сбрасываем isPush
            if (Vector3.Distance(transform.position, currentCoverTarget.position) < arrivalRadius)
            {
                isPush = false;
            }
            else
            {
                // Просто задаем цель и далее логика isCovering обработает поведение
                SetDestinationForCover(currentCoverTarget.position);
            }
        }

        // Режим отступления: если враг видит игрока и находится слишком близко
        if (PlayerSeen && Vector3.Distance(transform.position, player.position) < minPlayerDistance)
        {
            if (!isRetreating)
                BeginRetreat();
            else
            {
                // Отключаем автообновление вращения агента
                navMeshAgent.updateRotation = false;
                // При этом враг продолжает смотреть на игрока – обновляем поворот плавно
                Quaternion targetRotation = Quaternion.LookRotation(player.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

                if (Vector3.Distance(navMeshAgent.destination, currentRetreatPoint) > 0.5f)
                    SetDestinationForCover(currentRetreatPoint);

                if (Vector3.Distance(transform.position, currentRetreatPoint) < arrivalRadius + 0.2f)
                {
                    isRetreating = false;
                    isCover = true;
                    navMeshAgent.updateRotation = true;
                }
            }
            return;
        }
        else
        {
            isRetreating = false;
            navMeshAgent.updateRotation = true;
        }

        HealthPercentage = enemyHealth.HealthPercentage;
        animator.SetBool("isEngage", isEngage);
        animator.SetFloat("Speed", navMeshAgent.speed);
        animator.SetBool("isSit", isSit);

        if (enemyHealth.HealthPercentage <= 0)
        {
            DisableEnemy();
            return;
        }

        if (Vector3.Distance(transform.position, player.position) <= range * 1.5f)
            LookAtPlayer();

        if (isEngage && initialEnemyHealth != enemyHealth.HealthPercentage)
            StopEngage();

        if (isCover)
        {
            EnemyCovering();
        }
        else if (coverPoints == null || coverPoints.Count == 0)
        {
            AttackMode();
        }
        else if (!isEngage)
        {
            RandomEngage();
        }
        else if (isEngage && !isAmbush && !isPush && !isRetreating)
        {
            // Restart cover search instead of chasing player
            if (coverFormation != null)
                coverPoints = coverFormation.GetCoverPoints();
            currentCoverTarget = null;
            isCover = true;
            EnemyCovering();
        }

        UpdateAnimatorMovement();
    }

    // Coroutine, которая в режиме ambush ждет от 5 до 10 секунд и затем случайно решает перейти в push
    private IEnumerator AmbushPushTransition()
    {
        pushDecisionRunning = true;
        float waitTime = Random.Range(5f, 10f);
        yield return new WaitForSeconds(waitTime);
        // Если враг все еще в ambush и не видит игрока
        if (isAmbush && !PlayerSeen)
        {
            if (Random.Range(0, 100) < pushChance)
            {
                // Переходим в режим push
                isPush = true;
                isAmbush = false;
                // Выбираем точку укрытия, ближайшую к игроку
                CoverFormation.CoverPoint pushCover = ChoosePushCoverPoint();
                if (pushCover != null)
                {
                    currentCoverTarget = pushCover;
                    SetDestinationForCover(currentCoverTarget.position);
                }
            }
        }
        pushDecisionRunning = false;
    }

    // Выбирает из всех coverPoints ту, которая минимальна по расстоянию до игрока
    private CoverFormation.CoverPoint ChoosePushCoverPoint()
    {
        CoverFormation.CoverPoint best = null;
        float bestDistance = Mathf.Infinity;
        foreach (var cp in coverPoints)
        {
            float d = Vector3.Distance(cp.position, player.position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = cp;
            }
        }
        return best;
    }

    private void BeginRetreat()
    {
        isRetreating = true;
        // Рассчитываем точку отступления: от игрока в направлении от него
        currentRetreatPoint = transform.position + (transform.position - player.position).normalized * retreatDistance;
        navMeshAgent.updateRotation = false;
        SetDestinationForCover(currentRetreatPoint);
    }

    private void DisableEnemy()
    {
        navMeshAgent.enabled = false;
        lookRegistrator.enabled = false;
        StartCoroutine(WaitAndDisable());
    }

    private IEnumerator WaitAndDisable()
    {
        yield return new WaitForSeconds(2f);
        enemyWeapon.enabled = false;
        this.enabled = false;
    }

    private void InitializeWeaponProperties()
    {
        if (enemyWeapon != null)
        {
            ammoCount = enemyWeapon.ammoCount;
            reloadTime = enemyWeapon.reloadTime;
            range = enemyWeapon.range;
            damagePerBullet = enemyWeapon.damagePerBullet;
            accuracy = enemyWeapon.accuracy;
            isSingleShot = enemyWeapon.isSingleShot;
            isBurstFire = enemyWeapon.isBurstFire;
            isRapidFire = enemyWeapon.isRapidFire;
            bulletsPerShot = enemyWeapon.bulletsPerShot;
            shotsPerBurst = enemyWeapon.shotsPerBurst;
            hasMelee = enemyWeapon.hasMelee;
            meleeRange = enemyWeapon.meleeRange;
            meleeDamage = enemyWeapon.meleeDamage;
        }
    }

    private void LookAtPlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 50f);
    }

    /// <summary>
    /// Метод выбора точки укрытия с кешированием текущей цели.
    /// Если цель уже выбрана:
    /// - Если дистанция до цели >= arrivalRadius, применяется mid-route switching.
    /// - Если достигнута точка и игрок не виден, с шансом ambush враг остаётся на месте.
    /// </summary>
    private void EnemyCovering()
    {
        if (coverPoints == null || coverPoints.Count == 0)
            return;

        if (currentCoverTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, currentCoverTarget.position);

            if (distanceToTarget >= arrivalRadius)
            {
                if (Vector3.Distance(transform.position, player.position) >= 5f)
                {
                    if ((Time.time - lastMidRouteSwitchTime) >= midRouteSwitchCooldown && Random.Range(0, 100) < midRouteSwitchChance)
                    {
                        candidatePointsBuffer.Clear();
                        float nearestDistance = Mathf.Infinity;
                        for (int i = 0; i < coverPoints.Count; i++)
                        {
                            float d = Vector3.Distance(transform.position, coverPoints[i].position);
                            if (d < nearestDistance)
                                nearestDistance = d;
                        }
                        for (int i = 0; i < coverPoints.Count; i++)
                        {
                            float d = Vector3.Distance(transform.position, coverPoints[i].position);
                            if (d <= nearestDistance * coverSelectionRangeMultiplier && coverPoints[i] != currentCoverTarget)
                                candidatePointsBuffer.Add(coverPoints[i]);
                        }
                        if (candidatePointsBuffer.Count > 0)
                        {
                            CoverFormation.CoverPoint newTarget = ChooseBestCandidate(candidatePointsBuffer);
                            if (newTarget != null)
                            {
                                currentCoverTarget = newTarget;
                                SetDestinationForCover(newTarget.position);
                                lastMidRouteSwitchTime = Time.time;
                            }
                        }
                    }
                }

                if (Vector3.Distance(navMeshAgent.destination, currentCoverTarget.position) > 0.1f)
                    SetDestinationForCover(currentCoverTarget.position);
                return;
            }
            else // Агент достиг цели
            {
                int occupancy = GetOccupancyForCoverPoint(currentCoverTarget);
                if (occupancy > 0)
                {
                    CoverFormation.CoverPoint alternativeCover = GetAlternativeCover(currentCoverTarget);
                    if (alternativeCover != null)
                    {
                        currentCoverTarget = alternativeCover;
                        SetDestinationForCover(currentCoverTarget.position);
                        return;
                    }
                }

                lastCoverPoint = currentCoverTarget;
                if (!PlayerSeen)
                {
                    if (Random.Range(0, 100) < ambushChance)
                    {
                        isAmbush = true;
                        navMeshAgent.isStopped = true;
                        return;
                    }
                    else
                    {
                        isCover = false;
                        isWaiting = true;
                        if (Random.Range(0, 100) < sitChance)
                            StartCoroutine(SitRoutine());
                        StartCoroutine(WaitAndSit());
                        return;
                    }
                }
                else if (isEngage)
                {
                    CoverFormation.CoverPoint alternativeCover = GetAlternativeCover(lastCoverPoint);
                    if (alternativeCover != null)
                    {
                        currentCoverTarget = alternativeCover;
                        SetDestinationForCover(currentCoverTarget.position);
                    }
                }
                else
                {
                    StartCoroutine(WaitAndSit());
                    return;
                }
            }
        }

        candidatePointsBuffer.Clear();
        float minDist = Mathf.Infinity;
        for (int i = 0; i < coverPoints.Count; i++)
        {
            float d = Vector3.Distance(transform.position, coverPoints[i].position);
            if (d < minDist)
                minDist = d;
        }
        for (int i = 0; i < coverPoints.Count; i++)
        {
            float d = Vector3.Distance(transform.position, coverPoints[i].position);
            if (d <= minDist * coverSelectionRangeMultiplier)
                candidatePointsBuffer.Add(coverPoints[i]);
        }
        if (lastCoverPoint != null && candidatePointsBuffer.Count > 1)
            candidatePointsBuffer.Remove(lastCoverPoint);
        if (candidatePointsBuffer.Count == 0)
            return;

        CoverFormation.CoverPoint chosen = ChooseBestCandidate(candidatePointsBuffer);
        if (chosen == null)
            return;
        currentCoverTarget = chosen;
        Collider[] hitColliders = Physics.OverlapSphere(currentCoverTarget.position, 0.5f, LayerMask.GetMask("Enemy", "Player"));
        if (hitColliders.Length == 0)
            SetDestinationForCover(currentCoverTarget.position);
        else
            currentCoverTarget = null;
    }

    private int GetOccupancyForCoverPoint(CoverFormation.CoverPoint cp)
    {
        Collider[] colliders = Physics.OverlapSphere(cp.position, coverOccupancyRadius, LayerMask.GetMask("Enemy"));
        int count = 0;
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
                count++;
        }
        return count;
    }

    private CoverFormation.CoverPoint GetAlternativeCover(CoverFormation.CoverPoint currentCover)
    {
        CoverFormation.CoverPoint alternativeCover = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < coverPoints.Count; i++)
        {
            if (coverPoints[i] == currentCover)
                continue;
            float score = EvaluateCoverCandidate(coverPoints[i]);
            if (score < bestScore)
            {
                bestScore = score;
                alternativeCover = coverPoints[i];
            }
        }
        return alternativeCover;
    }

    private CoverFormation.CoverPoint ChooseBestCandidate(List<CoverFormation.CoverPoint> candidates)
    {
        CoverFormation.CoverPoint bestCandidate = null;
        float bestScore = float.MaxValue;
        foreach (var candidate in candidates)
        {
            float score = EvaluateCoverCandidate(candidate) + Random.Range(0f, 0.1f);
            if (score < bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }
        return bestCandidate;
    }

    private float EvaluateCoverCandidate(CoverFormation.CoverPoint candidate)
    {
        float distance = Vector3.Distance(transform.position, candidate.position);
        int occupancy = GetOccupancyForCoverPoint(candidate);
        Vector3 toCover = (candidate.position - transform.position).normalized;
        Vector3 toPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(toCover, toPlayer);
        float angleDiff = Mathf.Abs(angle - 90f);
        return distance * weightDistance + occupancy * weightOccupancy + angleDiff * weightAngle;
    }

    private void SetDestinationForCover(Vector3 destination)
    {
        if (!navMeshAgent.isActiveAndEnabled)
            return;
        if (!navMeshAgent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                navMeshAgent.Warp(hit.position);
            if (!navMeshAgent.isOnNavMesh)
                return;
        }
        if (Vector3.Distance(navMeshAgent.destination, destination) > 0.1f)
        {
            navMeshAgent.SetDestination(destination);
            navMeshAgent.speed = Random.Range(minSpeed, maxSpeed);
        }
    }

    private IEnumerator FollowPath(List<Vector3> path)
    {
        if (!navMeshAgent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                navMeshAgent.Warp(hit.position);
            else
                yield break;
        }
        for (int i = 0; i < path.Count; i++)
        {
            if (navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.SetDestination(path[i]);
                yield return new WaitUntil(() =>
                {
                    if (!navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh)
                        return true;
                    return navMeshAgent.remainingDistance < 0.5f;
                });
            }
            else
                yield break;
        }
    }

    private IEnumerator EngageTimer()
    {
        yield return new WaitForSeconds(Random.Range(minEngageTime, maxEngageTime));
        StopEngage();
    }

    private void StopEngage()
    {
        isEngage = false;
        isCover = true;
    }

    private IEnumerator WaitAndSit()
    {
        isWaiting = true;
        isCover = false;
        yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        isWaiting = false;
        isCover = true;
        yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        isCover = false;
        isWaiting = true;
    }

    private void AttackMode()
    {
        EnemyCovering();
    }

    private void RandomEngage()
    {
        int randomChance = Random.Range(0, 100);
        if (randomChance < engageChance)
        {
            isEngage = true;
            StartCoroutine(EngageTimer());
        }
    }

    private void UpdateAnimatorMovement()
    {
        if (isAmbush)
        {
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
            return;
        }
        Vector3 localDir = transform.InverseTransformDirection(navMeshAgent.velocity);
        float moveX = Mathf.Clamp(localDir.x, -2f, 2f);
        float moveY = Mathf.Clamp(localDir.z, -2f, 2f);
        animator.SetFloat("MoveX", moveX);
        animator.SetFloat("MoveY", moveY);
    }

    private IEnumerator RandomSitRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (!isSit && Random.Range(0, 100) < sitChance)
                StartCoroutine(SitRoutine());
        }
    }

    private IEnumerator SitRoutine()
    {
        isSit = true;
        animator.SetBool("isSit", true);
        yield return new WaitForSeconds(Random.Range(minSitTime, maxSitTime));
        isSit = false;
        animator.SetBool("isSit", false);
    }
}
