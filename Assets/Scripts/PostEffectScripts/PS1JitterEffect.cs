using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PS1JitterEffect : MonoBehaviour
{
    [Header("Jitter Settings")]
    [Tooltip("Материал, использующий шейдер PS1JitterPostProcess")]
    public Material jitterMaterial;
    [Tooltip("Интенсивность сдвига")]
    public float jitterIntensity = 1.0f;
    [Tooltip("Размер сетки квантования (чем больше, тем мельче шаг)")]
    public float jitterGrid = 50.0f;

    
    
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (jitterMaterial != null)
        {
            
            jitterMaterial.SetFloat("_JitterIntensity", jitterIntensity);
            jitterMaterial.SetFloat("_JitterGrid", jitterGrid);

            
            Vector3 camEuler = cam.transform.eulerAngles;
            float offsetX = Mathf.Sin(camEuler.y * Mathf.Deg2Rad);
            float offsetY = Mathf.Sin(camEuler.x * Mathf.Deg2Rad);
            
            Vector4 camOffset = new Vector4(offsetX, offsetY, 0, 0);
            jitterMaterial.SetVector("_CameraJitterOffset", camOffset);

            
            Graphics.Blit(source, destination, jitterMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
