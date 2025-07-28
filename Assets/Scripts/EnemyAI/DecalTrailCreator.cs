using System.Collections;
using UnityEngine;

public class DecalTrailCreator : MonoBehaviour
{
    public SimpleDecalContainer decalContainer; // Ссылка на ваш контейнер декалей
    public LayerMask groundLayer; // Слой "Ground"
    public float decalSpacing = 0.5f; // Расстояние между декалями
    public float movementThreshold = 0.1f; // Минимальная скорость для создания декали
    public float decalYOffset = 0.1f; // Оффсет по оси Y для декалей
    public float decalLifetime = 15f; // Время жизни декали (в секундах)

    private Rigidbody rb;
    private Vector3 lastDecalPosition;
    private bool firstDecalPlaced = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("На объекте отсутствует Rigidbody.");
        }

        // Устанавливаем начальную позицию для первой декали
        lastDecalPosition = transform.position;
        PlaceInitialDecal(); // Создаём первую декаль при старте
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Проверяем, движется ли объект
        if (rb.velocity.magnitude > movementThreshold)
        {
            // Проверяем, прошло ли достаточное расстояние для новой декали
            if (!firstDecalPlaced || Vector3.Distance(transform.position, lastDecalPosition) >= decalSpacing)
            {
                PlaceDecal();
            }
        }
    }

    private void PlaceInitialDecal()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, groundLayer))
        {
            Quaternion decalRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Vector3 decalPosition = hit.point + Vector3.up * decalYOffset;

            // Предполагаем, что CreateRandomDecal возвращает GameObject
            GameObject decal = decalContainer.CreateRandomDecal(decalPosition, decalRotation);
            if (decal != null)
            {
                StartCoroutine(DestroyDecalAfterTime(decal));
            }

            lastDecalPosition = transform.position;
            firstDecalPlaced = true;
        }
    }

    private void PlaceDecal()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, groundLayer))
        {
            Quaternion decalRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Vector3 decalPosition = hit.point + Vector3.up * decalYOffset;

            // Предполагаем, что CreateRandomDecal возвращает GameObject
            GameObject decal = decalContainer.CreateRandomDecal(decalPosition, decalRotation);
            if (decal != null)
            {
                StartCoroutine(DestroyDecalAfterTime(decal));
            }

            lastDecalPosition = transform.position;
            firstDecalPlaced = true;
        }
    }

    private IEnumerator DestroyDecalAfterTime(GameObject decal)
    {
        yield return new WaitForSeconds(decalLifetime);
        if (decal != null)
        {
            Destroy(decal);
        }
    }
}
