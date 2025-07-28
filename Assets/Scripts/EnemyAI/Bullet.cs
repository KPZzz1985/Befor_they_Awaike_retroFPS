using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float bulletSpeed;
    public float destroyDistance;
    public string playerTag;
    public string levelObjectsTag;

    // ��������� ���������� ��� ������� ������� ������
    public GameObject impactEffectPrefab;

    private Vector3 spawnPosition;
    public float lifeTime = 1.5f;

    public int damage;
    public bool isHoming = true; // ��������� ���� ��� ��������� ��������� ����������
    public float homingStrength = 0.5f; // ��������� ������ ���� ����� �������������� ���� ����
    public Transform target; // ����, � ������� ���� ����� ��������

    public float homingProbability = 1f;

    private Rigidbody rb;

    

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
        target = GameObject.FindGameObjectWithTag(playerTag)?.transform; // ������� ����� ������ ��� ����

        

        // ������ ��������� ����������� �������� ����
        Vector3 initialDirection = transform.forward;

        // ������������� �������������
        isHoming = Random.Range(0f, 1f) <= homingProbability;

        if (isHoming && target != null)
        {
            // ���������� ���� � ������� ���� ����� ����� �� ��������
            initialDirection = (target.position - spawnPosition).normalized;
        }

        rb.velocity = initialDirection * bulletSpeed;
        Destroy(gameObject, lifeTime);
    }


    void Update()
    {
        if (Vector3.Distance(spawnPosition, transform.position) > destroyDistance)
        {
            Destroy(gameObject);
        }

        // ���� ���� ������ �������������� � ���� ����
        if (isHoming && target != null)
        {
            Vector3 homingDirection = (target.position - transform.position).normalized;
            rb.velocity = Vector3.Lerp(rb.velocity.normalized, homingDirection, homingStrength * Time.deltaTime) * bulletSpeed;
        }
    }

    public void SetBulletParameters(float speed, float distance, string player, string levelObjects)
    {
        bulletSpeed = speed;
        destroyDistance = distance;
        playerTag = player;
        levelObjectsTag = levelObjects;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
        else if (other.CompareTag(levelObjectsTag) || other.CompareTag("Ground"))
        {
            // ������������� ������� ������ ��� ������������
            if (impactEffectPrefab != null)
            {
                // ������� ������ � ����� �������� � ���������� ��� � ��������������� ����������� �������� ����
                GameObject impactEffect = Instantiate(impactEffectPrefab, transform.position, Quaternion.LookRotation(-transform.forward));
                Destroy(impactEffect, 5f); // ���������� ������� ������ ����� 2 �������
            }
        }
        Destroy(gameObject); // ���������� ���� � ����� ������
    }
}
