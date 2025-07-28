using UnityEngine;

public class SideLeaningFPSController : MonoBehaviour
{
    [Header("Bone Reference")]
    public Transform leanBone;

    [Header("Leaning Settings")]
    public float leanAngle = 15f; // Максимальный угол наклона
    public float leanSpeed = 5f; // Скорость наклона
    public float smoothness = 0.1f; // Плавность наклона

    [Header("Offset Settings")]
    public Vector2 leanOffset = new Vector2(0.1f, 0.05f); // Оффсет по X (в сторону) и Y (по вертикали)

    [Header("Leaning Collision Settings")]
    public Transform leanRaycastOrigin; // Точка, от которой выполняется Raycast
    public float leanRaycastDistance = 0.5f; // Дистанция Raycast для проверки препятствий
    public LayerMask collisionLayer; // Слой препятствий

    private float currentLean = 0f;
    private Vector3 initialBonePosition;

    void Start()
    {
        if (leanBone != null)
        {
            initialBonePosition = leanBone.localPosition;
        }
    }

    void Update()
    {
        float targetLean = 0f;
        Vector3 targetOffset = initialBonePosition;

        // Определение целевого наклона и оффсета на основе ввода игрока
        if (Input.GetKey(KeyCode.E))
        {
            if (!Physics.Raycast(leanRaycastOrigin.position, leanRaycastOrigin.right, leanRaycastDistance, collisionLayer))
            {
                targetLean = -leanAngle;
                targetOffset = initialBonePosition + new Vector3(-leanOffset.x, leanOffset.y, 0f);
            }
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            if (!Physics.Raycast(leanRaycastOrigin.position, -leanRaycastOrigin.right, leanRaycastDistance, collisionLayer))
            {
                targetLean = leanAngle;
                targetOffset = initialBonePosition + new Vector3(leanOffset.x, leanOffset.y, 0f);
            }
        }

        // Интерполяция угла наклона для плавности
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);
        Vector3 currentOffset = Vector3.Lerp(leanBone.localPosition, targetOffset, Time.deltaTime * leanSpeed);

        // Применение наклона и оффсета к кости
        if (leanBone != null)
        {
            leanBone.localRotation = Quaternion.Euler(0f, 0f, currentLean);
            leanBone.localPosition = currentOffset;
        }
    }
}
