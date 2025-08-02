using UnityEngine;

public class PlayerShield : MonoBehaviour
{
    public int MaxShield = 100;
    private int currentShield;
    public int CurrentShield => currentShield;

    private void Start()
    {
        currentShield = MaxShield;
    }

    public void AddShield(int amount)
    {
        currentShield = Mathf.Clamp(currentShield + amount, 0, MaxShield);
        // TODO: update shield UI if needed
    }

    public void RemoveShield(int amount)
    {
        currentShield = Mathf.Clamp(currentShield - amount, 0, MaxShield);
        // TODO: update shield UI if needed
    }
}