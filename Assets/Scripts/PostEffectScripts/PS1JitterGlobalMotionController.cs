using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PS1JitterGlobalMotionController : MonoBehaviour
{
    [Header("Настройки эффекта при движении (когда камера двигается/вращается)")]
    public float presetTextureFPS = 10f;
    public float presetJitterAmount = 1f;
    public float presetTextureJitterAmplitude = 0.01f;

    [Header("Пороговые значения для определения движения/вращения")]
    // Если изменение позиции за кадр больше этого порога – считаем, что есть движение
    public float movementThreshold = 0.001f;
    // Если изменение угла за кадр больше этого порога – считаем, что камера вращается
    public float rotationThreshold = 0.1f;

    // Для отслеживания предыдущего состояния камеры
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    // Список материалов, использующих шейдер PSOneShaderGI_JitterWithTextureFrameRate
    private Material[] targetMaterials;

    // Дефолтные значения параметров, которые заданы через SetPSOneShaderParametersWindow
    private float defaultTextureFPS;
    private float defaultJitterAmount;
    private float defaultTextureJitterAmplitude;

    void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;

        // Находим все рендереры в сцене
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        List<Material> mats = new List<Material>();

        foreach (Renderer rend in renderers)
        {
            // Используем sharedMaterials, чтобы получить материалы, назначенные объектам
            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat != null && mat.shader != null)
                {
                    // Если имя шейдера соответствует нужному (убедитесь, что имя указано точно)
                    if (mat.shader.name == "Custom/PSOneShaderGI_JitterWithTextureFrameRate")
                    {
                        if (!mats.Contains(mat))
                            mats.Add(mat);
                    }
                }
            }
        }
        targetMaterials = mats.ToArray();
        Debug.Log($"Найдено {targetMaterials.Length} материалов с шейдером PSOneShaderGI_JitterWithTextureFrameRate.");

        // Сохраняем дефолтные значения из первого найденного материала (предполагается, что все они заданы одинаково)
        if (targetMaterials.Length > 0)
        {
            defaultTextureFPS = targetMaterials[0].GetFloat("_TextureFPS");
            defaultJitterAmount = targetMaterials[0].GetFloat("_JitterAmount");
            defaultTextureJitterAmplitude = targetMaterials[0].GetFloat("_TextureJitterAmplitude");
        }
    }

    void Update()
    {
        // Вычисляем, насколько изменилась позиция и вращение камеры за кадр
        float movementDelta = Vector3.Distance(transform.position, lastPosition);
        float rotationDelta = Quaternion.Angle(transform.rotation, lastRotation);

        float textureFPS, jitterAmount, textureJitterAmplitude;

        // Если камера движется или вращается – применяем preset-значения
        if (movementDelta > movementThreshold || rotationDelta > rotationThreshold)
        {
            textureFPS = presetTextureFPS;
            jitterAmount = presetJitterAmount;
            textureJitterAmplitude = presetTextureJitterAmplitude;
        }
        else
        {
            // Если камера неподвижна – восстанавливаем дефолтные значения,
            // установленные через окно настроек, а не затираем их нулями
            textureFPS = defaultTextureFPS;
            jitterAmount = defaultJitterAmount;
            textureJitterAmplitude = defaultTextureJitterAmplitude;
        }

        // Обновляем параметры во всех найденных материалах
        foreach (Material mat in targetMaterials)
        {
            if (mat != null)
            {
                mat.SetFloat("_TextureFPS", textureFPS);
                mat.SetFloat("_JitterAmount", jitterAmount);
                mat.SetFloat("_TextureJitterAmplitude", textureJitterAmplitude);
            }
        }

        // Обновляем предыдущие значения для следующего кадра
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }
}
