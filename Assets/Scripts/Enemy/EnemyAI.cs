using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    public enum EnemyState { Idle, Patrolling, Attacking, SeekingCover, Hiding, Chase, Retreat, UnderFire }

    [Header("General Settings")]
    public EnemyState initialState;
    public Transform player;
    public LayerMask playerLayer;
    public LayerMask coverLayer;
    public float sightRadius = 15f;
    public float coverSearchRadius = 10f;
    public float strafeSpeed = 3f;
    public float strafeIntensity = 2f;
    private Transform coverSpot;

    [Header("Shooting Settings")]
    public ShootingController shootingController;
    public float shootingAccuracy = 0.8f;
    public float timeBetweenShots = 2f;
    public float timeHiding = 3f;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolPointRange = 2f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;

    [Header("Retreat Settings")]
    public float retreatHealthPercentage = 10f;
    public float retreatDistance = 30f;

    [Header("UnderFire Settings")]
    public float underFireRadius = 5f;
    public float underFireRetreatRadius = 2f;

    [Header("Chase Settings")]
    public float attackRange = 5f;

    private EnemyState currentState;
    private NavMeshAgent agent;
    private int currentPatrolPoint;
    private bool canShoot = true;
    private Health health;
    private bool coverAvailable = false;
    private bool hasSeenPlayer = false;
    private List<CoverFormation.CoverPoint> currentCoverPoints;
    private StrategicSystem strategicSystem;

    void Start()
    {
        // subscribe to cover updates
        strategicSystem = FindObjectOfType<StrategicSystem>();
        if (strategicSystem != null)
        {
            strategicSystem.OnCoverPointsUpdated += HandleCoverPointsUpdated;
        }
        agent = GetComponent<NavMeshAgent>();
        currentState = initialState;
        agent.speed = patrolSpeed;
        health = GetComponent<Health>();

        if (!player)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        if (!coverSpot)
        {
            coverSpot = GameObject.FindGameObjectWithTag("CoverSpot").transform;
        }
    }

    private void OnDisable()
    {
        if (strategicSystem != null)
        {
            strategicSystem.OnCoverPointsUpdated -= HandleCoverPointsUpdated;
        }
    }

    private void HandleCoverPointsUpdated(List<CoverFormation.CoverPoint> covers)
    {
        currentCoverPoints = covers;
        // reset coverAvailable so it will be evaluated fresh in AI logic
        coverAvailable = covers != null && covers.Count > 0;
    }

    void Update()
    {
        if (hasSeenPlayer || Vector3.Distance(player.position, transform.position) <= sightRadius)
        {
            hasSeenPlayer = true;

            if (health.GetHealthPercentage() <= retreatHealthPercentage)
            {
                currentState = EnemyState.Retreat;
            }
            else if (Vector3.Distance(transform.position, player.position) > attackRange)
            {
                currentState = EnemyState.Chase;
            }
            else // default to attack
            {
                currentState = EnemyState.Attacking;
            }
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                agent.isStopped = true;
                Debug.Log("State: Idle");
                break;

            case EnemyState.Patrolling:
                Patrol();
                Debug.Log("State: Patrol");
                break;

            case EnemyState.Chase:
                Chase();
                Debug.Log("Patrol: Chase");
                break;

            case EnemyState.Attacking:
                Attack();
                Debug.Log("Patrol: Attack");
                break;

            case EnemyState.SeekingCover:
                FindCover();
                Debug.Log("State: FindCover");
                break;

           case EnemyState.Hiding:
                StartCoroutine(LeaveCover());
                Debug.Log("State: LeaveCover");
                break;

           case EnemyState.Retreat:
                Retreat();
                Debug.Log("State: Retreat");
                break;

           case EnemyState.UnderFire:
                UnderFire();
                Debug.Log("State: UnderFire");
                break;
        }
    }


    private void Patrol()
  {
    agent.isStopped = false;
    agent.SetDestination(patrolPoints[currentPatrolPoint].position);

    if (Vector3.Distance(transform.position, patrolPoints[currentPatrolPoint].position) < patrolPointRange)
    {
        // �������� ��������� ����� �������������� �� ������
        currentPatrolPoint = Random.Range(0, patrolPoints.Length);
    }
  }

    private void Attack()
  {
    if (coverAvailable && coverSpot)
    {
        currentState = EnemyState.SeekingCover;
    }
    else
    {
        Strafe();
    }

    transform.LookAt(player);

    if (canShoot && Random.value <= shootingAccuracy)
    {
        StartCoroutine(Shoot());
    }
  }

    private void FindCover()
  {
        // Select nearest dynamic cover point from strategic system
        if (currentCoverPoints != null && currentCoverPoints.Count > 0)
        {
            float minDist = Mathf.Infinity;
            CoverFormation.CoverPoint bestPoint = null;
            foreach (var cp in currentCoverPoints)
            {
                float d = Vector3.Distance(transform.position, cp.position);
                if (d < minDist)
                {
                    minDist = d;
                    bestPoint = cp;
                }
            }
            if (bestPoint != null)
            {
                coverAvailable = true;
                agent.isStopped = false;
                agent.SetDestination(bestPoint.position);
                return;
            }
        }
        // No dynamic cover available
        coverAvailable = false;
  }

   private void Strafe()
  {
    agent.isStopped = false;
    float direction = Mathf.Sign(Random.Range(-1f, 1f));
    Vector3 strafeDirection = transform.right * direction;
    Vector3 targetPosition = transform.position + strafeDirection * strafeSpeed * strafeIntensity;
    agent.SetDestination(targetPosition);
  }

    IEnumerator Shoot()
    {
        canShoot = false;
        shootingController.Shoot();
        yield return new WaitForSeconds(timeBetweenShots);
        canShoot = true;
    }

    IEnumerator LeaveCover()
  {
      yield return new WaitForSeconds(timeHiding);
      currentState = EnemyState.Attacking;
  }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, coverSearchRadius);
    }
      
    private void Retreat()
  {
    if (currentState != EnemyState.SeekingCover && coverAvailable)
    {
        FindCover();
    }
    else
    {
        agent.isStopped = false;
        agent.SetDestination(coverSpot.position);
        transform.LookAt(player);

        if (canShoot && Random.value <= shootingAccuracy)
        {
            StartCoroutine(Shoot());
        }

        if (IsInCover())
        {
            currentState = EnemyState.UnderFire;
            agent.isStopped = true;
        }
    }
  }
    private void Chase()
  {
    agent.isStopped = false;
    agent.SetDestination(player.position);

    if (Vector3.Distance(transform.position, player.position) <= attackRange)
    {
        if (health.GetHealthPercentage() <= retreatHealthPercentage)
        {
            currentState = EnemyState.Retreat;
        }
        else if (coverAvailable)
        {
            currentState = EnemyState.SeekingCover;
        }
        else
        {
            currentState = EnemyState.Attacking;
        }
    }
  }

  private bool IsInCover()
  {
    RaycastHit hit;
    Vector3 directionToPlayer = (player.position - transform.position).normalized;

    if (Physics.Raycast(transform.position, directionToPlayer, out hit))
    {
        if (hit.transform == player)
        {
            return false; // ����� ����� ����������
        }
    }
    return true; // ��������� ��������� � �������
  }

  private void UnderFire()
  {
    agent.isStopped = true;
    transform.LookAt(player);

    if (Vector3.Distance(transform.position, player.position) <= underFireRadius)
    {
        if (Vector3.Distance(transform.position, player.position) <= underFireRetreatRadius)
        {
            currentState = EnemyState.SeekingCover;
        }
        else if (canShoot && Random.value <= shootingAccuracy)
        {
            StartCoroutine(Shoot());
        }
    }
  }
}
