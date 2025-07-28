using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CoverFormation : MonoBehaviour
{
    public PlayerTargetSystem playerTargetSystem;
    public GameObject Player;
    [SerializeField] public float minDistanceBetweenRayPoints;
    [SerializeField] public List<CoverPoint> coverPointArray = new List<CoverPoint>();
    [SerializeField] private bool showGizmos;
    [SerializeField] private float gizmoSphereSize;
    [SerializeField] private Color gizmoColor;
    [SerializeField] public List<string> debugHitObjects = new List<string>(); // Лист для дебага объектов, попавшихся на пути луча

    [System.Serializable]
    public class CoverPoint
    {
        public string pointName;
        public Vector3 position;
    }

    private Vector3 previousPlayerPosition;
    [SerializeField] private List<Vector3> gizmoPoints = new List<Vector3>();

    public List<CoverPoint> GetCoverPoints()
    {
        return coverPointArray;
    }

    private void Start()
    {
        previousPlayerPosition = Player.transform.position;
    }

    private void Update()
    {
        if (Vector3.Distance(previousPlayerPosition, Player.transform.position) > 0.1f)
        {
            GenerateCoverPoints();
            previousPlayerPosition = Player.transform.position;
        }
    }

    private void GenerateCoverPoints()
    {
        coverPointArray.Clear();
        gizmoPoints.Clear();
        debugHitObjects.Clear(); // Очистка листа перед новой генерацией точек укрытия

        int coverPointIndex = 0;
        List<float> rayLengths = playerTargetSystem.GetRayLengths(); // Получаем обрезанные длины лучей
        float angleStep = 360f / playerTargetSystem.RayCount;

        for (int i = 0; i < playerTargetSystem.RayCount; i++)
        {
            Vector3 direction = Quaternion.Euler(0, angleStep * i, 0) * Player.transform.forward;

            debugHitObjects.Clear(); // Очищаем лист перед каждым лучом
            Vector3 previousCoverPointPosition = Vector3.zero;
            float currentRayLength = rayLengths[i];
            Vector3 rayOrigin = playerTargetSystem.RayOriginFixed; // Используем публичное свойство вместо метода
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, direction, currentRayLength);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance)); // Сортируем по расстоянию, чтобы обрабатывать объекты в порядке их удаления

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("LevelWalls"))
                {
                    // Пропускаем объекты в слое LevelWalls
                    continue;
                }

                if (!debugHitObjects.Contains(hit.collider.gameObject.name))
                {
                    debugHitObjects.Add(hit.collider.gameObject.name); // Добавляем имя объекта в лист для дебага, если его там еще нет
                }

                if (hit.collider.CompareTag("LevelObjects"))
                {
                    // Объект не в слое LevelWalls, создаём точку укрытия за ним
                    Vector3 hitPoint = hit.point;
                    Vector3 behindPoint = hitPoint + direction * minDistanceBetweenRayPoints;

                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(behindPoint, out navHit, 1f, NavMesh.AllAreas))
                    {
                        behindPoint = navHit.position;
                    }

                    if (!Physics.CheckSphere(behindPoint, gizmoSphereSize) && behindPoint != previousCoverPointPosition)
                    {
                        coverPointArray.Add(new CoverPoint { pointName = $"coverPoint{coverPointIndex}", position = behindPoint });
                        previousCoverPointPosition = behindPoint;
                        coverPointIndex++;

                        if (showGizmos)
                        {
                            gizmoPoints.Add(behindPoint);
                        }
                    }
                }
            }
        }
    }

    public CoverPoint GetNearestCoverPoint(Vector3 currentPosition)
    {
        float minDistance = Mathf.Infinity;
        CoverPoint nearestPoint = null;

        foreach (CoverPoint coverPoint in coverPointArray)
        {
            float distance = Vector3.Distance(currentPosition, coverPoint.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPoint = coverPoint;
            }
        }
        return nearestPoint;
    }

    public CoverPoint GetFarthestCoverPoint(Vector3 currentPosition)
    {
        float maxDistance = 0;
        CoverPoint farthestPoint = null;

        foreach (CoverPoint coverPoint in coverPointArray)
        {
            float distance = Vector3.Distance(currentPosition, coverPoint.position);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthestPoint = coverPoint;
            }
        }
        return farthestPoint;
    }

    private void OnDrawGizmos()
    {
        if (showGizmos)
        {
            Gizmos.color = gizmoColor;
            foreach (Vector3 point in gizmoPoints)
            {
                Gizmos.DrawWireSphere(point, gizmoSphereSize);
            }
        }
    }
}
