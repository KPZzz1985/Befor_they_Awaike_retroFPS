using UnityEngine;

public class WeaponSwinger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("������������ ���������� �� �����, ��� ������� �������� ���������� ������.")]
    public float maxDistanceToWall = 1.0f;

    [Tooltip("�� ����� ���������� ������ ����� ��������� �� �����.")]
    public float offsetDistance = 0.5f;

    [Tooltip("��������, � ������� ������ ����� ������������ � �������� ���������.")]
    public float smoothness = 5.0f;

    [Tooltip("������, ������ ����� �������� ���.")]
    public Transform raycastOrigin; // ��������� ��� ����

    private GameObject weaponHolder;
    private Vector3 originalPosition;
    public bool isRaycastHitting = false;

    private void Start()
    {
        weaponHolder = GameObject.FindGameObjectWithTag("playerWeapon");

        if (weaponHolder != null)
        {
            Debug.Log("������ � ����� playerWeapon: " + weaponHolder.name);
            originalPosition = weaponHolder.transform.localPosition;
        }
        else
        {
            Debug.LogError("������ � ����� weaponHolder �� ������!");
        }
    }

    private void Update()
    {
        if (weaponHolder == null || raycastOrigin == null) // ���������, ��� raycastOrigin �� null
            return;

        RaycastHit hit;
        Vector3 raycastStartPosition = raycastOrigin.position;

        if (Physics.Raycast(raycastStartPosition, raycastOrigin.forward, out hit, maxDistanceToWall))
        {
            if (!isRaycastHitting)
            {
                Debug.Log("��� ����� �������������� ������!");
                isRaycastHitting = true;
            }

            Debug.Log("��� ���������� � ��������: " + hit.transform.name);
           Vector3 targetPosition = originalPosition - new Vector3(0, 0, offsetDistance);
           weaponHolder.transform.localPosition = Vector3.Lerp(weaponHolder.transform.localPosition, targetPosition, Time.deltaTime * smoothness);
        }
        else
        {
            if (isRaycastHitting)
            {
                Debug.Log("��� ������ �� ������������ ������.");
                isRaycastHitting = false;
            }
            weaponHolder.transform.localPosition = Vector3.Lerp(weaponHolder.transform.localPosition, originalPosition, Time.deltaTime * smoothness);
        }

        Debug.DrawRay(raycastStartPosition, transform.forward * maxDistanceToWall, Color.yellow);
    }
}
