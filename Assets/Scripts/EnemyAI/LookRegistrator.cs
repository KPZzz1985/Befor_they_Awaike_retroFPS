using UnityEngine;
using System.Collections.Generic;

public class LookRegistrator : MonoBehaviour
{
    [SerializeField] private GameObject head;
    [SerializeField] public float viewDistance;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayers; // Добавлен слой препятствий
    [SerializeField] private Color rayColor = Color.red;
    [SerializeField] private int rayCount = 5;
    [SerializeField] private float spreadAngle = 45f;

    public GameObject Head
    {
        get { return head; }
    }

    private Transform player;
    private bool playerSeen = false;
    private Vector3 playerPosition; // Нужно для UpdatePlayerPosition

    public bool PlayerSeen => playerSeen; // Свойство только для чтения

    private void Start()
    {
        // Находим объект с тегом "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            // Пытаемся найти камеру внутри иерархии игрока
            Camera cam = playerObj.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                player = cam.transform;
            }
            else
            {
                // Если камера не найдена, используем transform корневого объекта игрока
                player = playerObj.transform;
            }
        }
    }

    private void FixedUpdate()
    {
        CheckPlayerVisibility();
    }

    private void CheckPlayerVisibility()
    {
        playerSeen = false;
        Vector3 direction = (player.position - head.transform.position).normalized;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = (i / (float)(rayCount - 1) - 0.5f) * spreadAngle;
            Vector3 rayDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
            Ray ray = new Ray(head.transform.position, rayDirection);

            if (Physics.Raycast(ray, out RaycastHit hit, viewDistance, Physics.DefaultRaycastLayers))
            {
                if (hit.collider.CompareTag("Player") && !IsObstacleBetween(head.transform.position, hit.point))
                {
                    playerSeen = true;
                    playerPosition = hit.transform.position; // Запоминаем последнюю позицию игрока
                    break;
                }
            }

            Debug.DrawLine(head.transform.position, head.transform.position + rayDirection * viewDistance,
                           playerSeen ? Color.green : rayColor);
        }
    }

    private bool IsObstacleBetween(Vector3 start, Vector3 end)
    {
        return Physics.Linecast(start, end, out RaycastHit hit, obstacleLayers);
    }

    // 🔥 Восстанавливаем ResetPlayerSeen() (требуется в EnemyPatrol.cs)
    public void ResetPlayerSeen()
    {
        playerSeen = false;
    }

    // 🔥 Восстанавливаем UpdatePlayerPosition() (требуется в EnemyPatrol.cs)
    public void UpdatePlayerPosition()
    {
        if (player != null)
        {
            playerPosition = player.position; // Обновляем последнюю известную позицию игрока
        }
    }
}
