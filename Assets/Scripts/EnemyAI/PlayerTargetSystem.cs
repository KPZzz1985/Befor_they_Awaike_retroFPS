using System.Collections.Generic;
using UnityEngine;

public class PlayerTargetSystem : MonoBehaviour
{
    [System.Serializable]
    public class EnemyInfo
    {
        public string enemyName;
        public bool visibilityRegistration;
        public bool directVisibility;

        public EnemyInfo(GameObject enemy, bool visibilityRegistration, bool directVisibility)
        {
            enemyName = enemy.name;
            this.visibilityRegistration = visibilityRegistration;
            this.directVisibility = directVisibility;
        }
    }

    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask levelWallsLayer;
    [SerializeField] private int rayCount = 8;
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private Color rayColor = Color.red;
    [SerializeField] public int coneAngle = 30;
    [SerializeField] private float coneLength = 10f;
    [SerializeField] private Color coneColor = Color.green;
    [SerializeField] public float minBattleArea = 5f;
    [SerializeField] public float maxBattleArea = 15f;
    [SerializeField] public List<EnemyInfo> enemiesAround = new List<EnemyInfo>();
    [SerializeField] public List<EnemyInfo> enemiesAhead = new List<EnemyInfo>();
    [SerializeField] private Transform cameraRotator;

    private List<float> rayLengths = new List<float>(); // Список для хранения обрезанных длин лучей

    public float MinBattleArea => minBattleArea;
    public float MaxBattleArea => maxBattleArea;

    public int RayCount => rayCount;
    public float RayLength => rayLength;

    [SerializeField] private LayerMask groundLayer; // Слой Ground
    [SerializeField] private float rayOriginHeight = 1.5f; // Высота над Ground
    private Vector3 rayOriginFixed; // Точка генерации лучей

    public Vector3 RayOriginFixed => rayOriginFixed;

    private void Update()
    {
        // Очищаем списки врагов и обрезанные длины лучей
        enemiesAround.Clear();
        enemiesAhead.Clear();
        rayLengths.Clear();

        // Проверяем лучи вокруг игрока
        float angleStep = 360f / rayCount;
        for (int i = 0; i < rayCount; i++)
        {
            Vector3 direction = Quaternion.Euler(0, angleStep * i, 0) * transform.forward;

            // Используем сохранённую точку генерации
            Vector3 rayOrigin = rayOriginFixed;

            Ray ray = new Ray(rayOrigin, direction);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, rayLength, enemyLayer | levelWallsLayer))
            {
                Debug.DrawRay(rayOrigin, direction * hit.distance, rayColor);
                rayLengths.Add(hit.distance);

                if (hit.collider.gameObject.layer != LayerMask.NameToLayer("LevelWalls"))
                {
                    if (!ContainsEnemy(enemiesAround, hit.collider.gameObject))
                    {
                        EnemyInfo enemyInfo = new EnemyInfo(hit.collider.gameObject, true, false);
                        enemiesAround.Add(enemyInfo);
                    }
                }
            }
            else
            {
                Debug.DrawRay(rayOrigin, direction * rayLength, rayColor);
                rayLengths.Add(rayLength);
            }
        }

        // Аналогичная логика для лучей перед игроком
        float horizontalAngleStep = coneAngle / rayCount;
        for (int i = 0; i < rayCount; i++)
        {
            Vector3 direction = Quaternion.Euler(0, -coneAngle * 0.5f + horizontalAngleStep * i, 0) * cameraRotator.forward;

            Vector3 rayOrigin = rayOriginFixed; // Используем фиксированную точку

            Ray ray = new Ray(rayOrigin, direction);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, coneLength, enemyLayer | levelWallsLayer))
            {
                Debug.DrawRay(rayOrigin, direction * hit.distance, coneColor);

                if (hit.collider.gameObject.layer != LayerMask.NameToLayer("LevelWalls"))
                {
                    if (!ContainsEnemy(enemiesAhead, hit.collider.gameObject))
                    {
                        EnemyInfo enemyInfo = new EnemyInfo(hit.collider.gameObject, false, true);
                        enemiesAhead.Add(enemyInfo);
                    }
                }
            }
            else
            {
                Debug.DrawRay(rayOrigin, direction * coneLength, coneColor);
            }
        }
    }

    private void LateUpdate()
    {
        Vector3 basePosition = transform.position;

        RaycastHit hit;
        if (Physics.Raycast(basePosition + Vector3.up * 10f, Vector3.down, out hit, Mathf.Infinity, groundLayer))
        {
            // Если найден Ground с тэгом
            if (hit.collider.CompareTag("Ground"))
            {
                rayOriginFixed = hit.point + Vector3.up * rayOriginHeight;
            }
            else
            {
                // Если нет тэга Ground, но есть коллайдер
                rayOriginFixed = hit.point + Vector3.up * rayOriginHeight;
            }
        }
        else
        {
            // Если Ground не найден, оставляем текущую точку
            rayOriginFixed = basePosition + Vector3.up * rayOriginHeight;
        }
    }

    private bool ContainsEnemy(List<EnemyInfo> enemies, GameObject enemy)
    {
        foreach (EnemyInfo enemyInfo in enemies)
        {
            if (enemyInfo.enemyName == enemy.name)
            {
                return true;
            }
        }
        return false;
    }

    public List<float> GetRayLengths()
    {
        return rayLengths;
    }

    public bool IsPointVisible(Vector3 point)
    {
        Vector3 directionToTarget = (point - transform.position).normalized;

        if (Vector3.Angle(cameraRotator.transform.forward, directionToTarget) < coneAngle / 2)
        {
            float distanceToTarget = Vector3.Distance(transform.position, point);
            if (distanceToTarget < coneLength)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, directionToTarget, out hit, coneLength, enemyLayer | levelWallsLayer))
                {
                    if (hit.collider.gameObject.layer != LayerMask.NameToLayer("LevelWalls"))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool IsPointInCone(Vector3 point)
    {
        Vector3 directionToTarget = (point - cameraRotator.position).normalized;

        if (Vector3.Angle(cameraRotator.transform.forward, directionToTarget) < coneAngle / 2)
        {
            float distanceToTarget = Vector3.Distance(cameraRotator.position, point);
            return distanceToTarget < coneLength;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // Рисуем радиусы minBattleArea и maxBattleArea
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minBattleArea);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxBattleArea);
    }
}
