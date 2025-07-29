using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System; // needed for Action
using System.Collections; // needed for IEnumerator
using Cysharp.Threading.Tasks;
using System.Threading;

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
        public bool abilityUsed;     // whether this enemy has used its grenade ability
        public bool isSitting;             // flag whether enemy is in sitting (ambush) state

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
    [Tooltip("Delay before grenade is thrown (seconds)")]
    public float grenadeThrowDelay = 3f;
    private float currentMana = 0f;
    public event Action<List<CoverFormation.CoverPoint>> OnCoverPointsUpdated; // notify subscribers of new cover points

    private CoverFormation coverFormation;
    private TacticalAttackSystem tacticalAttackSystem;
    private BattleFormation battleFormation;
    private EnemyChecker enemyChecker;
    private Dictionary<string, IBehaviorPattern> patterns;
    private Dictionary<string, CancellationTokenSource> runningPatterns;

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        coverFormation = FindObjectOfType<CoverFormation>();
        tacticalAttackSystem = FindObjectOfType<TacticalAttackSystem>();
        battleFormation = FindObjectOfType<BattleFormation>();
        enemyChecker = FindObjectOfType<EnemyChecker>();

        FindEnemies();

        // init pattern system
        patterns = new Dictionary<string, IBehaviorPattern>();
        runningPatterns = new Dictionary<string, CancellationTokenSource>();
        // register patterns
        patterns["L1Soldier"] = new L1AmbushPattern(this);
        currentMana = 0f;
    }

    private void Start()
    {
        // cover updates will run every frame in Update()
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
            currentMana = Mathf.Min(GrenadeManaMax, currentMana + (grenadeManaCost * Time.deltaTime / manaRechargeTime));
        UpdateAbilities();
        UpdateEnemyStatuses();
        UpdateIntruderAlert();
        UpdatePatternTriggers();
        UpdateActivation();
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
            enemyStatuses.Add(newEnemyStatus);
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

    private void UpdateAbilities()
    {
        if (grenadePrefab == null) return;
        if (currentMana < grenadeManaCost) return;
        // find first eligible soldier
        foreach (var status in enemyStatuses)
        {
            if (status.enemyObject.name == "L1Soldier" && status.intruderAlert && !status.PlayerSeen && !status.abilityUsed)
            {
                status.abilityUsed = true;
                currentMana -= grenadeManaCost;
                var ai = status.enemyObject.GetComponent<EnemyAI>();
                if (ai != null)
                    ai.PerformGrenadeThrow(grenadePrefab, grenadeThrowDelay, CancellationToken.None).Forget();
                break;
            }
        }
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
}
