using UnityEngine;
using UnityEngine.UI;

public class WeaponSwitch : MonoBehaviour
{
    public Image[] weapons;  // массив изображений оружия

    public int currentWeaponIndex = 0; // индекс текущего оружия

    void Start()
    {
        UpdateUI(); // обновляем отображение оружия
    }

    void Update()
    {
        // меняем оружие с помощью колесика мыши
        float mouseScrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (mouseScrollDelta > 0f)
        {
            currentWeaponIndex--;
            if (currentWeaponIndex < 0)
            {
                currentWeaponIndex = weapons.Length - 1;
            }
            UpdateUI();
        }
        else if (mouseScrollDelta < 0f)
        {
            currentWeaponIndex++;
            if (currentWeaponIndex >= weapons.Length)
            {
                currentWeaponIndex = 0;
            }
            UpdateUI();
        }

        // меняем оружие с помощью клавиш на клавиатуре
        for (int i = 0; i < weapons.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                currentWeaponIndex = i;
                UpdateUI();
                break;
            }
        }
    }

    void UpdateUI()
    {
        // отображаем текущее оружие
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(i == currentWeaponIndex);
        }
    }
}