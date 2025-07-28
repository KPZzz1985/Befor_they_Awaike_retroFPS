using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PS1JitterMotionController : MonoBehaviour
{
    [Header("—сылка на материал с шейдером PSOneShaderGI_JitterWithTextureFrameRate")]
    public Material ps1JitterMaterial;

    [Header("ѕредустановленные значени€ эффекта при движении")]
    // «начени€, которые должны примен€тьс€, когда камера двигаетс€ или вращаетс€
    public float presetTextureFPS = 10f;
    public float presetJitterAmount = 1f;
    public float presetTextureJitterAmplitude = 0.01f;

    [Header("ѕороговые значени€ дл€ определени€ движени€/вращени€")]
    // ѕорог изменени€ позиции (в мировых единицах) за один кадр, выше которого считаем, что есть движение
    public float movementThreshold = 0.001f;
    // ѕорог изменени€ угла (в градусах) за один кадр, выше которого считаем, что камера вращаетс€
    public float rotationThreshold = 0.1f;

    // ƒл€ отслеживани€ предыдущего состо€ни€ камеры
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    void Update()
    {
        // ¬ычисл€ем, насколько изменилась позици€ и вращение камеры за последний кадр
        float movementDelta = Vector3.Distance(transform.position, lastPosition);
        float rotationDelta = Quaternion.Angle(transform.rotation, lastRotation);

        // ≈сли камера движетс€ или вращаетс€ (превышен хот€ бы один из порогов),
        // выставл€ем предустановленные значени€ дл€ эффекта.
        if (movementDelta > movementThreshold || rotationDelta > rotationThreshold)
        {
            ps1JitterMaterial.SetFloat("_TextureFPS", presetTextureFPS);
            ps1JitterMaterial.SetFloat("_JitterAmount", presetJitterAmount);
            ps1JitterMaterial.SetFloat("_TextureJitterAmplitude", presetTextureJitterAmplitude);
        }
        else
        {
            //  амера неподвижна Ц отключаем текстурный jitter,
            // выставл€€ _TextureFPS и _TextureJitterAmplitude в 0.
            ps1JitterMaterial.SetFloat("_TextureFPS", 0f);
            ps1JitterMaterial.SetFloat("_TextureJitterAmplitude", 0f);
            // _JitterAmount можно оставить неизменным, если его хотите использовать только дл€ геометрического эффекта,
            // либо тоже обнулить, если требуетс€ полностью отключить эффект.
            ps1JitterMaterial.SetFloat("_JitterAmount", presetJitterAmount);
        }

        // ќбновл€ем предыдущие значени€ позиции и вращени€ дл€ следующего кадра
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }
}
