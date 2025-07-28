using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RandomizedSmoke : MonoBehaviour
{
    [Header("Вероятность того, что частицы вообще запустятся")]
    [Tooltip("0 → никогда не запустятся, 1 → всегда запустятся")]
    [Range(0f, 1f)]
    public float playChance = 0.5f;

    [Header("Максимальные значения (диапазон от 0 до этих значений)")]
    [Tooltip("Start Lifetime (секунды)")]
    public float maxStartLifetime = 0.75f;
    [Tooltip("Start Speed")]
    public float maxStartSpeed = 1.24f;
    [Tooltip("Start Size (3D) X, Y, Z")]
    public Vector3 maxStartSize = new Vector3(0.8f, 0.4f, 0.8f);
    [Tooltip("Gravity Modifier (диапазон мин…макс)")]
    public float minGravityModifier = -5f;
    public float maxGravityModifier = 0f;
    [Tooltip("Simulation Speed")]
    public float maxSimulationSpeed = 0.25f;

    // Ссылка на ParticleSystem
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        // Отключаем PlayOnAwake через API, на всякий случай:
        var main = ps.main;
        main.playOnAwake = false;

        // Если система частиц где-то «сработает» в Awake до изменения playOnAwake,
        // сразу её останавливаем и очищаем:
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Включаем 3D-режим для размера (чтобы задавать X, Y, Z отдельно):
        main.startSize3D = true;
    }

    private void Start()
    {
        // Сначала проверим: нужно ли вообще запускать частицы?
        if (!ShouldPlay())
        {
            // Если не проходим по вероятности, то просто уходим, не вызывая ps.Play()
            // Система останется в отключённом состоянии.
            return;
        }

        // Если дошли сюда — будем настраивать случайные параметры и запускать.
        ApplyRandomParameters();
        ps.Play();
    }

    /// <summary>
    /// Проверяет рандомом, запускать ли систему частиц.
    /// Возвращает true, если выпало <= playChance.
    /// </summary>
    private bool ShouldPlay()
    {
        // Random.value возвращает float в [0, 1). 
        // Если мы хотим, чтобы система запускалась с вероятностью playChance,
        // то проверяем Random.value < playChance.
        return Random.value < playChance;
    }

    /// <summary>
    /// Задаёт всем нужным параметрам случайное значение в диапазоне [0 .. max…].
    /// </summary>
    private void ApplyRandomParameters()
    {
        var main = ps.main;

        // Случайная продолжительность жизни частицы [0, maxStartLifetime]
        main.startLifetime = Random.Range(0f, maxStartLifetime);

        // Случайная скорость [0, maxStartSpeed]
        main.startSpeed = Random.Range(0f, maxStartSpeed);

        // Случайный 3D-размер (каждая ось от 0 до maxStartSize.*)
        main.startSizeX = new ParticleSystem.MinMaxCurve(Random.Range(0f, maxStartSize.x));
        main.startSizeY = new ParticleSystem.MinMaxCurve(Random.Range(0f, maxStartSize.y));
        main.startSizeZ = new ParticleSystem.MinMaxCurve(Random.Range(0f, maxStartSize.z));

        // Случайный Gravity Modifier [minGravityModifier, maxGravityModifier]
        main.gravityModifier = Random.Range(minGravityModifier, maxGravityModifier);

        // Случайная Simulation Speed [0, maxSimulationSpeed]
        main.simulationSpeed = Random.Range(0f, maxSimulationSpeed);
    }
}
