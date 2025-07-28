using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectHandler : MonoBehaviour
{
    private EnemyHealth enemyHealth;
    private Animator animator;

    private EnemyMovement2 movement;
    private EnemyWeapon weapon;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        animator = GetComponent<Animator>();

        movement = GetComponent<EnemyMovement2>();        // Footnote: enemy movement controller
        weapon = GetComponent<EnemyWeapon>();          // Footnote: enemy shooting controller
    }

    public void ApplyEffect(StatusEffectData data)
    {
        StartCoroutine(EffectCoroutine(data));
    }

    private IEnumerator EffectCoroutine(StatusEffectData data)
    {
        // 1) Включаем визуалку/анимацию
        animator.SetBool(data.enterParam, true);

        if (data.isEffectAnimation)
        {
            movement.enabled = false;      // Footnote: disable movement
            weapon.enabled = false;      // Footnote: disable shooting
        }

        float elapsed = 0f;
        while (elapsed < data.duration && !enemyHealth.isDead)
        {
            yield return new WaitForSeconds(data.tickInterval);
            enemyHealth.TakeDamage(data.tickDamage, transform.position, Vector3.zero, data.type);
            elapsed += data.tickInterval;
        }

        // 2) Завершаем эффект
        if (!enemyHealth.isDead)
        {
            animator.SetBool(data.exitParam, false);

            if (data.isEffectAnimation)
            {
                movement.enabled = true;   // Footnote: re-enable movement
                weapon.enabled = true;   // Footnote: re-enable shooting
            }
        }
    }

}

