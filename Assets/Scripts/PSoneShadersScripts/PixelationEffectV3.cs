using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PixelationEffectV3 : MonoBehaviour
{
    public int pixelationScale = 64;
    public float sharpness = 0.5f;
    private RenderTexture _renderTexture;
    private Material _sharpenMaterial;

    private void Start()
    {
        int width = Mathf.FloorToInt(Screen.width / pixelationScale);
        int height = Mathf.FloorToInt(Screen.height / pixelationScale);

        _renderTexture = new RenderTexture(width, height, 24);
        _renderTexture.filterMode = FilterMode.Point;

        _sharpenMaterial = new Material(Shader.Find("Hidden/Sharpen"));
        _sharpenMaterial.SetFloat("_Sharpness", sharpness);
    }

    private void OnDisable()
    {
        if (_renderTexture != null)
        {
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        if (_sharpenMaterial != null)
        {
            Destroy(_sharpenMaterial);
            _sharpenMaterial = null;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_renderTexture != null && _sharpenMaterial != null)
        {
            Graphics.Blit(source, _renderTexture);
            _sharpenMaterial.SetTexture("_MainTex", _renderTexture);
            Graphics.Blit(_renderTexture, destination, _sharpenMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}