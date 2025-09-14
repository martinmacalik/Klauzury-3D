using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCamera;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float jumpPower = 7f;
    [SerializeField] private float gravity = 10f;

    [Header("Look")]
    [SerializeField] private float lookSpeed = 2f;
    [SerializeField] private float lookXLimit = 45f;

    [Header("Crouch")]
    [SerializeField] private float defaultHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private bool crouchToggle = false;

    private CharacterController characterController;
    private Vector3 velocity;
    private float rotationX = 0f;
    private bool canMove = true;
    private bool isCrouching = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- Look ---
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeed, 0f);
        }

        // --- Crouch input ---
        if (crouchToggle)
        {
            if (Input.GetKeyDown(crouchKey)) isCrouching = !isCrouching;
        }
        else
        {
            isCrouching = Input.GetKey(crouchKey);
        }

        // Height lerp (optional: smooth)
        characterController.height = Mathf.Lerp(characterController.height,
                                                isCrouching ? crouchHeight : defaultHeight,
                                                Time.deltaTime * 12f);

        // --- Movement input ---
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f); // no faster diagonals

        Vector3 move = transform.right * input.x + transform.forward * input.y;

        bool isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
        float targetSpeed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);

        // Horizontal move
        Vector3 horizontal = move * targetSpeed;

        // Preserve vertical velocity
        float y = velocity.y;

        velocity = new Vector3(horizontal.x, y, horizontal.z);

        // Jump
        if (characterController.isGrounded)
        {
            if (velocity.y < 0f) velocity.y = -2f; // stick to ground
            if (Input.GetButtonDown("Jump") && canMove && !isCrouching)
            {
                velocity.y = jumpPower;
            }
        }

        // Gravity
        velocity.y -= gravity * Time.deltaTime;

        // Move character
        characterController.Move(velocity * Time.deltaTime);
    }
}
