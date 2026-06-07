using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Health))]

public class PlayerDash : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private Health health;

    public float dashSpeed = 14f, dashDuration = 0.15f, dashCooldown = 1f;

    public ParticleSystem dustEffect;

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
        health = GetComponent<Health>();
        health.OnDeath += HandleDeath;
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
                rb.linearVelocity = Vector2.zero;
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

        float angle = Mathf.Atan2(-dashDirection.y, -dashDirection.x) * Mathf.Rad2Deg;
        dustEffect.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        dustEffect.Play();
    }

    public bool IsDashing()
    {
        return isDashing;
    }

    private void HandleDeath(DamageInfo damageInfo)
    {
        isDashing = false;
        rb.linearVelocity = Vector2.zero;
        enabled = false;
    }

    private void OnDestroy()
    {
        health.OnDeath -= HandleDeath;
    }
}
