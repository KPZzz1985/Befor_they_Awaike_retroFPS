using UnityEngine;

public class KeyInventory : MonoBehaviour
{
    public int KeyCount { get; private set; }

    public void AddKey(int amount)
    {
        KeyCount += amount;
        // TODO: handle UI or gameplay logic for keys
    }
}