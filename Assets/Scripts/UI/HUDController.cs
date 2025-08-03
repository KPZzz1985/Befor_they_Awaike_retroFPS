using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class HUDController : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public WeaponSwitcher weaponSwitcher;
    public PlayerInventory inventory;

    [Header("Digit Sprites (0-9)")]
    public Sprite[] digitSprites;

    [Header("Health Display")]
    public Image[] healthDigits;

    [Header("Clip & Reserve Display")]
    public Image[] clipDigits;
    public Image slashImage;
    public Image[] reserveDigits;
    public Image ammoIcon; // Still show ammo icon for clip
    public Image shieldPlaceholder;
    
    [Header("Damage & Death Overlays")]
    [Tooltip("Красная плашка для вспышки при получении урона")]
    public Image damageOverlay;
    [Tooltip("Черная плашка для полного затемнения при смерти")]
    public Image deathOverlay;
    [Tooltip("Время вспышки урона в миллисекундах")]
    public int damageFlashDuration = 200;
    [Tooltip("Максимальная альфа для вспышки урона (0-1)")]
    public float maxDamageAlpha = 0.3f;
    [Tooltip("Время затемнения смерти в миллисекундах")]
    public int deathFadeDuration = 2000;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindObjectOfType<PlayerInventory>();
            
        // Инициализируем плашки урона и смерти
        InitializeOverlays();
    }

    private void Update()
    {
        UpdateHealthDisplay();
        UpdateAmmoDisplay();
    }

    private void UpdateHealthDisplay()
    {
        int health = playerHealth.CurrentHealth;
        DisplayNumber(health, healthDigits);
    }

    private void UpdateAmmoDisplay()
    {
        PM_Shooting ps = null;
        foreach (var w in weaponSwitcher.weapons)
        {
            if (w.activeSelf)
            {
                // include inactive to find PM_Shooting in child hierarchy
                ps = w.GetComponentInChildren<PM_Shooting>(true);
                break;
            }
        }

        if (ps != null)
        {
            int currentClip = ps.CurrentAmmo;
            int reserve = inventory != null ? inventory.GetReserveAmmo(ps.WeaponIndex) : 0;

            // Update clip icon
            if (ammoIcon != null)
            {
                ammoIcon.enabled = true;
                ammoIcon.sprite = ps.ammoIcon;
            }

            // Display clip and reserve
            DisplayNumber(currentClip, clipDigits);
            DisplayNumber(reserve, reserveDigits);
            if (slashImage != null)
                slashImage.enabled = true;
        }
        else
        {
            // disable when no weapon
            if (ammoIcon != null) ammoIcon.enabled = false;
            if (slashImage != null) slashImage.enabled = false;
            foreach (var img in clipDigits) img.enabled = false;
            foreach (var img in reserveDigits) img.enabled = false;
        }
    }

    private void DisplayNumber(int number, Image[] digitImages)
    {
        string s = number.ToString();
        for (int i = digitImages.Length - 1, j = s.Length - 1; i >= 0; i--, j--)
        {
            if (j >= 0)
            {
                int d = s[j] - '0';
                digitImages[i].sprite = digitSprites[d];
                digitImages[i].enabled = true;
            }
            else
            {
                digitImages[i].enabled = false;
            }
        }
    }
    
    /// <summary>
    /// Инициализация плашек урона и смерти
    /// </summary>
    private void InitializeOverlays()
    {
        if (damageOverlay != null)
        {
            // Красная плашка для урона, изначально прозрачная
            damageOverlay.color = new Color(1f, 0f, 0f, 0f);
            damageOverlay.enabled = true;
        }
        
        if (deathOverlay != null)
        {
            // Черная плашка для смерти, изначально прозрачная
            deathOverlay.color = new Color(0f, 0f, 0f, 0f);
            deathOverlay.enabled = true;
        }
    }
    
    /// <summary>
    /// Вспышка красной плашки при получении урона
    /// </summary>
    public async UniTask ShowDamageFlash()
    {
        if (damageOverlay == null) return;
        
        // Плавное появление
        float halfDuration = damageFlashDuration * 0.5f;
        
        // Fade in до максимальной альфы
        await FadeOverlay(damageOverlay, 0f, maxDamageAlpha, (int)halfDuration);
        
        // Fade out обратно к прозрачности
        await FadeOverlay(damageOverlay, maxDamageAlpha, 0f, (int)halfDuration);
    }
    
    /// <summary>
    /// Полное затемнение экрана при смерти
    /// </summary>
    public async UniTask ShowDeathOverlay()
    {
        if (deathOverlay == null) return;
        
        // Плавное затемнение до полной черноты
        await FadeOverlay(deathOverlay, 0f, 1f, deathFadeDuration);
        
        Debug.Log("Смерть игрока: экран полностью затемнен");
    }
    
    /// <summary>
    /// Плавное изменение альфы плашки
    /// </summary>
    private async UniTask FadeOverlay(Image overlay, float fromAlpha, float toAlpha, int durationMs)
    {
        if (overlay == null) return;
        
        Color startColor = overlay.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, toAlpha);
        startColor.a = fromAlpha;
        
        float elapsedTime = 0f;
        float duration = durationMs / 1000f; // Конвертируем в секунды
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            
            overlay.color = Color.Lerp(startColor, endColor, progress);
            
            await UniTask.Yield(); // Ждем следующий кадр
        }
        
        // Устанавливаем финальный цвет
        overlay.color = endColor;
    }
}