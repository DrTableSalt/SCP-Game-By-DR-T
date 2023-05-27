using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool CanMove { get; set; } = true;
    private bool IsSprinting => canSprint && Input.GetKey(Controls.i.SprintKey);
    private bool ShouldJump => Input.GetKeyDown(Controls.i.JumpKey) && characterController.isGrounded;
    private bool ShouldCrouch => Input.GetKeyDown(Controls.i.CrouchKey) && !duringCrouchAnimation && characterController.isGrounded;

    [Header("Functional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canUseHeadBob = true;
    [SerializeField] private bool willSlideOnSlopes = true;

    [Header("Health Parameters")]
    [SerializeField] private float maxHealth = 100;
    [SerializeField] private float timeBeforeRegenStarts = 3;
    [SerializeField] private float healthValueIncrement = 1;
    [SerializeField] private float healthTimeIncrement = 0.1f;
    private float currentHealth;
    private Coroutine regeneratingHealth;
    public static Action<float> OnTakeDamage;
    public static Action<float> OnDamage;
    public static Action<float> OnHeal;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 3.0f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float slopeSpeed = 8f;

    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 180)] private float lowerLookLimit = 80.0f;
    [SerializeField, Range(1, 20)] private float camSpeed = 1.5f;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpPower = 8.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    private bool isCrouching;
    private bool duringCrouchAnimation;

    [Header("Headbob Parameters")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.11f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.025f;
    private float defaultYPos = 0;
    private float timer;


    // SLIDING PARAMETERS

    private Vector3 hitPointNormal;
    private bool isSliding 
    { 
        get
        {
            if (characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 4f))
            {
                hitPointNormal = slopeHit.normal;
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
            }
            else
            {
                return false;
            }
        }
    
    }

    private Camera playerCam;
    [SerializeField] private Camera objectCam;
    private CharacterController characterController;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float rotationX = 0;
    private float rotationY = 0;

    public static PlayerController instance;

    void OnEnable()
    {
        OnTakeDamage += ApplyDamage;
    }
    void OnDisable()
    {
        OnTakeDamage -= ApplyDamage;
    }

    void Awake()
    {
        instance = this;

        playerCam = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentHealth = maxHealth;

        defaultYPos = playerCam.transform.localPosition.y;
    }

    void Update()
    {
        if (CanMove)
        {
            HandleMovementInput();
            HandleMouseLook();
            ApplyFinalMovement();
        }
        if (canJump)
        {
            HandleJump();
        }

        if (canCrouch)
        {
            HandleCrouch();
        }

        if (canUseHeadBob)
        {
            HandleHeadBob();
        }

    }

    private void HandleMovementInput()
    {
        currentInput = new Vector2((isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;
    }

    private void HandleMouseLook()
    {
        rotationY += Input.GetAxis("Mouse X") * lookSpeedX;
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;

        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);


        Quaternion target1 = Quaternion.Euler(rotationX, 0, 0);
        Quaternion target2 = Quaternion.Euler(0, rotationY, 0);
        playerCam.transform.localRotation = Quaternion.Slerp(playerCam.transform.localRotation, target1, Time.deltaTime * camSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, target2, Time.deltaTime * camSpeed);
    }

    private void HandleJump()
    {
        if (ShouldJump)
            moveDirection.y = jumpPower;
    }

    private void HandleCrouch()
    {
        if (ShouldCrouch)
        {
            StartCoroutine(CrouchStand());
        }
    }

    private void HandleHeadBob()
    {
        if(!characterController.isGrounded) return;
        if (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * (isCrouching ? crouchBobSpeed : IsSprinting ? sprintBobSpeed : walkBobSpeed);
            playerCam.transform.localPosition = new Vector3(
                playerCam.transform.localPosition.x,
                defaultYPos + Mathf.Sin(timer) * (isCrouching ? crouchBobAmount : IsSprinting ? sprintBobAmount : walkBobAmount),
                playerCam.transform.localPosition.z
            );

        }
    }

    private IEnumerator CrouchStand()
    {
        duringCrouchAnimation = true;

        float timeElapsed = 0;
        float targetHeight = isCrouching ? standHeight : crouchHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        while (timeElapsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed/timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed/timeToCrouch);
        
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isCrouching = !isCrouching;

        duringCrouchAnimation = false;
    }


    private void ApplyDamage(float dmg)
    {
        currentHealth -= dmg;
        OnDamage?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            KillPlayer();
        }
        else if (regeneratingHealth != null)
        {
            StopCoroutine(regeneratingHealth);
        }

        regeneratingHealth = StartCoroutine(RegenHealth());
    }

    private void KillPlayer()
    {
        currentHealth = 0;

        if (regeneratingHealth != null)
        {
            StopCoroutine(regeneratingHealth);

            print("Dead");
        }
    }

    private IEnumerator RegenHealth()
    {
        yield return new WaitForSeconds(timeBeforeRegenStarts);
        WaitForSeconds timeToWait = new WaitForSeconds(healthTimeIncrement);

        while (currentHealth < maxHealth)
        {
            currentHealth += healthValueIncrement;

            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            OnHeal?.Invoke(currentHealth);
            yield return timeToWait;
        }

        regeneratingHealth = null;
    }

    private void ApplyFinalMovement()
    {
        if(!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        if (willSlideOnSlopes && isSliding)
        {
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;
        }

            characterController.Move(moveDirection * Time.deltaTime);
    }


}
