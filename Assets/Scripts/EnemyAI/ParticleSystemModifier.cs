using System.Collections; // ���������, ��� ����������� ��� ������������ ���
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleSystemModifier : MonoBehaviour
{
    private ParticleSystem ps; // �������� ��� ����������

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        StartCoroutine(AdjustParticleSystem(ps.main.duration));
    }

    private IEnumerator AdjustParticleSystem(float duration)
    {
        float elapsedTime = 0f;
        var mainModule = ps.main; // �������� �������� ������ �����, ����� �������� ������ ������ ��� ������

        float startGravityModifier = mainModule.gravityModifier.constant;
        float startSizeMin = mainModule.startSize.constantMin;
        float startSizeMax = mainModule.startSize.constantMax;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // ������� ��������� ��������
            float newSizeMin = Mathf.Lerp(startSizeMin, 0f, progress);
            float newSizeMax = Mathf.Lerp(startSizeMax, 0f, progress);
            float newGravityModifier = Mathf.Lerp(startGravityModifier, 100f, progress);

            mainModule.startSize = new ParticleSystem.MinMaxCurve(newSizeMin, newSizeMax);
            mainModule.gravityModifier = newGravityModifier; // ���������� ��� ���������� ��������� ��������

            yield return null;
        }

        // ��������� �������� ��������
        mainModule.startSize = new ParticleSystem.MinMaxCurve(0f, 0f);
        mainModule.gravityModifier = 40f; // ���������� ��� ���������� ��������� ��������
    }
}
