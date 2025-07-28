using UnityEngine;
using System;

public class RagdollCollisionDetector : MonoBehaviour
{
    public Action<Vector3> OnCollisionDetected;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("LevelObjects") && OnCollisionDetected != null)
        {
            // ѕередаем точку контакта первого столкновени€
            OnCollisionDetected.Invoke(collision.contacts[0].point);
        }
    }
}

