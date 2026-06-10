using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Health))]

public class PlayerDash : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private Health health;

    [SerializeField] private float dashSpeed = 14f, dashDuration = 0.15f, dashCooldown = 1f;

    [SerializeField] private ParticleSystem dustEffect;

    private bool isDashing = false, canDash = true;
    private Vector2 dashDirection;
    private float dashTimer = 0f, cooldownTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        health = GetComponent<Health>();
        health.OnDeath += HandleDeath;
    }

    private void Update()
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

    private void FixedUpdate()
    {
        if (isDashing)
            rb.linearVelocity = dashDirection * dashSpeed;
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (!context.performed || !canDash || isDashing || health.IsDead)
            return;

        dashDirection = playerMovement.GetLastMoveDirection();
        isDashing = true;
        dashTimer = dashDuration;

        float angle = Mathf.Atan2(-dashDirection.y, -dashDirection.x) * Mathf.Rad2Deg;
        if (dustEffect != null)
        {
            dustEffect.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            dustEffect.Play();
        }
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
        if (health != null)
            health.OnDeath -= HandleDeath;
    }
}
