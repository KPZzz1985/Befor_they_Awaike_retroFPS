using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System; // needed for Action
using System.Collections; // needed for IEnumerator
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;
using Random = UnityEngine.Random;

public class StrategicSystem : MonoBehaviour
{
    [System.Serializable]
    public class EnemyStatus
    {
        public GameObject enemyObject;
        public List<string> activeComponents;
        public bool intruderAlert;
        public float activationRadius = 50f;
        public float lastSeenTime;          // time when enemy last saw player
        public float patternDelay;          // random delay before triggering pattern
        public bool patternTriggered;       // flag whether pattern trigger timed out
        // removed abilityUsed; now using round-robin selection among eligible soldiers
        public bool isSitting;              // flag whether enemy is in sitting (ambush) state

        // 🔥 Добавлен новый флаг
        public bool isDead;
        public string weaponName;
        public bool isShooting;
        public bool isPatrol;
        public bool isAssault = false;
        public bool isPlayerSeen = false; // ✅ Новый флаг

        public bool PlayerSeen
        {
            get
            {
                LookRegistrator lookRegistrator = enemyObject?.GetComponent<LookRegistrator>();
                return lookRegistrator != null && lookRegistrator.PlayerSeen;
            }
        }
    }

    public List<EnemyStatus> enemyStatuses = new List<EnemyStatus>();
    public Transform playerTransform;
    public float activationRadius = 50f;
    [SerializeField] private float coverUpdateInterval = 1f; // seconds between cover updates
    [Header("Pattern Timing Settings")]
    [Tooltip("Minimum delay before pattern triggers (seconds)")]
    public float minPatternDelay = 2f;
    [Tooltip("Maximum delay before pattern triggers (seconds)")]
    public float maxPatternDelay = 4f;
    [Header("Ambush Pattern Settings")]
    [Tooltip("Radius around player to sample ambush destination (meters)")]
    public float ambushRadius = 12f;
    [Tooltip("Minimum approach distance from origin towards player (meters)")]
    public float approachDistanceMin = 10f;
    [Tooltip("Maximum approach distance from origin towards player (meters)")]
    public float approachDistanceMax = 15f;
    [Tooltip("How long enemies wait in ambush after arriving (seconds)")]
    public float ambushWaitDuration = 15f;
    [Tooltip("Max spacing of group members when regrouping (meters)")]
    public float groupSpacingRadius = 3f;
    [Tooltip("Number of enemies per ambush group")]
    public int ambushGroupSize = 2;
    [Header("Ability System Settings")]
    [Tooltip("Prefab of the grenade to throw")]
    public GameObject grenadePrefab;
    [Tooltip("Cost in mana for throwing a grenade")]
    public float grenadeManaCost = 65f;
    [Tooltip("Time to charge full mana (seconds)")]
    public float manaRechargeTime = 25f;
    [Tooltip("Maximum mana")]
    public float maxMana = 100f;
    [Tooltip("Delay before grenade is thrown (seconds)")]
    public float grenadeThrowDelay = 3f;
    [Tooltip("Chance (0-1) for grenade throw on each check")]
    [Range(0f,1f)] public float grenadeThrowChance = 0.5f;
    [Tooltip("Minimum seconds between grenade throw checks")]
    public float grenadeCheckIntervalMin = 2.5f;
    [Tooltip("Maximum seconds between grenade throw checks")]
    public float grenadeCheckIntervalMax = 5f;
    [Tooltip("Max distance from soldier to player to allow throw (meters)")]
    public float grenadeThrowDistance = 12f;
    private float currentMana = 0f;
    // Round-robin tracer for last thrower to prevent same unit throwing twice in a row
    private EnemyStatus lastThrowerStatus;
    public event Action<List<CoverFormation.CoverPoint>> OnCoverPointsUpdated; // notify subscribers of new cover points

    private CoverFormation coverFormation;
    private TacticalAttackSystem tacticalAttackSystem;
    private BattleFormation battleFormation;
    private EnemyChecker enemyChecker;
    private Dictionary<string, IBehaviorPattern> patterns;
    private Dictionary<string, CancellationTokenSource> runningPatterns;

    // Add music settings
    [Header("Music Settings")]
    [SerializeField] private AudioClip ambientClip;
    [SerializeField] private AudioClip fightClip;
    [SerializeField] [Range(0f,1f)] private float ambientVolume = 1f;
    [SerializeField] private float fadeDuration = 3f;
    [SerializeField] private float exitDelay = 10f;
    [SerializeField] private float exitFadeDuration = 3f;

