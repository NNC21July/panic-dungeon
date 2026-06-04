using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDash : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;

    public float dashSpeed = 14f, dashDuration = 0.15f, dashCooldown = 1f;

    private bool isDashing = false, canDash = true;
    private Vector2 dashDirection;
    private float dashTimer = 0f, cooldownTimer = 0f;

    void Awake()
    {
        // manually disable all maps and enable only the Movement action map
        var inputActions = GetComponent<PlayerInput>().actions;
        foreach (var map in inputActions.actionMaps)
            map.Disable();
        inputActions.FindActionMap("Movement").Enable();

        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                canDash = false;
                cooldownTimer = dashCooldown;
            }
        }
        if (!canDash)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
                canDash = true;
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
            rb.linearVelocity = dashDirection * dashSpeed;
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (!context.performed || !canDash || isDashing)
            return;

        dashDirection = playerMovement.GetLastMoveDirection();

        isDashing = true;
        dashTimer = dashDuration;
    }

    public bool IsDashing()
    {
        return isDashing;
    }
}
