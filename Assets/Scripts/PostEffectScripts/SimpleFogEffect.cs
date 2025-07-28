using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class SimpleFogEffect : MonoBehaviour
{
    public Material fogMaterial;
    public Color initialFogColor = new Color(0.5f, 0.5f, 0.5f, 1); // Задайте начальный цвет тумана здесь

    private void OnEnable()
    {
        // При активации компонента устанавливаем начальный цвет тумана
        if (fogMaterial != null)
        {
            fogMaterial.SetColor("_FogColor", initialFogColor);
        }
    }

    private void OnDisable()
    {
        // При деактивации компонента можно восстановить начальный цвет тумана, если нужно
        // Это полезно, если вы хотите сбросить состояние материала при остановке игры в редакторе
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
