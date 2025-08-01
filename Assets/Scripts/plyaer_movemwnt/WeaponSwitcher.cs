using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    public List<GameObject> weapons; // ������ ������
    public List<char> weaponKeys; // �������, ��������������� ������
    public float switchSpeed; // �������� ������������

    private int currentWeaponIndex;

    private void Start()
    {
        currentWeaponIndex = 0;
        if (weapons.Count > 0)
        {
            ActivateWeapon(currentWeaponIndex);
        }
    }

    private void Update()
    {
        for (int i = 0; i < weaponKeys.Count; i++)
        {
            if (Input.GetKeyDown(weaponKeys[i].ToString()))
            {
                // Do nothing if selecting the same weapon
                if (i == currentWeaponIndex) continue;
                StartCoroutine(SwitchWeapon(i));
            }
        }
    }

    private void ActivateWeapon(int index)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            bool activate = (i == index);
            weapons[i].SetActive(activate);
            if (activate)
            {
                // Reset reload state on newly activated weapon scripts in any children
                var ew = weapons[i].GetComponentInChildren<EnemyWeapon>();
                if (ew != null)
                    ew.ResetReloadState();
                var ps = weapons[i].GetComponentInChildren<PM_Shooting>();
                if (ps != null)
                    ps.ResetReloadState();
            }
        }
    }

    private IEnumerator SwitchWeapon(int newWeaponIndex)
    {
        DeactivateCurrentWeapon();
        yield return new WaitForSeconds(switchSpeed);
        ActivateWeapon(newWeaponIndex);
        SetTriggerForAllWeapons("Weapon Select");
        currentWeaponIndex = newWeaponIndex;
    }

    private void DeactivateCurrentWeapon()
    {
        if (weapons.Count > 0)
        {
            weapons[currentWeaponIndex].SetActive(false);
        }
    }

    private void SetTriggerForAllWeapons(string trigger)
    {
        GameObject[] allWeapons = GameObject.FindGameObjectsWithTag("playerWeapon");
        foreach (GameObject weapon in allWeapons)
        {
            Animator animator = weapon.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(trigger);
            }
        }
    }
}
