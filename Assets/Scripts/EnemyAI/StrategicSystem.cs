using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class StrategicSystem : MonoBehaviour
{
    [System.Serializable]
    public class EnemyStatus
    {
        public GameObject enemyObject;
        public List<string> activeComponents;
        public bool intruderAlert;
        public float activationRadius = 50f;

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

    private CoverFormation coverFormation;
    private TacticalAttackSystem tacticalAttackSystem;
    private BattleFormation battleFormation;
    private EnemyChecker enemyChecker;

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        coverFormation = FindObjectOfType<CoverFormation>();
        tacticalAttackSystem = FindObjectOfType<TacticalAttackSystem>();
        battleFormation = FindObjectOfType<BattleFormation>();
        enemyChecker = FindObjectOfType<EnemyChecker>();

        FindEnemies();
    }

    private void Update()
    {
        UpdateEnemyStatuses();
        UpdateIntruderAlert();
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

    public List<Vector3> GenerateUniquePath(Vector3 start, Vector3 end)
    {
        List<Vector3> path = new List<Vector3>();
        path.Add(start);

        int numIntermediatePoints = Random.Range(2, 4);
        for (int i = 0; i < numIntermediatePoints; i++)
        {
            Vector3 randomPoint = Vector3.Lerp(start, end, (float)(i + 1) / (numIntermediatePoints + 1)) +
                                  new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));

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
