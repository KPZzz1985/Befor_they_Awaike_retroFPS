using UnityEngine;

public class WeaponTypeManager : MonoBehaviour
{
    public string weaponTagName = "Weapon_type";
    public string currentWeapon = "";

    void Update()
    {
        // ����� ������ � ����� Weapon_type
        GameObject weaponObject = GameObject.FindGameObjectWithTag(weaponTagName);

        // ���� ������ ������, �������� ��������� WeaponSwitch
        if (weaponObject != null)
        {
            WeaponSwitch weaponSwitch = weaponObject.GetComponent<WeaponSwitch>();

            // �������� ������� ������ �� WeaponSwitch
            if (weaponSwitch != null)
            {
                currentWeapon = weaponSwitch.weapons[weaponSwitch.currentWeaponIndex].name;
            }
        }
    }
}