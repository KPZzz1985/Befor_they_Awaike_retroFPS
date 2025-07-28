using UnityEngine;
using UnityEngine.UI;

public class WeaponSwitch : MonoBehaviour
{
    public Image[] weapons;  // ������ ����������� ������

    public int currentWeaponIndex = 0; // ������ �������� ������

    void Start()
    {
        UpdateUI(); // ��������� ����������� ������
    }

    void Update()
    {
        // ������ ������ � ������� �������� ����
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

        // ������ ������ � ������� ������ �� ����������
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
        // ���������� ������� ������
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(i == currentWeaponIndex);
        }
    }
}