using Fusion;
using UnityEngine;

[DefaultExecutionOrder(-1500)]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(NetworkHealth))]
[RequireComponent(typeof(NetworkPlayerGameplayGate))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerDash))]
[RequireComponent(typeof(NetworkPlayerMovementSync))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private PlayerDash playerDash;
    private NetworkPlayerGameplayGate gameplayGate;
    public Vector2 SavedDashDirection { get; private set; }
    public int DashCount { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        playerDash = GetComponent<PlayerDash>();
        gameplayGate = GetComponent<NetworkPlayerGameplayGate>();

        playerMovement.enabled = false;
        playerDash.enabled = false;
    }

    public override void Spawned()
    {
        Runner.SetIsSimulated(Object, true);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = Object.HasInputAuthority
            ? RigidbodyType2D.Dynamic
            : RigidbodyType2D.Kinematic;
        rb.interpolation = Object.HasInputAuthority
            ? RigidbodyInterpolation2D.Interpolate
            : RigidbodyInterpolation2D.None;
        rb.useFullKinematicContacts = true;
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid)
            return;

        UpdateControlState();
        if (Object.HasInputAuthority && !Runner.ProvideInput)
            playerMovement.SetMoveInput(Vector2.zero);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority || !Runner.IsForward)
            return;

        UpdateControlState();
        if (!playerMovement.enabled)
            return;

        bool hasInput = GetInput(out NetworkInputData inputData) && Runner.ProvideInput;
        playerMovement.SetMoveInput(hasInput ? inputData.MoveDir : Vector2.zero);

        if (hasInput && inputData.Buttons.IsSet(NetworkInputButton.Dash) && playerDash.TryDash()) // dash and play dust effect
        {
            SavedDashDirection = playerMovement.GetLastMoveDirection();
            DashCount++;
        }
    }

    private void UpdateControlState()
    {
        bool canAct = Object.HasInputAuthority && gameplayGate.CanAct &&
            (Runner.IsServer || Runner.IsConnectedToServer);

        if (!canAct)
        {
            if (playerMovement.enabled)
                playerMovement.StopMovement();
            if (playerDash.enabled)
                playerDash.StopDash();

            rb.bodyType = RigidbodyType2D.Kinematic;
            return;
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        playerMovement.enabled = true;
        playerDash.enabled = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        playerMovement.StopMovement();
        playerDash.StopDash();
    }
}
