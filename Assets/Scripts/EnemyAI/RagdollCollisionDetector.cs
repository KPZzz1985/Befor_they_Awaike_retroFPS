using UnityEngine;
using System;

public class RagdollCollisionDetector : MonoBehaviour
{
    public Action<Vector3> OnCollisionDetected;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("LevelObjects") && OnCollisionDetected != null)
        {
            // �������� ����� �������� ������� ������������
            OnCollisionDetected.Invoke(collision.contacts[0].point);
        }
    }
}

