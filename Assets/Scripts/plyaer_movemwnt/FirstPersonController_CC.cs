using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController_CC : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float crouchSpeed = 2f; // Скорость при приседании
    public float jumpForce = 5f;
    public float gravity = -9.81f;
    public float dashDistance = 5f;
    public float dashCooldown = 2f;
    public float dashTime = 0.2f;

    [Header("Crouch Settings")]
    public bool isCrouching = false;
    public Transform cameraRotator;
    public float crouchHeight = 0.5f;
    public float crouchSmoothness = 0.1f;
    public float colliderHeight = 1f; // Высота коллайдера при приседании
    public float dashEndTime;

    [Header("Ground Check Settings")]
    public GameObject groundCheckObject;
    public LayerMask groundMask;
    public float checkGroundRadius = 0.4f;

    [Header("Camera Settings")]
    public Transform cameraDirection;

    
    public bool isGrounded;
    public bool isDashing = false;

    private CharacterController characterController;
    private Vector3 velocity;
    private float lastDashTime;
    public float dashSmoothness = 0.1f;
    private Vector3 dashVelocity;
    private float originalCameraHeight; // Исходная высота камеры
    private float originalColliderHeight; // Исходная высота коллайдера

  

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        CreateGroundCheckObject();

        originalCameraHeight = cameraRotator.localPosition.y;
        originalColliderHeight = characterController.height;
        
    }

   void Update()
    {
        CheckGrounded();
        Crouch();
        Move();

        if (isDashing)
        {
            if (Time.time > dashEndTime)
            {
                isDashing = false;
            }
            else
            {
                Dash();
            }
        }
        else if (isGrounded && !isCrouching && Input.GetKeyDown(KeyCode.LeftShift) && Time.time - lastDashTime > dashCooldown)
        {
            StartDash();
        }
    }

    private void CreateGroundCheckObject()
    {
        groundCheckObject = new GameObject("GroundCheck");
        groundCheckObject.transform.SetParent(transform);
        groundCheckObject.transform.localPosition = new Vector3(0, -1f, 0);
        SphereCollider groundCheckCollider = groundCheckObject.AddComponent<SphereCollider>();
        groundCheckCollider.radius = checkGroundRadius;
        groundCheckCollider.isTrigger = true;
    }

    private void CheckGrounded()
    {
        Collider[] colliders = Physics.OverlapSphere(groundCheckObject.transform.position, checkGroundRadius, groundMask);
        isGrounded = colliders.Length > 0;
    }

    private void Crouch()
  {
    if (Input.GetKey(KeyCode.C))
    {
        isCrouching = true;
        cameraRotator.localPosition = Vector3.Lerp(cameraRotator.localPosition, new Vector3(cameraRotator.localPosition.x, crouchHeight, cameraRotator.localPosition.z), crouchSmoothness);
        characterController.height = Mathf.Lerp(characterController.height, colliderHeight, crouchSmoothness);
    }
    else
    {
        isCrouching = false;
        cameraRotator.localPosition = Vector3.Lerp(cameraRotator.localPosition, new Vector3(cameraRotator.localPosition.x, originalCameraHeight, cameraRotator.localPosition.z), crouchSmoothness);
        characterController.height = Mathf.Lerp(characterController.height, originalColliderHeight, crouchSmoothness);
    }

    // Найти все объекты с тегом playerWeapon
    GameObject[] playerWeapons = GameObject.FindGameObjectsWithTag("playerWeapon");

    // Отправить флаг isCrouch в аниматор каждого объекта
    foreach (GameObject playerWeapon in playerWeapons)
    {
        Animator animator = playerWeapon.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("isCrouch", isCrouching);
        }
    }
  }


  private void Move()
{
    float horizontal = Input.GetAxis("Horizontal");
    float vertical = Input.GetAxis("Vertical");

    Vector3 moveDirection = cameraDirection.forward * vertical + cameraDirection.right * horizontal;
    moveDirection.y = 0;
    moveDirection.Normalize();

    float currentSpeed = isCrouching ? crouchSpeed : moveSpeed; // Скорость изменяется в зависимости от приседания

    Vector3 preMovePosition = characterController.transform.position; // Запоминаем позицию до движения
    characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
    Vector3 postMovePosition = characterController.transform.position; // Запоминаем позицию после движения

    if (isGrounded && velocity.y < 0)
    {
        velocity.y = -2f; // Small negative value to ensure player stays on ground.
    }

    if (isGrounded && Input.GetKeyDown(KeyCode.Space))
    {
        RaycastHit hit;
        if (!Physics.Raycast(transform.position, transform.forward, out hit, 1f) || hit.normal.y > 0.5f)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
    }

    // Apply gravity.
    velocity.y += gravity * Time.deltaTime;
    characterController.Move(velocity * Time.deltaTime);

    // Check if player is not grounded after moving
    if (!isGrounded)
    {
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    // Найти все объекты с тегом playerWeapon
    GameObject[] playerWeapons = GameObject.FindGameObjectsWithTag("playerWeapon");

    // Отправить параметр движения в аниматор каждого объекта
    foreach (GameObject playerWeapon in playerWeapons)
    {
        Animator animator = playerWeapon.GetComponent<Animator>();
        if (animator != null)
        {
            // Вычисляем скорость на основе изменения позиции и времени
            float moveSpeed = (postMovePosition - preMovePosition).magnitude / Time.deltaTime;
            animator.SetFloat("Movement", moveSpeed);
        }
    }
}




     private void StartDash()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 dashDirection = cameraDirection.forward * vertical + cameraDirection.right * horizontal;
        dashDirection.y = 0;
        dashDirection.Normalize();

        dashVelocity = dashDirection * dashDistance / dashTime;
        dashEndTime = Time.time + dashTime;

        isDashing = true;
        lastDashTime = Time.time;
    }   

    private void Dash()
    {
        characterController.Move(dashVelocity * Time.deltaTime);
    }
}
