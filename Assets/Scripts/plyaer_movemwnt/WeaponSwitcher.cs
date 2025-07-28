using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    public List<GameObject> weapons; // список оружия
    public List<char> weaponKeys; // символы, соответствующие оружию
    public float switchSpeed; // скорость переключения

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
                StartCoroutine(SwitchWeapon(i));
            }
        }
    }

    private void ActivateWeapon(int index)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            weapons[i].SetActive(i == index);
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
