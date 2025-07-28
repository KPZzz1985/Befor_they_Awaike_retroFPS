using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PS1JitterMotionController : MonoBehaviour
{
    [Header("������ �� �������� � �������� PSOneShaderGI_JitterWithTextureFrameRate")]
    public Material ps1JitterMaterial;

    [Header("����������������� �������� ������� ��� ��������")]
    // ��������, ������� ������ �����������, ����� ������ ��������� ��� ���������
    public float presetTextureFPS = 10f;
    public float presetJitterAmount = 1f;
    public float presetTextureJitterAmplitude = 0.01f;

    [Header("��������� �������� ��� ����������� ��������/��������")]
    // ����� ��������� ������� (� ������� ��������) �� ���� ����, ���� �������� �������, ��� ���� ��������
    public float movementThreshold = 0.001f;
    // ����� ��������� ���� (� ��������) �� ���� ����, ���� �������� �������, ��� ������ ���������
    public float rotationThreshold = 0.1f;

    // ��� ������������ ����������� ��������� ������
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    void Update()
    {
        // ���������, ��������� ���������� ������� � �������� ������ �� ��������� ����
        float movementDelta = Vector3.Distance(transform.position, lastPosition);
        float rotationDelta = Quaternion.Angle(transform.rotation, lastRotation);

        // ���� ������ �������� ��� ��������� (�������� ���� �� ���� �� �������),
        // ���������� ����������������� �������� ��� �������.
        if (movementDelta > movementThreshold || rotationDelta > rotationThreshold)
        {
            ps1JitterMaterial.SetFloat("_TextureFPS", presetTextureFPS);
            ps1JitterMaterial.SetFloat("_JitterAmount", presetJitterAmount);
            ps1JitterMaterial.SetFloat("_TextureJitterAmplitude", presetTextureJitterAmplitude);
        }
        else
        {
            // ������ ���������� � ��������� ���������� jitter,
            // ��������� _TextureFPS � _TextureJitterAmplitude � 0.
            ps1JitterMaterial.SetFloat("_TextureFPS", 0f);
            ps1JitterMaterial.SetFloat("_TextureJitterAmplitude", 0f);
            // _JitterAmount ����� �������� ����������, ���� ��� ������ ������������ ������ ��� ��������������� �������,
            // ���� ���� ��������, ���� ��������� ��������� ��������� ������.
            ps1JitterMaterial.SetFloat("_JitterAmount", presetJitterAmount);
        }

        // ��������� ���������� �������� ������� � �������� ��� ���������� �����
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }
}
