using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float MovementSpeed = 4f;
    public float InputDeadzone = 0.01f;

    [Header("Input")]
    public InputActionReference moveAction; // Vector2 action (WASD / stick)
    public AiPlayerMovement playerMovementMouse;

    [Header("References")]
    public Rigidbody2D rb;
    public CharacterRenderer characterRenderer; // your class that has SetDirection(Vector2)

    public bool IsControlled;
    public bool IsControllMouse;

    [HideInInspector]
    public GameObject ThisCharater;

    private Vector2 _moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ThisCharater = rb.gameObject;
    }

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.performed += OnMove;
            moveAction.action.canceled += OnMove;
            moveAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.performed -= OnMove;
            moveAction.action.canceled -= OnMove;
            moveAction.action.Disable();
        }
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (IsControllMouse && IsControlled)
            return;

        if (!IsControlled || rb == null) 
            return;

        Move2D();
    }

    void Move2D()
    {
        Vector2 input = Vector2.ClampMagnitude(_moveInput, 1f);
        if (input.magnitude < InputDeadzone)
            input = Vector2.zero;

        Vector2 movement = input * MovementSpeed;
        Vector2 newPos = rb.position + movement * Time.fixedDeltaTime;
        rb.MovePosition(newPos);

        // tell your renderer the movement direction (you might prefer input or movement)
/*        if (characterRenderer != null)
            characterRenderer.SetDirection(movement);*/
    }
}
