using Fusion;
using UnityEngine;

[DefaultExecutionOrder(-1500)]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(NetworkHealth))]
[RequireComponent(typeof(NetworkPlayerGameplayGate))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    [SerializeField] private PlayerTuning playerTuning;
    [Networked] private bool IsDashing { get; set; }
    [Networked] private float DashTimer { get; set; }
    [Networked] private float CooldownTimer { get; set; }
    [Networked] private Vector2 DashDirection { get; set; }
    [Networked] private Vector2 MoveInput { get; set; }
    private Rigidbody2D rb;
    private Vector2 lastMoveDirection = Vector2.right;
    private NetworkPlayerGameplayGate gameplayGate;
    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        rb = GetComponent<Rigidbody2D>();
        gameplayGate = GetComponent<NetworkPlayerGameplayGate>();
    }

    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);
    }

    private void FixedUpdate()
    {
        if (Object == null || !Object.IsValid)
            return;

        if (!gameplayGate.CanAct)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (IsDashing)
        {
            float progress = 1f - DashTimer / playerTuning.DashDuration;

            rb.linearVelocity =
                PlayerMovementMotion.CalculateDashVelocity(
                    DashDirection,
                    playerTuning.DashSpeed,
                    progress,
                    playerTuning.DashSpeedCurve
                );
            return;
        }
        rb.linearVelocity =
            PlayerMovementMotion.CalculateVelocity(
                rb.linearVelocity,
                MoveInput,
                playerTuning.MoveSpeed,
                playerTuning.Acceleration,
                playerTuning.Deceleration,
                Time.fixedDeltaTime
            );
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority && !Object.HasStateAuthority) // doesn't control this body
            return;
        if (!gameplayGate.CanAct)
        {
            MoveInput = Vector2.zero;
            IsDashing = false;
            DashTimer = 0f;
            return;
        }

        bool hasInput = GetInput(out NetworkInputData inputData);
        MoveInput = hasInput ? inputData.MoveDir : Vector2.zero;

        if (hasInput && inputData.Buttons.IsSet(NetworkInputButton.Dash) && CooldownTimer <= 0f && !IsDashing)
        {
            IsDashing = true;
            DashDirection = lastMoveDirection;
            DashTimer = playerTuning.DashDuration;
            CooldownTimer = playerTuning.DashCooldown;
        }

        if (IsDashing)
        {
            DashTimer -= Runner.DeltaTime;
            if (DashTimer <= 0f)
                IsDashing = false;
        }

        if (MoveInput != Vector2.zero)
            lastMoveDirection = MoveInput.normalized;

        if (CooldownTimer > 0f)
            CooldownTimer -= Runner.DeltaTime;
    }
}
