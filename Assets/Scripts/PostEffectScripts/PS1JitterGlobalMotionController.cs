using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PS1JitterGlobalMotionController : MonoBehaviour
{
    [Header("��������� ������� ��� �������� (����� ������ ���������/���������)")]
    public float presetTextureFPS = 10f;
    public float presetJitterAmount = 1f;
    public float presetTextureJitterAmplitude = 0.01f;

    [Header("��������� �������� ��� ����������� ��������/��������")]
    // ���� ��������� ������� �� ���� ������ ����� ������ � �������, ��� ���� ��������
    public float movementThreshold = 0.001f;
    // ���� ��������� ���� �� ���� ������ ����� ������ � �������, ��� ������ ���������
    public float rotationThreshold = 0.1f;

    // ��� ������������ ����������� ��������� ������
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    // ������ ����������, ������������ ������ PSOneShaderGI_JitterWithTextureFrameRate
    private Material[] targetMaterials;

    // ��������� �������� ����������, ������� ������ ����� SetPSOneShaderParametersWindow
    private float defaultTextureFPS;
    private float defaultJitterAmount;
    private float defaultTextureJitterAmplitude;

    void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;

        // ������� ��� ��������� � �����
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        List<Material> mats = new List<Material>();

        foreach (Renderer rend in renderers)
        {
            // ���������� sharedMaterials, ����� �������� ���������, ����������� ��������
            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat != null && mat.shader != null)
                {
                    // ���� ��� ������� ������������� ������� (���������, ��� ��� ������� �����)
                    if (mat.shader.name == "Custom/PSOneShaderGI_JitterWithTextureFrameRate")
                    {
                        if (!mats.Contains(mat))
                            mats.Add(mat);
                    }
                }
            }
        }
        targetMaterials = mats.ToArray();
        Debug.Log($"������� {targetMaterials.Length} ���������� � �������� PSOneShaderGI_JitterWithTextureFrameRate.");

        // ��������� ��������� �������� �� ������� ���������� ��������� (��������������, ��� ��� ��� ������ ���������)
        if (targetMaterials.Length > 0)
        {
            defaultTextureFPS = targetMaterials[0].GetFloat("_TextureFPS");
            defaultJitterAmount = targetMaterials[0].GetFloat("_JitterAmount");
            defaultTextureJitterAmplitude = targetMaterials[0].GetFloat("_TextureJitterAmplitude");
        }
    }

    void Update()
    {
        // ���������, ��������� ���������� ������� � �������� ������ �� ����
        float movementDelta = Vector3.Distance(transform.position, lastPosition);
        float rotationDelta = Quaternion.Angle(transform.rotation, lastRotation);

        float textureFPS, jitterAmount, textureJitterAmplitude;

        // ���� ������ �������� ��� ��������� � ��������� preset-��������
        if (movementDelta > movementThreshold || rotationDelta > rotationThreshold)
        {
            textureFPS = presetTextureFPS;
            jitterAmount = presetJitterAmount;
            textureJitterAmplitude = presetTextureJitterAmplitude;
        }
        else
        {
            // ���� ������ ���������� � ��������������� ��������� ��������,
            // ������������� ����� ���� ��������, � �� �������� �� ������
            textureFPS = defaultTextureFPS;
            jitterAmount = defaultJitterAmount;
            textureJitterAmplitude = defaultTextureJitterAmplitude;
        }

        // ��������� ��������� �� ���� ��������� ����������
        foreach (Material mat in targetMaterials)
        {
            if (mat != null)
            {
                mat.SetFloat("_TextureFPS", textureFPS);
                mat.SetFloat("_JitterAmount", jitterAmount);
                mat.SetFloat("_TextureJitterAmplitude", textureJitterAmplitude);
            }
        }

        // ��������� ���������� �������� ��� ���������� �����
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }
}
