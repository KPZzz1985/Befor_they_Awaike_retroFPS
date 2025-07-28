using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SimpleFogEffect : MonoBehaviour
{
    public Material fogMaterial;
    public Color initialFogColor = new Color(0.5f, 0.5f, 0.5f, 1); // ������� ��������� ���� ������ �����

    private void OnEnable()
    {
        // ��� ��������� ���������� ������������� ��������� ���� ������
        if (fogMaterial != null)
        {
            fogMaterial.SetColor("_FogColor", initialFogColor);
        }
    }

    private void OnDisable()
    {
        // ��� ����������� ���������� ����� ������������ ��������� ���� ������, ���� �����
        // ��� �������, ���� �� ������ �������� ��������� ��������� ��� ��������� ���� � ���������
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (fogMaterial != null)
        {
            Graphics.Blit(source, destination, fogMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
