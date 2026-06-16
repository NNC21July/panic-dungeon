using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerDash))]
[RequireComponent(typeof(StatusEffectController))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerReset : MonoBehaviour
{
    private Health health;
    private PlayerMovement playerMovement;
    private PlayerDash playerDash;
    private StatusEffectController effectController;
    private Rigidbody2D rb;

    private void Awake()
    {
        health = GetComponent<Health>();
        playerMovement = GetComponent<PlayerMovement>();
        playerDash = GetComponent<PlayerDash>();
        effectController = GetComponent<StatusEffectController>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void ResetAt(Vector2 spawnPos)
    {
        effectController.StopAllEffects();
        health.ResetHealth();
        rb.MovePosition(spawnPos);
        playerMovement.ResetMovement();
        playerDash.ResetDash();
    }
}