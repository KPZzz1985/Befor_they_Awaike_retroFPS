using System.Collections; // Убедитесь, что используете это пространство имён
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleSystemModifier : MonoBehaviour
{
    private ParticleSystem ps; // Изменено для читаемости

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        StartCoroutine(AdjustParticleSystem(ps.main.duration));
    }

    private IEnumerator AdjustParticleSystem(float duration)
    {
        float elapsedTime = 0f;
        var mainModule = ps.main; // Получаем основной модуль здесь, чтобы избежать ошибки только для чтения

        float startGravityModifier = mainModule.gravityModifier.constant;
        float startSizeMin = mainModule.startSize.constantMin;
        float startSizeMax = mainModule.startSize.constantMax;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Плавное изменение значений
            float newSizeMin = Mathf.Lerp(startSizeMin, 0f, progress);
            float newSizeMax = Mathf.Lerp(startSizeMax, 0f, progress);
            float newGravityModifier = Mathf.Lerp(startGravityModifier, 100f, progress);

            mainModule.startSize = new ParticleSystem.MinMaxCurve(newSizeMin, newSizeMax);
            mainModule.gravityModifier = newGravityModifier; // Исправлено для корректной установки значения

            yield return null;
        }

        // Установка конечных значений
        mainModule.startSize = new ParticleSystem.MinMaxCurve(0f, 0f);
        mainModule.gravityModifier = 40f; // Исправлено для корректной установки значения
    }
}
