using UnityEngine;

public class PM_animations : MonoBehaviour
{
    [Header("Player Controller")]
    public FirstPersonController_CC playerController;

    [Header("Animator")]
    public Animator animator;

    void Start()
    {
        if (playerController == null)
        {
            Debug.LogWarning("Player Controller is not assigned. PM_animations will not work.");
            enabled = false;
            return;
        }

        if (animator == null)
        {
            Debug.LogWarning("Animator is not assigned. PM_animations will not work.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        UpdateAnimatorParameters();
    }

    private void UpdateAnimatorParameters()
    {
        float horizontal = Mathf.Abs(Input.GetAxis("Horizontal"));
        float vertical = Mathf.Abs(Input.GetAxis("Vertical"));
        float movementAmount = playerController.isGrounded ? Mathf.Clamp01(horizontal + vertical) : 0;

        animator.SetFloat("Movement", movementAmount);
        animator.SetBool("IsGrounded", playerController.isGrounded);

        if (playerController.isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("JumpStart");
        }
    }
}