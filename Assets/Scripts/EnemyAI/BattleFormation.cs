using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BattleFormation : MonoBehaviour
{
    public PlayerTargetSystem playerTargetSystem;
    public TacticalAttackSystem tacticalAttackSystem;
    public float timeBetweenFormationChanges;
    public bool showGizmos;
    public float gizmoSphereSize;
    public Color tacticalPointGizmoColor;
    public Color strafePointGizmoColor;
    public float strafeDistance;

    public List<TacticalPoint> tacticalPoints = new List<TacticalPoint>();
    public List<StrafePoint> leftStrafePoints = new List<StrafePoint>();
    public List<StrafePoint> rightStrafePoints = new List<StrafePoint>();

    [SerializeField] private float minDistanceBetweenPoints;
    [SerializeField] private int pointsInCone;

    [System.Serializable]
    public class TacticalPoint
    {
        public string enemyName;
        public Vector3 position;
    }

    [System.Serializable]
    public class StrafePoint
    {
        public string enemyName;
        public Vector3 position;
    }

    private void Start()
    {
        StartCoroutine(UpdateFormationPoints());
    }

    private IEnumerator UpdateFormationPoints()
    {
        while (true)
        {
            UpdateFormation();
            yield return new WaitForSeconds(timeBetweenFormationChanges);
        }
    }

    private void UpdateFormation()
{
    List<StrategicSystem.EnemyStatus> intruders = tacticalAttackSystem.GetIntruders();
    tacticalPoints.Clear();
    leftStrafePoints.Clear();
    rightStrafePoints.Clear();

    int conePoints = 0;

    for (int i = 0; i < intruders.Count; i++)
    {
        if (intruders[i].enemyObject == null)
        {
            continue;
        }

        Vector3 randomPoint;
        Vector3 enemyDirection = intruders[i].enemyObject.transform.forward;
        do
        {
            randomPoint = GetRandomPointInBattleAreaBasedOnDirection(intruders[i].enemyObject.transform.position, enemyDirection);
        } while (playerTargetSystem.IsPointVisible(randomPoint) || IsPointCloseToEnemiesAround(randomPoint));

        if (playerTargetSystem.IsPointInCone(randomPoint))
        {
            if (conePoints < pointsInCone)
            {
                conePoints++;
            }
            else
            {
                continue;
            }
        }

        tacticalPoints.Add(new TacticalPoint { enemyName = intruders[i].enemyObject.name, position = randomPoint });

        Vector3 strafeDirection = (randomPoint - tacticalAttackSystem.playerTransform.position).normalized;
        Vector3 leftStrafePosition = randomPoint - Vector3.Cross(strafeDirection, Vector3.up) * strafeDistance;
        Vector3 rightStrafePosition = randomPoint + Vector3.Cross(strafeDirection, Vector3.up) * strafeDistance;

        leftStrafePoints.Add(new StrafePoint { enemyName = intruders[i].enemyObject.name, position = leftStrafePosition });
        rightStrafePoints.Add(new StrafePoint { enemyName = intruders[i].enemyObject.name, position = rightStrafePosition });
    }
}

private Vector3 GetRandomPointInBattleAreaBasedOnDirection(Vector3 enemyPosition, Vector3 enemyDirection)
{
    float angleOffset = Random.Range(-45, 45);
    float distance = Random.Range(playerTargetSystem.MinBattleArea, playerTargetSystem.MaxBattleArea);
    Quaternion rotation = Quaternion.Euler(0, angleOffset, 0);
    Vector3 direction = rotation * enemyDirection;
    direction.y = 0;

    Vector3 randomPoint = enemyPosition + direction.normalized * distance;

    NavMeshHit hit;
    if (NavMesh.SamplePosition(randomPoint, out hit, distance, NavMesh.AllAreas))
    {
        return hit.position;
    }

    return randomPoint;
}



   private void OnDrawGizmosSelected()
   {
    if (showGizmos)
    {
        Gizmos.color = tacticalPointGizmoColor;
        foreach (TacticalPoint tacticalPoint in tacticalPoints)
        {
            Gizmos.DrawSphere(tacticalPoint.position, gizmoSphereSize);
        }

        Gizmos.color = strafePointGizmoColor;
        foreach (StrafePoint strafePoint in leftStrafePoints)
        {
            Gizmos.DrawSphere(strafePoint.position, gizmoSphereSize);
        }

        foreach (StrafePoint strafePoint in rightStrafePoints)
        {
            Gizmos.DrawSphere(strafePoint.position, gizmoSphereSize);
        }
    }
   }

   private Vector3 GetRandomPointInBattleArea()
  {
    float angle = Random.Range(0, 360);
    float distance = Random.Range(playerTargetSystem.MinBattleArea, playerTargetSystem.MaxBattleArea);
    Vector3 direction = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
    Vector3 randomPoint = tacticalAttackSystem.playerTransform.position + direction * distance;

    NavMeshHit hit;
    if (NavMesh.SamplePosition(randomPoint, out hit, distance, NavMesh.AllAreas))
    {
        return hit.position;
    }

    return randomPoint;
  }

private bool IsPointCloseToEnemiesAround(Vector3 point)
  {
    foreach (PlayerTargetSystem.EnemyInfo enemyInfo in playerTargetSystem.enemiesAround)
    {
        GameObject enemy = GameObject.Find(enemyInfo.enemyName);
        if (Vector3.Distance(point, enemy.transform.position) <= minDistanceBetweenPoints)
        {
            return true;
        }
    }

    return false;
  }



}