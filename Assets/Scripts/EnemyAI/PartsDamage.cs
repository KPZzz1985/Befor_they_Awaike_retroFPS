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

        // Здесь можно добавить эффекты, такие как визуальные или звуковые отклики на урон
        Debug.Log($"{gameObject.name} получил урон. Текущее здоровье: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Здесь можно добавить эффекты, такие как взрыв, звук смерти, и т.д.
        Debug.Log($"{gameObject.name} уничтожен.");
        
        // Уничтожить объект после смерти
        Destroy(gameObject);
    }
}