    private AudioSource ambientSource;
    private AudioSource fightSource;
    private bool isInFightMusic = false;
    private float exitTimer = 0f;

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        coverFormation = FindObjectOfType<CoverFormation>();
        tacticalAttackSystem = FindObjectOfType<TacticalAttackSystem>();
        battleFormation = FindObjectOfType<BattleFormation>();
        enemyChecker = FindObjectOfType<EnemyChecker>();

        FindEnemies();
        // initialize last thrower
        lastThrowerStatus = null;

        // init pattern system
        patterns = new Dictionary<string, IBehaviorPattern>();
        runningPatterns = new Dictionary<string, CancellationTokenSource>();
        // register patterns
        patterns["L1Soldier"] = new L1AmbushPattern(this);
        currentMana = 0f;

        // Initialize audio sources
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.clip = ambientClip;
        ambientSource.loop = true;
        ambientSource.volume = ambientVolume;
        ambientSource.Play();

        fightSource = gameObject.AddComponent<AudioSource>();
        fightSource.clip = fightClip;
        fightSource.loop = true;
        fightSource.volume = 0f;
        fightSource.Play();
    }

    private void Start()
    {
        // start grenade throw loop
        GrenadeLoop().Forget();
    }

    // Removed coroutine-based tick; using Update() for continuous cover generation

    private void Update()
    {
        // continuous cover updates
        if (coverFormation != null)
        {
            coverFormation.GenerateCoverPoints();
            OnCoverPointsUpdated?.Invoke(coverFormation.GetCoverPoints());
        }
        // recharge mana
        if (manaRechargeTime > 0f)
            currentMana = Mathf.Min(maxMana, currentMana + (maxMana * Time.deltaTime / manaRechargeTime));
        UpdateEnemyStatuses();
        UpdateIntruderAlert();
        UpdatePatternTriggers();
        UpdateActivation();
        ManageMusic();
    }

    private void FindEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            EnemyStatus newEnemyStatus = new EnemyStatus
            {
                enemyObject = enemy,
                intruderAlert = false,
                activeComponents = new List<string>(),
                activationRadius = activationRadius
            };
            // initialize pattern tracking
            newEnemyStatus.lastSeenTime = Time.time;
            newEnemyStatus.patternDelay = UnityEngine.Random.Range(minPatternDelay, maxPatternDelay);
            newEnemyStatus.patternTriggered = false;
            // abilityUsed flag removed; round-robin via lastThrowerStatus
            enemyStatuses.Add(newEnemyStatus);
            // we'll cache L1Soldier statuses later after all are added
        }
    }

    private void UpdateEnemyStatuses()
    {
        foreach (EnemyStatus enemyStatus in enemyStatuses)
        {
            if (enemyStatus.enemyObject == null) continue;

            EnemyHealth enemyHealth = enemyStatus.enemyObject.GetComponent<EnemyHealth>();
            EnemyWeapon enemyWeapon = enemyStatus.enemyObject.GetComponent<EnemyWeapon>();
            EnemyPatrol enemyPatrol = enemyStatus.enemyObject.GetComponent<EnemyPatrol>();
            EnemyMovement2 enemyMovement = enemyStatus.enemyObject.GetComponent<EnemyMovement2>();

            // 🔥 Обновляем информацию о враге
            enemyStatus.isDead = enemyHealth != null && enemyHealth.isDead;
            enemyStatus.weaponName = enemyChecker != null ? enemyChecker.GetEnemyWeaponName(enemyStatus.enemyObject) : "Unknown";
            enemyStatus.isPatrol = enemyPatrol != null && enemyPatrol.enabled;
            enemyStatus.isPlayerSeen = enemyStatus.PlayerSeen;

            // ✅ Теперь StrategicSystem управляет стрельбой
            enemyStatus.isShooting = enemyStatus.isPlayerSeen;

            // ✅ Если тревога включена, отключаем патруль и включаем штурм
            if (enemyStatus.intruderAlert)
            {
                if (enemyPatrol != null && enemyPatrol.enabled)
                {
                    enemyPatrol.enabled = false;
                }

                if (enemyMovement != null && !enemyMovement.enabled)
                {
                    enemyMovement.enabled = true;
                }

                enemyStatus.isAssault = true;
            }
        }
    }


    private void UpdateIntruderAlert()
    {
        foreach (EnemyStatus enemyStatus in enemyStatuses)
        {
            if (enemyStatus.enemyObject == null) continue;

            if (enemyStatus.PlayerSeen)
            {
               // reset last seen and pattern trigger when player is seen
               enemyStatus.lastSeenTime = Time.time;
               enemyStatus.patternTriggered = false;
               enemyStatus.patternDelay = UnityEngine.Random.Range(minPatternDelay, maxPatternDelay);
                enemyStatus.intruderAlert = true;
                AlertNearbyEnemies(enemyStatus);
            }
        }
    }

    private void AlertNearbyEnemies(EnemyStatus sourceEnemyStatus)
    {
        AlertRadius alertRadius = sourceEnemyStatus.enemyObject.GetComponent<AlertRadius>();
        if (alertRadius != null)
        {
            foreach (EnemyStatus otherEnemyStatus in enemyStatuses)
            {
                if (otherEnemyStatus.enemyObject != null && !otherEnemyStatus.PlayerSeen)
                {
                    float distance = Vector3.Distance(sourceEnemyStatus.enemyObject.transform.position, otherEnemyStatus.enemyObject.transform.position);
                    if (distance <= alertRadius.alertRadius)
                    {
                        otherEnemyStatus.intruderAlert = true;
                    }
                }
            }
        }
    }

    private void UpdatePatternTriggers()
    {
        foreach (var status in enemyStatuses)
        {
            if (status.isDead) continue;
            if (status.intruderAlert && !status.PlayerSeen && status.patternTriggered)
            {
                string key = status.enemyObject.name;
                // start pattern if registered and not already running
                if (patterns.ContainsKey(key) && !runningPatterns.ContainsKey(key))
                {
                    // collect group of statuses for this key
                    var group = enemyStatuses.FindAll(s => s.enemyObject.name == key && s.intruderAlert && s.patternTriggered && !s.PlayerSeen);
                    if (group.Count >= 2)
                    {
                        var cts = new CancellationTokenSource();
                        runningPatterns[key] = cts;
                        patterns[key]
                            .RunAsync(group, cts.Token)
                            .ContinueWith(() =>
                            {
                                // cleanup when done
                                runningPatterns.Remove(key);
                                // reset flags for statuses
                                foreach (var s in group)
                                {
                                    s.patternTriggered = false;
                                }
                            })
                            .Forget();
                    }
                }
            }
        }
    }

    private void UpdateActivation()
    {
        foreach (EnemyStatus enemyStatus in enemyStatuses)
        {
            if (enemyStatus.enemyObject == null) continue;

            float distance = Vector3.Distance(playerTransform.position, enemyStatus.enemyObject.transform.position);
            if (distance <= enemyStatus.activationRadius)
            {
                ActivateEnemy(enemyStatus, true);
            }
            else
            {
                ActivateEnemy(enemyStatus, false);
            }
        }
    }

    private void ActivateEnemy(EnemyStatus enemyStatus, bool activate)
    {
        if (enemyStatus.enemyObject == null) return;

        MonoBehaviour[] components = enemyStatus.enemyObject.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component is EnemyPatrol && enemyStatus.intruderAlert)
            {
                component.enabled = false;
                Debug.Log($"[StrategicSystem] Отключаем EnemyPatrol у {enemyStatus.enemyObject.name} (IntruderAlert активен)");
                continue;
            }

            if (!(component is EnemyMovement2) && !(component is DecalTrailCreator))
            {
                component.enabled = activate;
            }
        }
    }

    private async UniTask ThrowAbility(EnemyStatus status, CancellationToken cancellationToken)
    {
        // trigger throw animation
        var animator = status.enemyObject.GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger("Throw");
        // disable movement and shooting
        var movement = status.enemyObject.GetComponent<EnemyMovement2>();
        var weapon = status.enemyObject.GetComponent<EnemyWeapon>();
        // stop NavMeshAgent
        var agent = status.enemyObject.GetComponent<NavMeshAgent>();
        // only stop agent if it's on a NavMesh
        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = true;
        if (movement != null) movement.enabled = false;
        if (weapon != null) weapon.enabled = false;

        // wait for throw delay (animation placeholder)
        await UniTask.Delay(TimeSpan.FromSeconds(grenadeThrowDelay), cancellationToken: cancellationToken);

        // determine spawn position: child 'GrenadeSpawnPoint' if present, else root + offset
        Transform spawnPoint = status.enemyObject.transform.Find("GrenadeSpawnPoint");
        Vector3 spawnPos = (spawnPoint != null)
            ? spawnPoint.position
            : status.enemyObject.transform.position + Vector3.up * 1f;
        var grenade = Instantiate(grenadePrefab, spawnPos, Quaternion.identity);
        var rb = grenade.GetComponent<Rigidbody>() ?? grenade.AddComponent<Rigidbody>();
        Vector3 dir = (playerTransform.position - spawnPos).normalized;
        float speed = 10f;
        rb.velocity = dir * speed + Vector3.up * 5f;

        // resume movement and shooting
        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = false;
        if (movement != null) movement.enabled = true;
        if (weapon != null) weapon.enabled = true;
    }

    private async UniTaskVoid GrenadeLoop()
    {
        while (true)
        {
            float wait = Random.Range(grenadeCheckIntervalMin, grenadeCheckIntervalMax);
            await UniTask.Delay(TimeSpan.FromSeconds(wait));
            TryGrenadeThrow();
        }
    }

    private void TryGrenadeThrow()
    {
        if (grenadePrefab == null || currentMana < grenadeManaCost) return;
        // find eligible soldiers
        var eligible = enemyStatuses
            .Where(s => !s.isDead
                     && s.enemyObject.name.StartsWith("L1Soldier")
                     && s.intruderAlert
                     && !s.PlayerSeen
                     && Vector3.Distance(s.enemyObject.transform.position, playerTransform.position) <= grenadeThrowDistance)
            .ToList();
        if (eligible.Count == 0) return;
        // pick random excluding last thrower
        var pool = eligible.Where(s => s != lastThrowerStatus).ToList();
        if (pool.Count == 0) pool = eligible;
        var chosen = pool[Random.Range(0, pool.Count)];
        // chance check
        if (Random.value <= grenadeThrowChance)
        {
            currentMana = Mathf.Max(0f, currentMana - grenadeManaCost);
            ThrowAbility(chosen, CancellationToken.None).Forget();
        }
        lastThrowerStatus = chosen;
    }

    public List<Vector3> GenerateUniquePath(Vector3 start, Vector3 end)
    {
        List<Vector3> path = new List<Vector3>();
        path.Add(start);

        int numIntermediatePoints = UnityEngine.Random.Range(2, 4);
        for (int i = 0; i < numIntermediatePoints; i++)
        {
            Vector3 randomPoint = Vector3.Lerp(start, end, (float)(i + 1) / (numIntermediatePoints + 1)) +
                                  new Vector3(UnityEngine.Random.Range(-2f, 2f), 0, UnityEngine.Random.Range(-2f, 2f));

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                path.Add(hit.position);
            }
            else
            {
                Debug.LogWarning("Сгенерированная точка вне NavMesh. Пропускаем точку.");
            }
        }

        path.Add(end);
        return path;
    }

    // Visual debug of strategic system state
    private void OnGUI()
    {
        float width = 200f;
        float x = Screen.width - width - 10f;
        float y = 10f;
        GUI.Label(new Rect(x, y, width, 20), $"Mana: {currentMana:F1}/{maxMana}");
        y += 22f;
        GUI.Label(new Rect(x, y, width, 20), $"Rechratesec: {manaRechargeTime:F1}");
        y += 22f;
        GUI.Label(new Rect(x, y, width, 20), $"GrenadeCost: {grenadeManaCost}");
    }

    // Music management
    private void ManageMusic()
    {
        bool anyAlert = enemyStatuses.Any(s => s.intruderAlert && !s.isDead);
        if (anyAlert)
        {
            exitTimer = 0f;
            if (!isInFightMusic)
            {
                isInFightMusic = true;
                StopAllCoroutines();
                // Restart fight track from beginning
                fightSource.Stop();
                fightSource.Play();
                // Fade out ambient and fade in fight music
                StartCoroutine(FadeMusic(ambientSource, ambientSource.volume, 0f, fadeDuration));
                StartCoroutine(FadeMusic(fightSource, fightSource.volume, ambientVolume, fadeDuration));
            }
        }
        else
        {
            if (isInFightMusic)
            {
                exitTimer += Time.deltaTime;
                if (exitTimer >= exitDelay)
                {
                    isInFightMusic = false;
                    StopAllCoroutines();
                    StartCoroutine(FadeMusic(fightSource, fightSource.volume, 0f, exitFadeDuration));
                    StartCoroutine(FadeMusic(ambientSource, ambientSource.volume, ambientVolume, exitFadeDuration));
                }
            }
        }
    }

    private IEnumerator FadeMusic(AudioSource source, float from, float to, float duration)
    {
        float elapsed = 0f;
        source.volume = from;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        source.volume = to;
    }
}
