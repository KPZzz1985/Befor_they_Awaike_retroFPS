using UnityEngine;

public class UpgradeCoreInventory : MonoBehaviour
{
    public int CoreCount { get; private set; }

    public void AddCore(int amount)
    {
        CoreCount += amount;
        // TODO: handle UI or gameplay logic for cores
    }
}