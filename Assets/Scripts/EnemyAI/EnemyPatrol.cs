using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private float patrolRadius;
    [SerializeField] private int minPatrolPoints = 3;
    [SerializeField] private int maxPatrolPoints;
    [SerializeField] private Vector2 speedRange;
    [SerializeField] private LookRegistrator lookRegistrator;
    [SerializeField] private float minWaitTime;
    [SerializeField] private float maxWaitTime;
    public EnemyMovement2 enemyMovement2; // Добавлено: публичное поле для компонента EnemyMovement2

    private NavMeshAgent navMeshAgent;
    public List<Vector3> patrolPoints;
    private int currentPatrolPoint;
    private bool isWaiting;
    public Animator animator;
    public GameObject player;

    private void OnEnable()
    {
        Debug.Log($"{gameObject.name}: EnemyPatrol ВКЛЮЧЕН!");
    }

    private void Start()
    {
        player = GameObject.FindWithTag("Player");
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = animator.GetComponent<Animator>();
        Respawn();
    }

    public void Respawn()
    {
        patrolPoints = new List<Vector3>();
        int patrolPointsCount = Mathf.Max(minPatrolPoints, Random.Range(minPatrolPoints, maxPatrolPoints + 1));

        for (int i = 0; i < patrolPointsCount; i++)
        {
            patrolPoints.Add(RandomNavmeshLocation(patrolRadius));
        }

        currentPatrolPoint = 0;
        isWaiting = false;
        lookRegistrator.ResetPlayerSeen();
        GoToNextPatrolPoint();
    }

    private void Update()
    {
        if (enemyMovement2 != null && enemyMovement2.enabled)
        {
            Debug.Log($"{gameObject.name}: EnemyPatrol отключается, потому что EnemyMovement2 включен.");
            return;
        }

        Debug.Log($"{gameObject.name}: EnemyPatrol работает.");

        lookRegistrator.UpdatePlayerPosition();

        if (lookRegistrator.PlayerSeen)
        {
            // Если игрок замечен, включаем режим атаки
            EngagePlayer();
        }
        else
        {
            // Оставшийся код патрулирования...
            if (navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh && !navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f && !isWaiting)
            {
                StartCoroutine(WaitAndGoToNextPatrolPoint());
            }
        }

        // Текущее состояние и анимации
        UpdateMovementAnimation();
    }

    private void UpdateMovementAnimation()
    {
        Vector3 movementVector = navMeshAgent.velocity;
        Vector3 normalizedMovementVector = movementVector.normalized;
        Vector3 normalizedLookVector = transform.forward;

        float dotProduct = Vector3.Dot(normalizedMovementVector, normalizedLookVector);

        animator.SetFloat("MoveX", dotProduct);
        animator.SetFloat("MoveY", dotProduct);
        animator.SetFloat("Speed", navMeshAgent.speed);
        animator.SetBool("IsWaiting", isWaiting);
        animator.SetBool("PlayerSeen", lookRegistrator.PlayerSeen);
    }

    public void EngagePlayer()
    {
        enemyMovement2.enabled = true; // Включаем EnemyMovement2
        this.enabled = false;          // Отключаем себя (EnemyPatrol)
    }

    private IEnumerator WaitAndGoToNextPatrolPoint()
    {
        isWaiting = true;
        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(waitTime);
        GoToNextPatrolPoint();
        isWaiting = false;
    }

    private void GoToNextPatrolPoint()
    {
        if (navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh && patrolPoints.Count > 0)
        {
            navMeshAgent.destination = patrolPoints[currentPatrolPoint];
            currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Count;
            navMeshAgent.speed = Random.Range(speedRange.x, speedRange.y);
        }
    }

    private Vector3 RandomNavmeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;

        NavMeshHit hit;
        Vector3 finalPosition = Vector3.zero;

        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }

        return finalPosition;
    }
}
