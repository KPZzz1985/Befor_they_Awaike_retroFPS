using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    [SerializeField]
    private float destroyTime = 0.25f; 

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}
