using NaughtyAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Required] private PlayerTuning playerTuning;
    private Rigidbody2D rb;
    private PlayerDash playerDash;
    private Health health;
    [ShowNonSerializedField]
    private Vector2 moveInput, lastMoveDirection = new Vector2(1, 0);

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        rb = GetComponent<Rigidbody2D>();
        playerDash = GetComponent<PlayerDash>();
        health = GetComponent<Health>();
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void FixedUpdate()
    {
        if (playerDash != null && playerDash.IsDashing())
            return;

        rb.linearVelocity = PlayerMovementMotion.CalculateVelocity(
            rb.linearVelocity,
            moveInput,
            playerTuning.MoveSpeed,
            playerTuning.Acceleration,
            playerTuning.Deceleration,
            Time.fixedDeltaTime
        );
    }

    public void Move(InputAction.CallbackContext context)
    {
        SetMoveInput(context.ReadValue<Vector2>());
    }

    public void SetMoveInput(Vector2 direction)
    {
        if (!isActiveAndEnabled || (health != null && health.IsDead))
            return;

        moveInput = direction;
        if (moveInput != Vector2.zero)
            lastMoveDirection = moveInput.normalized;
    }

    public Vector2 GetLastMoveDirection()
    {
        return lastMoveDirection;
    }

    private void HandleDeath(DamageInfo damageInfo)
    {
        StopMovement();
    }

    public void StopMovement()
    {
        moveInput = new Vector2(0, 0);
        rb.linearVelocity = Vector2.zero;
        enabled = false;
    }

    public void ResetMovement()
    {
        rb.linearVelocity = Vector2.zero;
        enabled = true;
        moveInput = new Vector2(0, 0);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }
}
