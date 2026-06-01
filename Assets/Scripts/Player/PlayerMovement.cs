using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private Rigidbody2D rb;
    public float moveSpeed = 5f;
    private Vector2 moveInput;

    void Awake()
    {
        // manually disable all maps and enable only the Movement action map
        var inputActions = GetComponent<PlayerInput>().actions;
        foreach (var map in inputActions.actionMaps)
            map.Disable();
        inputActions.FindActionMap("Movement").Enable();

        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput.x, moveInput.y).normalized * moveSpeed;
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
