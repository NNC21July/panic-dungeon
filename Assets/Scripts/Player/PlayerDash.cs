using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Health))]

public class PlayerDash : MonoBehaviour
{
    [SerializeField] private PlayerTuning playerTuning;
    [SerializeField] private ParticleSystem dustEffect;
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private Health health;
    private bool isDashing = false, canDash = true;
    private Vector2 dashDirection;
    private float dashTimer = 0f, cooldownTimer = 0f;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

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
                cooldownTimer = playerTuning.DashCooldown;
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
        if (!isDashing)
            return;

        float progress = 1f - dashTimer / playerTuning.DashDuration;
        rb.linearVelocity = PlayerMovementMotion.CalculateDashVelocity(
            dashDirection,
            playerTuning.DashSpeed,
            progress,
            playerTuning.DashSpeedCurve
        );
    }

    public void Dash(InputAction.CallbackContext context)
    {
        if (!context.performed || !canDash || isDashing || health.IsDead)
            return;

        dashDirection = playerMovement.GetLastMoveDirection();
        isDashing = true;
        dashTimer = playerTuning.DashDuration;

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

    public void ResetDash()
    {
        canDash = true;
        isDashing = false;
        dashTimer = 0f;
        cooldownTimer = 0f;
        rb.linearVelocity = Vector2.zero;
        enabled = true;
    }

    private void OnDestroy()
    {
        health.OnDeath -= HandleDeath;
    }
}
