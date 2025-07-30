using UnityEngine;
using UnityEditor;

public class SetPSOneShaderParametersWindow : EditorWindow
{
    // ����� ��� ���������� ���������� � EditorPrefs
    private const string TilingKey = "PSOneShader_Tiling";
    private const string PixelationAmountKey = "PSOneShader_PixelationAmount";
    private const string ColorPrecisionKey = "PSOneShader_ColorPrecision";
    private const string JitterAmountKey = "PSOneShader_JitterAmount";
    private const string TextureFPSKey = "PSOneShader_TextureFPS";
    private const string TextureJitterAmplitudeKey = "PSOneShader_TextureJitterAmplitude";

    // �������� �� ��������� (���� ���������� ��� �� ���� ���������)
    private float newTiling = 64f;
    private float newPixelationAmount = 64f;
    private float newColorPrecision = 32f;
    private float newJitterAmount = 1f;
    private float newTextureFPS = 10f;
    private float newTextureJitterAmplitude = 0.01f;

    // ��������� ����� ���� ��� ������ ����
    [MenuItem("Tools/Set PSOneShaderGI Parameters for Materials")]
    public static void ShowWindow()
    {
        GetWindow<SetPSOneShaderParametersWindow>("Set PSOneShaderGI Params");
    }

    // ��� �������� ���� ��������� ����������� ��������
    private void OnEnable()
    {
        newTiling = EditorPrefs.GetFloat(TilingKey, 64f);
        newPixelationAmount = EditorPrefs.GetFloat(PixelationAmountKey, 64f);
        newColorPrecision = EditorPrefs.GetFloat(ColorPrecisionKey, 32f);
        newJitterAmount = EditorPrefs.GetFloat(JitterAmountKey, 1f);
        newTextureFPS = EditorPrefs.GetFloat(TextureFPSKey, 10f);
        newTextureJitterAmplitude = EditorPrefs.GetFloat(TextureJitterAmplitudeKey, 0.01f);
    }

    // ��� �������� ���� ���������� ������� ��������
    private void OnDisable()
    {
        EditorPrefs.SetFloat(TilingKey, newTiling);
        EditorPrefs.SetFloat(PixelationAmountKey, newPixelationAmount);
        EditorPrefs.SetFloat(ColorPrecisionKey, newColorPrecision);
        EditorPrefs.SetFloat(JitterAmountKey, newJitterAmount);
        EditorPrefs.SetFloat(TextureFPSKey, newTextureFPS);
        EditorPrefs.SetFloat(TextureJitterAmplitudeKey, newTextureJitterAmplitude);
    }

    private void OnGUI()
    {
        GUILayout.Label("��������� ���������� PSOneShaderGI_JitterWithTextureFrameRate", EditorStyles.boldLabel);

        // ���������� ���� ��� �������������� ����������
        newTiling = EditorGUILayout.FloatField("_Tiling", newTiling);
        newPixelationAmount = EditorGUILayout.FloatField("_PixelationAmount", newPixelationAmount);
        newColorPrecision = EditorGUILayout.FloatField("_ColorPrecision", newColorPrecision);
        newJitterAmount = EditorGUILayout.FloatField("_JitterAmount", newJitterAmount);
        newTextureFPS = EditorGUILayout.FloatField("_TextureFPS", newTextureFPS);
        newTextureJitterAmplitude = EditorGUILayout.FloatField("_TextureJitterAmplitude", newTextureJitterAmplitude);

        GUILayout.Space(10);
        if (GUILayout.Button("���������"))
        {
            ApplySettingsToAllMaterials();
        }
    }

    private void ApplySettingsToAllMaterials()
    {
        // ������� ��� ��������� � �������
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            // ���� �������� ����������, ��������� ������� ������ �������
            if (mat != null)
            {
                bool updated = false;

                if (mat.HasProperty("_Tiling"))
                {
                    mat.SetFloat("_Tiling", newTiling);
                    updated = true;
                }
                if (mat.HasProperty("_PixelationAmount"))
                {
                    mat.SetFloat("_PixelationAmount", newPixelationAmount);
                    updated = true;
                }
                if (mat.HasProperty("_ColorPrecision"))
                {
                    mat.SetFloat("_ColorPrecision", newColorPrecision);
                    updated = true;
                }
                if (mat.HasProperty("_JitterAmount"))
                {
                    mat.SetFloat("_JitterAmount", newJitterAmount);
                    updated = true;
                }
                if (mat.HasProperty("_TextureFPS"))
                {
                    mat.SetFloat("_TextureFPS", newTextureFPS);
                    updated = true;
                }
                if (mat.HasProperty("_TextureJitterAmplitude"))
                {
                    mat.SetFloat("_TextureJitterAmplitude", newTextureJitterAmplitude);
                    updated = true;
                }

                // ���� ���� �� ���� �������� ���� ���������, �������� �������� ��� ����������
                if (updated)
                {
                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }
        }

        Debug.Log($"��������� {count} ���������� � ����������� PSOneShaderGI_JitterWithTextureFrameRate.");
    }
}

// Utility for applying PSOneShaderGI params without UI
public static class PSOneShaderGIUtility
{
    private const string TilingKey = "PSOneShader_Tiling";
    private const string PixelationAmountKey = "PSOneShader_PixelationAmount";
    private const string ColorPrecisionKey = "PSOneShader_ColorPrecision";
    private const string JitterAmountKey = "PSOneShader_JitterAmount";
    private const string TextureFPSKey = "PSOneShader_TextureFPS";
    private const string TextureJitterAmplitudeKey = "PSOneShader_TextureJitterAmplitude";

    public static void ApplySettings()
    {
        float tiling = EditorPrefs.GetFloat(TilingKey, 64f);
        float pixelAmount = EditorPrefs.GetFloat(PixelationAmountKey, 64f);
        float colorPrecision = EditorPrefs.GetFloat(ColorPrecisionKey, 32f);
        float jitter = EditorPrefs.GetFloat(JitterAmountKey, 1f);
        float textureFPS = EditorPrefs.GetFloat(TextureFPSKey, 10f);
        float textureJitterAmp = EditorPrefs.GetFloat(TextureJitterAmplitudeKey, 0.01f);

        string[] guids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if (mat.HasProperty("_Tiling")) mat.SetFloat("_Tiling", tiling);
            if (mat.HasProperty("_PixelationAmount")) mat.SetFloat("_PixelationAmount", pixelAmount);
            if (mat.HasProperty("_ColorPrecision")) mat.SetFloat("_ColorPrecision", colorPrecision);
            if (mat.HasProperty("_JitterAmount")) mat.SetFloat("_JitterAmount", jitter);
            if (mat.HasProperty("_TextureFPS")) mat.SetFloat("_TextureFPS", textureFPS);
            if (mat.HasProperty("_TextureJitterAmplitude")) mat.SetFloat("_TextureJitterAmplitude", textureJitterAmp);
            EditorUtility.SetDirty(mat);
        }
        Debug.Log($"PSOneShaderGI params applied to {guids.Length} materials.");
    }
}

// Auto-apply settings before entering Play mode
[InitializeOnLoad]
public static class PSOneShaderGIAutoApply
{
    static PSOneShaderGIAutoApply()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            PSOneShaderGIUtility.ApplySettings();
        }
    }
}
