using UnityEngine;

public class ShellBehavior : MonoBehaviour
{
    public float disableDelay = 2f; // Настраиваемая задержка

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private bool isGrounded;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") && !isGrounded)
        {
            isGrounded = true;
            Invoke("DisableComponents", disableDelay);
        }
    }

    private void DisableComponents()
    {
        rb.isKinematic = true;
        capsuleCollider.enabled = false;
    }
}