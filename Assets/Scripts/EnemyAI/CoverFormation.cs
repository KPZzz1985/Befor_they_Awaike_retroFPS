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
    [SerializeField] public List<string> debugHitObjects = new List<string>(); // ���� ��� ������ ��������, ���������� �� ���� ����

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

    // cover generation is now handled by StrategicSystem tick loop
    private void Update()
    {
        // intentionally left empty
    }

    public void GenerateCoverPoints()
    {
        coverPointArray.Clear();
        gizmoPoints.Clear();
        debugHitObjects.Clear(); // ������� ����� ����� ����� ���������� ����� �������

        int coverPointIndex = 0;
        List<float> rayLengths = playerTargetSystem.GetRayLengths(); // �������� ���������� ����� �����
        float angleStep = 360f / playerTargetSystem.RayCount;

        for (int i = 0; i < playerTargetSystem.RayCount; i++)
        {
            Vector3 direction = Quaternion.Euler(0, angleStep * i, 0) * Player.transform.forward;

            debugHitObjects.Clear(); // ������� ���� ����� ������ �����
            Vector3 previousCoverPointPosition = Vector3.zero;
            float currentRayLength = rayLengths[i];
            Vector3 rayOrigin = playerTargetSystem.RayOriginFixed; // ���������� ��������� �������� ������ ������
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, direction, currentRayLength);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance)); // ��������� �� ����������, ����� ������������ ������� � ������� �� ��������

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("LevelWalls"))
                {
                    // ���������� ������� � ���� LevelWalls
                    continue;
                }

                if (!debugHitObjects.Contains(hit.collider.gameObject.name))
                {
                    debugHitObjects.Add(hit.collider.gameObject.name); // ��������� ��� ������� � ���� ��� ������, ���� ��� ��� ��� ���
                }

                if (hit.collider.CompareTag("LevelObjects"))
                {
                    // ������ �� � ���� LevelWalls, ������ ����� ������� �� ���
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
