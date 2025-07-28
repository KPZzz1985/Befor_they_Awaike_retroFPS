using UnityEngine;

namespace MyGame.EnemyAI // ����� ������� ����� ������������ ����
{
    /// <summary>
    /// �������� ��� ����������� ������ �����.
    /// � ������� ����� ����� ����������� ����� �������, �� ����� ���������� � ����������.
    /// </summary>
    public class EnemySkills : MonoBehaviour
    {
        // ��������, ������ ������� ����� ������� ����� (�� ������� ������ ������ ��������)

        /// <summary>
        /// ���������, ����� �� ���� ���� ����� � ����������.
        /// </summary>
        public bool IsAnySkillReady()
        {
            // ���� ������ ���������� true ��� �����
            return true;
        }

        /// <summary>
        /// ���������� ��������� ����� � ���������� ����� ��� ��������.
        /// </summary>
        public float ActivateRandomSkill()
        {
            // ��� ������� ���������� ��������� ����� �������� ������
            float duration = Random.Range(3f, 5f);
            Debug.Log($"����������� ����� �� {duration} ������.");
            return duration;
        }
    }
}
