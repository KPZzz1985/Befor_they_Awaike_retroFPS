using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float bulletSpeed;
    public float destroyDistance;
    public string playerTag;
    public string levelObjectsTag;

    // Добавляем переменную для префаба системы частиц
    public GameObject impactEffectPrefab;

    private Vector3 spawnPosition;
    public float lifeTime = 1.5f;

    public int damage;
    public bool isHoming = true; // Добавляем флаг для включения коррекции траектории
    public float homingStrength = 0.5f; // Насколько сильно пуля будет корректировать свой путь
    public Transform target; // Цель, в которую пуля будет попадать

    public float homingProbability = 1f;

    private Rigidbody rb;

    

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
        target = GameObject.FindGameObjectWithTag(playerTag)?.transform; // Попытка найти игрока как цель

        

        // Задаем начальное направление движения пули
        Vector3 initialDirection = transform.forward;

        // Рандомизируем самонаведение
        isHoming = Random.Range(0f, 1f) <= homingProbability;

        if (isHoming && target != null)
        {
            // Направляем пулю в сторону цели сразу после ее создания
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

        // Если пуля должна самонаводиться и есть цель
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
            // Инстанциируем систему частиц при столкновении
            if (impactEffectPrefab != null)
            {
                // Создаем эффект в точке контакта и направляем его в противоположном направлении движения пули
                GameObject impactEffect = Instantiate(impactEffectPrefab, transform.position, Quaternion.LookRotation(-transform.forward));
                Destroy(impactEffect, 5f); // Уничтожаем систему частиц через 2 секунды
            }
        }
        Destroy(gameObject); // Уничтожаем пулю в любом случае
    }
}
