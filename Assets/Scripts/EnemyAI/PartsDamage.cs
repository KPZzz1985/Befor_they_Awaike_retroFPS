using UnityEngine;

public class PartsDamage : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        // ����� ����� �������� �������, ����� ��� ���������� ��� �������� ������� �� ����
        Debug.Log($"{gameObject.name} ������� ����. ������� ��������: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // ����� ����� �������� �������, ����� ��� �����, ���� ������, � �.�.
        Debug.Log($"{gameObject.name} ���������.");
        
        // ���������� ������ ����� ������
        Destroy(gameObject);
    }
}
