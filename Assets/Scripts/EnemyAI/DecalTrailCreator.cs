using System.Collections;
using UnityEngine;

public class DecalTrailCreator : MonoBehaviour
{
    public SimpleDecalContainer decalContainer; // ������ �� ��� ��������� �������
    public LayerMask groundLayer; // ���� "Ground"
    public float decalSpacing = 0.5f; // ���������� ����� ��������
    public float movementThreshold = 0.1f; // ����������� �������� ��� �������� ������
    public float decalYOffset = 0.1f; // ������ �� ��� Y ��� �������
    public float decalLifetime = 15f; // ����� ����� ������ (� ��������)

    private Rigidbody rb;
    private Vector3 lastDecalPosition;
    private bool firstDecalPlaced = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("�� ������� ����������� Rigidbody.");
        }

        // ������������� ��������� ������� ��� ������ ������
        lastDecalPosition = transform.position;
        PlaceInitialDecal(); // ������ ������ ������ ��� ������
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // ���������, �������� �� ������
        if (rb.velocity.magnitude > movementThreshold)
        {
            // ���������, ������ �� ����������� ���������� ��� ����� ������
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

            // ������������, ��� CreateRandomDecal ���������� GameObject
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

            // ������������, ��� CreateRandomDecal ���������� GameObject
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
