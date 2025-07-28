using UnityEngine;

[RequireComponent(typeof(Light))]
public class PixelatedShadows : MonoBehaviour
{
    public int shadowResolution = 128; // Разрешение теней

    private Light _light;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _light.shadows = LightShadows.Hard; // Используйте жесткие тени для пиксельного эффекта
    }

    private void Start()
    {
        // Установите разрешение теней
        if (_light.type == LightType.Directional)
        {
            QualitySettings.shadowResolution = (ShadowResolution)shadowResolution;
        }
    }
}
