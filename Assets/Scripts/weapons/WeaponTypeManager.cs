using UnityEngine;

public class WeaponTypeManager : MonoBehaviour
{
    public string weaponTagName = "Weapon_type";
    public string currentWeapon = "";

    void Update()
    {
        // Найти объект с тэгом Weapon_type
        GameObject weaponObject = GameObject.FindGameObjectWithTag(weaponTagName);

        // Если объект найден, получить компонент WeaponSwitch
        if (weaponObject != null)
        {
            WeaponSwitch weaponSwitch = weaponObject.GetComponent<WeaponSwitch>();

            // Получить текущее оружие из WeaponSwitch
            if (weaponSwitch != null)
            {
                currentWeapon = weaponSwitch.weapons[weaponSwitch.currentWeaponIndex].name;
            }
        }
    }
}