using UnityEngine;
using UnityEditor;

public class SetPSOneShaderParametersWindow : EditorWindow
{
    // Ключи для сохранения параметров в EditorPrefs
    private const string TilingKey = "PSOneShader_Tiling";
    private const string PixelationAmountKey = "PSOneShader_PixelationAmount";
    private const string ColorPrecisionKey = "PSOneShader_ColorPrecision";
    private const string JitterAmountKey = "PSOneShader_JitterAmount";
    private const string TextureFPSKey = "PSOneShader_TextureFPS";
    private const string TextureJitterAmplitudeKey = "PSOneShader_TextureJitterAmplitude";

    // Значения по умолчанию (если параметров ещё не было сохранено)
    private float newTiling = 64f;
    private float newPixelationAmount = 64f;
    private float newColorPrecision = 32f;
    private float newJitterAmount = 1f;
    private float newTextureFPS = 10f;
    private float newTextureJitterAmplitude = 0.01f;

    // Добавляем пункт меню для вызова окна
    [MenuItem("Tools/Set PSOneShaderGI Parameters for Materials")]
    public static void ShowWindow()
    {
        GetWindow<SetPSOneShaderParametersWindow>("Set PSOneShaderGI Params");
    }

    // При открытии окна считываем сохранённые значения
    private void OnEnable()
    {
        newTiling = EditorPrefs.GetFloat(TilingKey, 64f);
        newPixelationAmount = EditorPrefs.GetFloat(PixelationAmountKey, 64f);
        newColorPrecision = EditorPrefs.GetFloat(ColorPrecisionKey, 32f);
        newJitterAmount = EditorPrefs.GetFloat(JitterAmountKey, 1f);
        newTextureFPS = EditorPrefs.GetFloat(TextureFPSKey, 10f);
        newTextureJitterAmplitude = EditorPrefs.GetFloat(TextureJitterAmplitudeKey, 0.01f);
    }

    // При закрытии окна записываем текущие значения
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
        GUILayout.Label("Установка параметров PSOneShaderGI_JitterWithTextureFrameRate", EditorStyles.boldLabel);

        // Отображаем поля для редактирования параметров
        newTiling = EditorGUILayout.FloatField("_Tiling", newTiling);
        newPixelationAmount = EditorGUILayout.FloatField("_PixelationAmount", newPixelationAmount);
        newColorPrecision = EditorGUILayout.FloatField("_ColorPrecision", newColorPrecision);
        newJitterAmount = EditorGUILayout.FloatField("_JitterAmount", newJitterAmount);
        newTextureFPS = EditorGUILayout.FloatField("_TextureFPS", newTextureFPS);
        newTextureJitterAmplitude = EditorGUILayout.FloatField("_TextureJitterAmplitude", newTextureJitterAmplitude);

        GUILayout.Space(10);
        if (GUILayout.Button("Применить"))
        {
            ApplySettingsToAllMaterials();
        }
    }

    private void ApplySettingsToAllMaterials()
    {
        // Находим все материалы в проекте
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            // Если материал существует, проверяем наличие нужных свойств
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

                // Если хотя бы одно свойство было обновлено, помечаем материал как изменённый
                if (updated)
                {
                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }
        }

        Debug.Log($"Обновлено {count} материалов с параметрами PSOneShaderGI_JitterWithTextureFrameRate.");
    }
}
