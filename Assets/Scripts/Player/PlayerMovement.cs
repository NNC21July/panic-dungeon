using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerDash))]
[RequireComponent(typeof(Health))]
public class PlayerMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private Rigidbody2D rb;
    private PlayerDash playerDash;
    private Health health;
    public float moveSpeed = 5f;
    private Vector2 moveInput, lastMoveDirection = new Vector2(1, 0);

    void Awake()
    {
        // manually disable all maps and enable only the Movement action map
        var inputActions = GetComponent<PlayerInput>().actions;
        foreach (var map in inputActions.actionMaps)
            map.Disable();
        inputActions.FindActionMap("Movement").Enable();

        rb = GetComponent<Rigidbody2D>();
        playerDash = GetComponent<PlayerDash>();
        health = GetComponent<Health>();
        health.OnDeath += HandleDeath;
    }

    void FixedUpdate()
    {
        if (playerDash.IsDashing())
            return;

        rb.linearVelocity = new Vector2(moveInput.x, moveInput.y).normalized * moveSpeed;
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

    private void OnDestroy()
    {
        health.OnDeath -= HandleDeath;
    }
}
