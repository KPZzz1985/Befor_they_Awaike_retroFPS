using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AmmoEntry
{
    [Tooltip("PM_Shooting component of the weapon (drag weapon GameObject here)")]
    public PM_Shooting shooter;

    [Tooltip("Current ammo in clip")] public int currentAmmo;
    [Tooltip("Max ammo in clip")] public int maxAmmo;
    [Tooltip("Reserve ammo available for reloads")] public int reserveAmmo;
    [Tooltip("Maximum reserve ammo allowed")] public int reserveLimit;

    /// <summary>
    /// Gets the weapon index in WeaponSwitcher (from shooter component)
    /// </summary>
    public int WeaponIndex => shooter != null ? shooter.WeaponIndex : -1;
}

public class PlayerInventory : MonoBehaviour
{
    [Tooltip("Reference to the WeaponSwitcher (needed for some HUD setups)")]
    public WeaponSwitcher weaponSwitcher;

    [Header("Ammo Entries (manually assign for each weapon)")]
    public List<AmmoEntry> ammoEntries;

    public int CoreCount { get; private set; }
    public int KeyCount { get; private set; }

    private void Awake()
    {
        // Auto-populate clip values and reserveLimit from shooter
        foreach (var entry in ammoEntries)
        {
            if (entry.shooter != null)
            {
                entry.maxAmmo = entry.shooter.MaxAmmo;
                entry.currentAmmo = entry.shooter.CurrentAmmo;
                if (entry.reserveLimit <= 0)
                    entry.reserveLimit = entry.maxAmmo;
            }
        }
    }

    /// <summary>
    /// Adds ammo into clip for the specified weapon.
    /// </summary>
    public void AddAmmo(int weaponIndex, int amount)
    {
        var entry = ammoEntries.Find(e => e.WeaponIndex == weaponIndex);
        if (entry != null)
            entry.currentAmmo = Mathf.Clamp(entry.currentAmmo + amount, 0, entry.maxAmmo);
    }

    /// <summary>
    /// Gets current clip ammo for the specified weapon.
    /// </summary>
    public int GetCurrentAmmo(int weaponIndex)
    {
        var entry = ammoEntries.Find(e => e.WeaponIndex == weaponIndex);
        return entry != null ? entry.currentAmmo : 0;
    }

    /// <summary>
    /// Gets max clip ammo for the specified weapon.
    /// </summary>
    public int GetMaxAmmo(int weaponIndex)
    {
        var entry = ammoEntries.Find(e => e.WeaponIndex == weaponIndex);
        return entry != null ? entry.maxAmmo : 0;
    }

    /// <summary>
    /// Adds ammo into reserve for the specified weapon.
    /// </summary>
    public void AddReserve(int weaponIndex, int amount)
    {
        var entry = ammoEntries.Find(e => e.WeaponIndex == weaponIndex);
        if (entry != null)
            entry.reserveAmmo = Mathf.Clamp(entry.reserveAmmo + amount, 0, entry.reserveLimit);
    }

    /// <summary>
    /// Removes ammo from reserve, returns actual removed amount.
    /// </summary>
    public int RemoveReserve(int weaponIndex, int amount)
    {
        var entry = ammoEntries.Find(e => e.WeaponIndex == weaponIndex);
        if (entry == null) return 0;
        int removed = Mathf.Min(amount, entry.reserveAmmo);
        entry.reserveAmmo -= removed;
        return removed;
    }

    /// <summary>
    /// Gets reserve ammo available for reloads.
    /// </summary>
    public int GetReserveAmmo(int weaponIndex)
    {
        var entry = ammoEntries.Find(e => e.WeaponIndex == weaponIndex);
        return entry != null ? entry.reserveAmmo : 0;
    }

    /// <summary>
    /// Adds upgrade cores to inventory.
    /// </summary>
    public void AddCore(int amount)
    {
        CoreCount += amount;
    }

    /// <summary>
    /// Adds keys to inventory.
    /// </summary>
    public void AddKey(int amount)
    {
        KeyCount += amount;
    }
}