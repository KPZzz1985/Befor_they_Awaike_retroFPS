using UnityEngine;

[RequireComponent(typeof(Light))]
public class PixelatedShadows : MonoBehaviour
{
    public int shadowResolution = 128; // ���������� �����

    private Light _light;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _light.shadows = LightShadows.Hard; // ����������� ������� ���� ��� ����������� �������
    }

    private void Start()
    {
        // ���������� ���������� �����
        if (_light.type == LightType.Directional)
        {
            QualitySettings.shadowResolution = (ShadowResolution)shadowResolution;
        }
    }
}
