using UnityEngine;

[RequireComponent(typeof(Collider), typeof(Rigidbody))]
public class PickableItem : MonoBehaviour
{
    public enum ItemType { Health, Ammo, Shield, UpgradeCore, Key }
    public ItemType itemType;
    [Tooltip("Amount to apply (health, ammo, shield). Ignored for UpgradeCore and Key.")]
    public int amount = 1;
    [Tooltip("For Ammo: index of weapon in WeaponSwitcher.weapons list. -1 = apply to all weapons.")]
    public int weaponIndex = -1;

    [Header("Audio")]
    [Tooltip("Sound to play when the item is picked up.")]
    public AudioClip pickupSound;
    [Tooltip("Volume of the pickup sound.")]
    [Range(0f,1f)] public float pickupVolume = 1f;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var playerGo = other.gameObject;

        var health = playerGo.GetComponent<PlayerHealth>();
        var switcher = playerGo.GetComponent<WeaponSwitcher>();
        var shield = playerGo.GetComponent<PlayerShield>();
        var inventory = playerGo.GetComponent<PlayerInventory>();

        bool picked = false;
        switch(itemType)
        {
            case ItemType.Health:
                if (health != null && health.CurrentHealth < health.maxHealth)
                {
                    int heal = Mathf.Min(amount, health.maxHealth - health.CurrentHealth);
                    health.Heal(heal);
                    picked = heal > 0;
                }
                break;
            case ItemType.Ammo:
                if (inventory != null)
                {
                    // add to reserve ammo pool
                    if (weaponIndex >= 0)
                    {
                        inventory.AddReserve(weaponIndex, amount);
                        picked = true;
                    }
                    else
                    {
                        // reserve for all weapons
                        foreach (var entry in inventory.ammoEntries)
                            inventory.AddReserve(entry.WeaponIndex, amount);
                        picked = inventory.ammoEntries.Count > 0;
                    }
                }
                break;
            case ItemType.Shield:
                if (shield != null && shield.CurrentShield < shield.MaxShield)
                {
                    int fill = Mathf.Min(amount, shield.MaxShield - shield.CurrentShield);
                    shield.AddShield(fill);
                    picked = fill > 0;
                }
                break;
            case ItemType.UpgradeCore:
                if (inventory != null)
                {
                    inventory.AddCore(amount);
                    picked = true;
                }
                break;
            case ItemType.Key:
                if (inventory != null)
                {
                    inventory.AddKey(amount);
                    picked = true;
                }
                break;
        }

        if (picked)
        {
            // Play pickup sound
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
            Destroy(gameObject);
        }
    }
}