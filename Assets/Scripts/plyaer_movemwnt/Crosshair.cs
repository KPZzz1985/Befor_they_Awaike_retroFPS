// Crosshair.cs
using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public float size = 100f;
    public float shootingSize = 150f;
    public Color color = Color.white;

    private WeaponAim weaponAim;
    private PM_Shooting pmShooting;
    private float currentSize;
    private Vector3 screenPoint;
    private Camera mainCamera;

    // Получаем ссылку на PlayerHealth
    private PlayerHealth playerHealth;

    private void Start()
    {
        GameObject weaponHolder = GameObject.FindWithTag("weaponHolder");
        if (weaponHolder != null)
        {
            weaponAim = weaponHolder.GetComponent<WeaponAim>();
            mainCamera = weaponAim.mainCamera;
        }
        else
        {
            Debug.LogError("Не найден объект с тегом weaponHolder");
        }

        GameObject playerWeapon = GameObject.FindWithTag("playerWeapon");
        if (playerWeapon != null)
        {
            pmShooting = playerWeapon.GetComponent<PM_Shooting>();
        }
        else
        {
            Debug.LogError("Не найден объект с тегом playerWeapon");
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
        else
        {
            Debug.LogError("Не найден объект с тегом Player");
        }

        currentSize = size;
    }

    private void Update()
    {
        screenPoint = new Vector3(Screen.width / 2, Screen.height / 2, 0);

        // Вместо pmShooting.isReloading проверяем параметр аниматора через weaponAim.weaponAnimator
        bool isReloading = weaponAim.weaponAnimator.GetBool("isReloading");
        currentSize = Mathf.Lerp(
            currentSize,
            (weaponAim.isAiming || isReloading)
                ? 0
                : (Input.GetMouseButton(0) ? shootingSize : size),
            Time.deltaTime * weaponAim.aimSpeed
        );
    }

    private void OnGUI()
    {
        if (weaponAim == null || pmShooting == null)
            return;

        // Проверяем параметр аниматора вместо pmShooting.isReloading
        bool isReloading = weaponAim.weaponAnimator.GetBool("isReloading");
        if (weaponAim.isAiming || isReloading)
            return;

        GUI.color = color;
        GUI.DrawTexture(new Rect(
            screenPoint.x - currentSize / 2,
            Screen.height - screenPoint.y - 1,
            currentSize, 1),
            Texture2D.whiteTexture
        );
        GUI.DrawTexture(new Rect(
            screenPoint.x - 1,
            Screen.height - screenPoint.y - currentSize / 2,
            1, currentSize),
            Texture2D.whiteTexture
        );

        // Отображаем текущее здоровье
        if (playerHealth != null)
        {
            GUI.Label(new Rect(10, Screen.height - 30, 200, 30), playerHealth.CurrentHealth.ToString());
        }
    }
}
