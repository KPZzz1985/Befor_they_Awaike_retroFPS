using UnityEngine;
using UnityEngine.UI;

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

    private void Awake()
    {
        if (inventory == null)
            inventory = FindObjectOfType<PlayerInventory>();
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
}