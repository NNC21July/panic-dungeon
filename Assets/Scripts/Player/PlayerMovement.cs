using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Health))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private PlayerDash playerDash;
    private Health health;
    private Vector2 moveInput, lastMoveDirection = new Vector2(1, 0);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerDash = GetComponent<PlayerDash>();
        health = GetComponent<Health>();
        health.OnDeath += HandleDeath;
    }

    private void FixedUpdate()
    {
        if (playerDash != null && playerDash.IsDashing())
            return;

        rb.linearVelocity = moveInput.normalized * moveSpeed;
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (moveInput != Vector2.zero)
            lastMoveDirection = moveInput.normalized;
    }

    public Vector2 GetLastMoveDirection()
    {
        return lastMoveDirection;
    }

    private void HandleDeath(DamageInfo damageInfo)
    {
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
