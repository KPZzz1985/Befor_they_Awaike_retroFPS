using UnityEngine;

public class AlertRadius : MonoBehaviour
{
    public float alertRadius = 10f; // Радиус, который нужно отображать
    public Color alertColor = Color.yellow; // Цвет радиуса
    public bool showGizmo = true; // Показывать ли гизмо в редакторе

    public float minAlertDelay = 0.5f; // Минимальная задержка оповещения
    public float maxAlertDelay = 2.0f; // Максимальная задержка оповещения

    private void OnDrawGizmosSelected()
    {
        if (showGizmo)
        {
            Gizmos.color = alertColor;
            Gizmos.DrawWireSphere(transform.position, alertRadius);
        }
    }
}
