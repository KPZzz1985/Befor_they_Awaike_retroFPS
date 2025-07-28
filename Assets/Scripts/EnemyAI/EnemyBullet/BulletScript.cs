using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 10;
    public float lifeTime = 5f;
    public Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.velocity = transform.forward * speed;
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // ��������� ����� ������
            // ����� ����� ������������ ���� ����� ��� ��������� ����� ������, ���� � ��� ���� �����
        }
        Destroy(gameObject);
    }
}
