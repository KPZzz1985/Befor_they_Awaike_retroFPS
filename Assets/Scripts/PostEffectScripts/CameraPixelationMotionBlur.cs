using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraPixelationMotionBlur : MonoBehaviour
{
    public int pixelSize = 64;
    private Camera cam;
    private RenderTexture renderTexture;

    public Shader motionBlurShader;
    public float blend = 0.5f;
    
    private Material motionBlurMaterial;
    private RenderTexture prevFrame;

    // Добавляем ссылку на контроллер персонажа
    public FirstPersonController_CC playerController;

    public int dashBlurFactor = 2; // Степень размытия при dash
    public float dashDuration = 1.0f; // Продолжительность эффекта dash в секундах
    public float dashFadeTime = 0.5f; // Время затухания эффекта dash в секундах

    private float dashStartTime; // Время активации dash

    public PlayerHealth playerHealth; // ссылка на скрипт PlayerHealth
    private float lastHealth; // для отслеживания изменения здоровья
    public float damageBlurIntensity = 2; // интенсивность размытия при получении урона
    public float deathBlurIntensity = 5; // интенсивность размытия при смерти

    private void Awake()
  {
    cam = GetComponent<Camera>();
    
    if (motionBlurShader != null)
    {
        motionBlurMaterial = new Material(motionBlurShader);
    }

    // Получаем ссылку на скрипт контроллера персонажа
    playerController = FindObjectOfType<FirstPersonController_CC>();
    
    // Получаем ссылку на скрипт PlayerHealth
    playerHealth = FindObjectOfType<PlayerHealth>();
    
    if(playerHealth != null)
    {
        lastHealth = playerHealth.CurrentHealth;
    }
  }

   private void OnRenderImage(RenderTexture source, RenderTexture destination)
{
    int tempPixelSize = pixelSize;

    // Проверяем изменение здоровья
    if(playerHealth.CurrentHealth < lastHealth)
    {
        // Например, увеличиваем размытие, когда игрок получает урон
        float damageTakenFactor = Mathf.Clamp((lastHealth - playerHealth.CurrentHealth) / 100f, 0f, 1f);
        tempPixelSize = (int)Mathf.Lerp(pixelSize, pixelSize * (1 + damageTakenFactor * 5), 0.5f);
        lastHealth = playerHealth.CurrentHealth;
    }

    if (motionBlurShader != null)
    {
        if (prevFrame == null)
        {
            prevFrame = new RenderTexture(source.width, source.height, 0);
            Graphics.Blit(source, prevFrame);
        }

        if (playerController.isDashing)
        {
            motionBlurMaterial.SetTexture("_PrevTex", prevFrame);
            tempPixelSize *= dashBlurFactor;
            dashStartTime = Time.time;
            motionBlurMaterial.SetFloat("_Blend", blend);
            Graphics.Blit(source, prevFrame, motionBlurMaterial);
        }
        else
        {
            float timeSinceDash = Time.time - dashStartTime;
            if(timeSinceDash < dashFadeTime)
            {
                float fadePercent = timeSinceDash / dashFadeTime;
                tempPixelSize = (int)Mathf.Lerp(tempPixelSize * dashBlurFactor, pixelSize, fadePercent);
            }
            Graphics.Blit(source, prevFrame);
        }
    }
    else
    {
        Graphics.Blit(source, prevFrame);
    }

    if(playerHealth.CurrentHealth <= 0)
    {
        tempPixelSize *= 5; // или другой фактор, который вы хотите установить для сильного размытия при смерти
    }

    if (tempPixelSize <= 1)
    {
        Graphics.Blit(source, destination);
        return;
    }

    if (renderTexture == null || renderTexture.width != cam.pixelWidth / tempPixelSize || renderTexture.height != cam.pixelHeight / tempPixelSize)
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }

        renderTexture = new RenderTexture(cam.pixelWidth / tempPixelSize, cam.pixelHeight / tempPixelSize, 0, RenderTextureFormat.ARGB32);
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
    }

    Graphics.Blit(prevFrame, renderTexture);
    Graphics.Blit(renderTexture, destination);
}




    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
        }

        if (prevFrame != null)
        {
            Destroy(prevFrame);
        }

        if (motionBlurMaterial != null)
        {
            Destroy(motionBlurMaterial);
        }
    }
}
