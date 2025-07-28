using UnityEngine;

public class AlertRadius : MonoBehaviour
{
    public float alertRadius = 10f; // ������, ������� ����� ����������
    public Color alertColor = Color.yellow; // ���� �������
    public bool showGizmo = true; // ���������� �� ����� � ���������

    public float minAlertDelay = 0.5f; // ����������� �������� ����������
    public float maxAlertDelay = 2.0f; // ������������ �������� ����������

    private void OnDrawGizmosSelected()
    {
        if (showGizmo)
        {
            Gizmos.color = alertColor;
            Gizmos.DrawWireSphere(transform.position, alertRadius);
        }
    }
}
