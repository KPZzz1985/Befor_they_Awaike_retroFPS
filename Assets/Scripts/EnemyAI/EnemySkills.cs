using UnityEngine;

namespace MyGame.EnemyAI // ћожно указать любое пространство имен
{
    /// <summary>
    /// «аглушка дл€ специальных умений врага.
    /// ¬ будущем здесь можно реализовать набор навыков, их врем€ подготовки и выполнени€.
    /// </summary>
    public class EnemySkills : MonoBehaviour
    {
        // Ќапример, список навыков можно хранить здесь (на текущий момент просто заглушка)

        /// <summary>
        /// ѕровер€ет, готов ли хоть один навык к применению.
        /// </summary>
        public bool IsAnySkillReady()
        {
            // ѕока всегда возвращаем true дл€ теста
            return true;
        }

        /// <summary>
        /// јктивирует случайный навык и возвращает врем€ его действи€.
        /// </summary>
        public float ActivateRandomSkill()
        {
            // ƒл€ примера возвращаем случайное врем€ действи€ навыка
            float duration = Random.Range(3f, 5f);
            Debug.Log($"јктивирован навык на {duration} секунд.");
            return duration;
        }
    }
}
