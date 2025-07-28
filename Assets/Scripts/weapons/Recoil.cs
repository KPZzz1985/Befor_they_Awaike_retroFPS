using UnityEngine;

public class Recoil : MonoBehaviour
{
    [Header("Recoil Settings")]
    public float recoilRange = 0.5f; // диапазон отдачи
    public float recoilSpeed = 10f; // скорость возвращения камеры
    public float recoilDuration = 0.1f; // продолжительность отдачи
    public AnimationCurve recoilCurve; // кривая, задающая силу отдачи

    [Header("Object Settings")]
    public Transform recoilTransform; // объект, который поворачиваем во время отдачи

    [Header("Input Settings")]
    public KeyCode fireButton = KeyCode.Mouse0; // кнопка выстрела

    private bool canFire = true; // флаг, определяющий, можно ли вызвать отдачу
    private float recoilTime; // время прошедшее с начала отдачи
    private Quaternion originalRotation; // исходная ориентация объекта

    private void Start()
    {
        originalRotation = recoilTransform.localRotation; // сохраняем исходную ориентацию
    }

    private void FixedUpdate()
    {
        // Возвращаем объект обратно в исходную ориентацию
        recoilTransform.localRotation = Quaternion.Lerp(recoilTransform.localRotation, originalRotation, Time.deltaTime * recoilSpeed);

        // Отнимаем время от продолжительности отдачи
        recoilTime -= Time.deltaTime;

        // Если время отдачи равно 0, то можно вызвать отдачу снова
        if (recoilTime <= 0)
        {
            canFire = true;
        }
    }

    private void Update()
    {
        // Если уже происходит отдача, то выходим
        if (!canFire)
        {
            return;
        }

        // Если нажата кнопка выстрела, то вызываем отдачу
        if (Input.GetKeyDown(fireButton))
        {
            // Создаем случайное отклонение по вертикали
            float recoilAmount = recoilCurve.Evaluate(Random.value) * recoilRange;
            Quaternion recoilRotation = Quaternion.AngleAxis(recoilAmount, transform.up);

            // Применяем отклонение к ориентации объекта
            Quaternion targetRotation = recoilTransform.localRotation * recoilRotation;
            recoilTransform.localRotation = Quaternion.RotateTowards(recoilTransform.localRotation, targetRotation, 180f);

            // Устанавливаем время отдачи и флаг, что нельзя вызвать отдачу снова
            recoilTime = recoilDuration;
            canFire = false;
        }
    }
}