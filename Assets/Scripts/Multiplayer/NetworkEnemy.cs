using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkHealth))]
[RequireComponent(typeof(CircleCollider2D))]
public class NetworkEnemy : NetworkBehaviour
{
    [SerializeField] private ZombieTuning zombieTuning;
    private Rigidbody2D rb;
    private NetworkObject target;
    private NetworkHealth targetHealth;
    private NetworkMatchManager matchManager;
    private NetworkHealth health;
    private CircleCollider2D bodyCollider;
    private float attackCooldownTimer;
    private TickTimer deathTimer;
    public ZombieTuning Tuning => zombieTuning;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<NetworkHealth>();
        bodyCollider = GetComponent<CircleCollider2D>();
    }

    public override void Spawned()
    {
        target = null;
        targetHealth = null;
        matchManager = null;
        attackCooldownTimer = 0f;
        deathTimer = TickTimer.None;
        bodyCollider.enabled = true;
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            NetworkMatchManager candidate = root.GetComponentInChildren<NetworkMatchManager>(true);
            if (candidate != null && candidate.Runner == Runner)
            {
                matchManager = candidate;
                break;
            }
        }
        rb.bodyType = Object.HasStateAuthority ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        // NetworkTransform handles remote interpolation; do not add a second smoothing pass.
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        Runner.AddVisibilityNodes(gameObject);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !Runner.IsForward)
            return;
        if (health.IsDead)
        {
            StopDeadBody();
            if (!deathTimer.IsRunning)
                deathTimer = TickTimer.CreateFromSeconds(Runner, 0.5f);
            if (deathTimer.Expired(Runner))
                Runner.Despawn(Object);
            return;
        }
        if (matchManager == null || matchManager.State != NetworkMatchState.Playing)
        {
            target = null;
            targetHealth = null;
            rb.linearVelocity = Vector2.zero;
            return;
        }
        UpdateTarget();
        attackCooldownTimer = ZombieRules.AdvanceCooldown(attackCooldownTimer, Runner.DeltaTime);
        if (target != null && targetHealth != null && !targetHealth.IsDead && attackCooldownTimer <= 0f &&
            ZombieRules.IsInAttackRange(rb.position, target.transform.position, zombieTuning.AttackRange))
        {
            // A close player on the other side of a wall is not reachable by a bite.
            RaycastHit2D obstruction = Runner.GetPhysicsScene2D().Linecast(rb.position, target.transform.position,
                (1 << GameLayers.Wall) | (1 << GameLayers.Obstacle));
            if (obstruction.collider == null)
            {
                attackCooldownTimer = zombieTuning.AttackCooldown;
                ZombieRules.ApplyAttack(targetHealth, zombieTuning.AttackDamage, gameObject);
            }
        }
    }

    private void UpdateTarget()
    {
        NetworkObject nearest = null;
        NetworkHealth nearestHealth = null;
        float nearestDistanceSquared = float.PositiveInfinity;
        float currentDistanceSquared = float.PositiveInfinity;
        bool currentTargetAlive = false;
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObj) || playerObj == null || !playerObj.IsValid)
                continue;
            NetworkHealth health = playerObj.GetComponent<NetworkHealth>();
            if (health == null || health.IsDead)
                continue;
            float distanceSquared = ((Vector2)playerObj.transform.position - rb.position).sqrMagnitude;
            if (playerObj == target)
            {
                currentTargetAlive = true;
                currentDistanceSquared = distanceSquared;
            }
            if (distanceSquared < nearestDistanceSquared)
            {
                nearest = playerObj;
                nearestHealth = health;
                nearestDistanceSquared = distanceSquared;
            }
        }
        // Find the closest first, then compare it with the current target just once.
        if (!currentTargetAlive || ZombieRules.ShouldSwitchTarget(currentDistanceSquared, nearestDistanceSquared, zombieTuning.TargetSwitchMargin))
        {
            target = nearest;
            targetHealth = nearestHealth;
        }
    }

    private void FixedUpdate()
    {
        if (Object == null || !Object.IsValid || !Object.HasStateAuthority)
            return;
        if (health.IsDead)
        {
            StopDeadBody();
            return;
        }
        if (matchManager == null || matchManager.State != NetworkMatchState.Playing ||
            target == null || !target.IsValid || targetHealth == null || targetHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        // Like single-player, submit one movement per Unity physics step, not once per Fusion catch-up tick.
        if (!ZombieRules.IsInAttackRange(rb.position, target.transform.position, zombieTuning.AttackRange))
            rb.MovePosition(ZombieRules.MoveTowards(rb.position, target.transform.position, zombieTuning.MoveSpeed, Time.fixedDeltaTime));
        else
            rb.linearVelocity = Vector2.zero;
    }

    public override void Render()
    {
        if (health.IsDead)
            StopDeadBody();
    }

    private void StopDeadBody()
    {
        target = null;
        targetHealth = null;
        bodyCollider.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        target = null;
        targetHealth = null;
        matchManager = null;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
}
