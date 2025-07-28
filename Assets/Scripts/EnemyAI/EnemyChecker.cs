using System.Collections.Generic;
using UnityEngine;

public class EnemyChecker : MonoBehaviour
{
    [System.Serializable]
    public class EnemyWeaponInfo
    {
        public GameObject enemyObject;
        public string weaponName;
    }

    public List<EnemyWeaponInfo> enemyWeapons = new List<EnemyWeaponInfo>();

    void Update()
    {
        // Clear the previous frame's data
        enemyWeapons.Clear();

        // Find all enemies in the scene
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        // Iterate over each enemy
        foreach (var enemy in enemies)
        {
            // Get the EnemyWeapon component
            EnemyWeapon enemyWeapon = enemy.GetComponent<EnemyWeapon>();

            // If the object has an EnemyWeapon component
            if (enemyWeapon != null)
            {
                // Add the enemy's GameObject and current weapon name to the list
                EnemyWeaponInfo info = new EnemyWeaponInfo();
                info.enemyObject = enemy;
                info.weaponName = enemyWeapon.currentWeaponPrefabName;
                enemyWeapons.Add(info);
            }
        }
    }

    public string GetEnemyWeaponName(GameObject enemy)
    {
        foreach (EnemyWeaponInfo info in enemyWeapons)
        {
            if (info.enemyObject == enemy)
            {
                return info.weaponName;
            }
        }

        return null;
    }
}
