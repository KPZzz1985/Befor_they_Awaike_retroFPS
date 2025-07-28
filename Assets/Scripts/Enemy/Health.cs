using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // Обработка смерти объекта, например, уничтожение или анимация
    Debug.Log(gameObject.name + " died.");
    Destroy(gameObject); // Уничтожение объекта. Замените эту строку, если нужно вызвать анимацию смерти или другую логику.
    }
}