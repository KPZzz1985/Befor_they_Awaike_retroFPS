using UnityEngine;

public class WeaponSwinger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Максимальное расстояние до стены, при котором начнется отъезжание оружия.")]
    public float maxDistanceToWall = 1.0f;

    [Tooltip("На какое расстояние оружие будет отъезжать от стены.")]
    public float offsetDistance = 0.5f;

    [Tooltip("Скорость, с которой оружие будет возвращаться в исходное положение.")]
    public float smoothness = 5.0f;

    [Tooltip("Объект, откуда будет исходить луч.")]
    public Transform raycastOrigin; // Добавляем это поле

    private GameObject weaponHolder;
    private Vector3 originalPosition;
    public bool isRaycastHitting = false;

    private void Start()
    {
        weaponHolder = GameObject.FindGameObjectWithTag("playerWeapon");

        if (weaponHolder != null)
        {
            Debug.Log("Объект с тегом playerWeapon: " + weaponHolder.name);
            originalPosition = weaponHolder.transform.localPosition;
        }
        else
        {
            Debug.LogError("Объект с тегом weaponHolder не найден!");
        }
    }

    private void Update()
    {
        if (weaponHolder == null || raycastOrigin == null) // Проверяем, что raycastOrigin не null
            return;

        RaycastHit hit;
        Vector3 raycastStartPosition = raycastOrigin.position;

        if (Physics.Raycast(raycastStartPosition, raycastOrigin.forward, out hit, maxDistanceToWall))
        {
            if (!isRaycastHitting)
            {
                Debug.Log("Луч начал регистрировать объект!");
                isRaycastHitting = true;
            }

            Debug.Log("Луч столкнулся с объектом: " + hit.transform.name);
           Vector3 targetPosition = originalPosition - new Vector3(0, 0, offsetDistance);
           weaponHolder.transform.localPosition = Vector3.Lerp(weaponHolder.transform.localPosition, targetPosition, Time.deltaTime * smoothness);
        }
        else
        {
            if (isRaycastHitting)
            {
                Debug.Log("Луч больше не регистрирует объект.");
                isRaycastHitting = false;
            }
            weaponHolder.transform.localPosition = Vector3.Lerp(weaponHolder.transform.localPosition, originalPosition, Time.deltaTime * smoothness);
        }

        Debug.DrawRay(raycastStartPosition, transform.forward * maxDistanceToWall, Color.yellow);
    }
}
