using UnityEngine;

public class ShootingController : MonoBehaviour
{
    [Header("Shooting Parameters")]
    [SerializeField] public Transform firePoint;
    [SerializeField] public GameObject bulletPrefab;
    [SerializeField] public float fireRate = 1f;
    [SerializeField] public float bulletSpeed = 10f;
    [SerializeField] public LayerMask hitLayers;
    [SerializeField] public float maxShootDistance = 100f;
    [SerializeField] public float damage = 10f;

    private float nextFireTime;

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    public void Shoot()
    {
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, maxShootDistance, hitLayers))
        {
            // Обработка попадания
            Debug.Log("Hit: " + hit.collider.name);

            // Применение урона
        Health targetHealth = hit.transform.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage);
        }

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * bulletSpeed;
        Destroy(bullet, 2f);
        }
    }
}