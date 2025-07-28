using UnityEngine;

public class VirtualMuscle : MonoBehaviour
{
    public CharacterJoint joint;
    public float strength;
    public Vector3 defaultRotation;
    public float animationTime;
    public float DieDuration; // Время затухания

    private Quaternion initialRotation;
    private float timer;
    private bool isReversed = false;
    private float dieTimer = 0; // Таймер для отслеживания затухания

    void Start()
    {
        if (joint == null)
        {
            joint = GetComponent<CharacterJoint>();
            Debug.Log("[VirtualMuscle] Joint получен из GameObject.");
        }

        initialRotation = joint.transform.localRotation;
        Debug.Log($"[VirtualMuscle] Поворот установлен: {initialRotation}");
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= animationTime)
        {
            isReversed = !isReversed;
            timer = 0;
            Debug.Log($"[VirtualMuscle] Анимация ревертнута: {isReversed}");
        }

        ApplyMuscleForce();

        // Проверяем, началось ли затухание
        if (dieTimer >= DieDuration)
        {
            strength -= Time.deltaTime; // Постепенно уменьшаем силу
            if (strength <= 0)
            {
                Debug.Log("[VirtualMuscle] Скрипт отключен.");
                this.enabled = false; // Отключаем скрипт
            }
        }
        else
        {
            dieTimer += Time.deltaTime;
        }
    }

    void ApplyMuscleForce()
    {
        Vector3 rotation = isReversed ? -defaultRotation : defaultRotation;
        Quaternion targetRotation = Quaternion.Euler(rotation) * initialRotation;

        joint.transform.localRotation = Quaternion.Slerp(joint.transform.localRotation, targetRotation, Time.deltaTime * strength);
        Debug.Log($"[VirtualMuscle] Сила применена. Поворот на: {targetRotation}");
    }
}
