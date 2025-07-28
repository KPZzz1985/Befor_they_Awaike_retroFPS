using System.Collections.Generic;
using UnityEngine;

public class TacticalAttackSystem : MonoBehaviour
{
    [SerializeField] private StrategicSystem strategicSystem;
    [SerializeField] public List<StrategicSystem.EnemyStatus> intruderAlertEnemies;
    [SerializeField] public Transform playerTransform;

    private void Start()
    {
        intruderAlertEnemies = new List<StrategicSystem.EnemyStatus>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        intruderAlertEnemies.Clear();
        foreach (var enemyStatus in strategicSystem.enemyStatuses)
        {
            if (enemyStatus.intruderAlert)
            {
                intruderAlertEnemies.Add(enemyStatus);
            }
        }
    }

    public List<StrategicSystem.EnemyStatus> GetIntruders()
    {
        return intruderAlertEnemies;
    }

    private void OnDrawGizmos()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                return; // Если объект игрока не найден – выходим, чтобы не получить ошибку
            }
        }

        Camera playerCamera = playerTransform.GetComponentInChildren<Camera>();
        Vector3 playerPos = (playerCamera != null)
                            ? playerCamera.transform.position
                            : playerTransform.position;

        if (strategicSystem != null)
        {
            foreach (var enemyStatus in strategicSystem.enemyStatuses)
            {
                if (enemyStatus.enemyObject != null)
                {
                    LookRegistrator lr = enemyStatus.enemyObject.GetComponentInChildren<LookRegistrator>();
                    Vector3 enemyPos = (lr != null && lr.Head != null)
                        ? lr.Head.transform.position
                        : enemyStatus.enemyObject.transform.position;

                    if (enemyStatus.PlayerSeen)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(enemyPos, playerPos);
                    }
                    else if (enemyStatus.intruderAlert)
                    {
                        Gizmos.color = Color.yellow;
                        for (float i = 0; i < 1; i += 0.2f)
                        {
                            Vector3 start = Vector3.Lerp(enemyPos, playerPos, i);
                            Vector3 end = Vector3.Lerp(enemyPos, playerPos, i + 0.1f);
                            Gizmos.DrawLine(start, end);
                        }
                    }
                }
            }
        }
    }




}
