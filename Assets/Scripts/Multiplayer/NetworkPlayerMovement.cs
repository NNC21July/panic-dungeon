using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CircleCollider2D))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f, acceleration = 50f, deceleration = 50f, collisionSkin = 0.02f, dashSpeed = 22f, dashDuration = 0.15f, dashCooldown = 1f;
    [SerializeField] private LayerMask collisionMask;
    [Networked] private Vector2 CurVelocity { get; set; }
    [Networked] private bool IsDashing { get; set; }
    [Networked] private float DashTimer { get; set; }
    [Networked] private float CooldownTimer { get; set; }
    [Networked] private Vector2 DashDirection { get; set; }
    private CircleCollider2D bodyCollider;
    private Vector2 lastMoveDirection = Vector2.right;

    private void Awake()
    {
        bodyCollider = GetComponent<CircleCollider2D>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData inputData))
            return;

        if (inputData.Buttons.IsSet(NetworkInputButton.Dash) && CooldownTimer <= 0f && !IsDashing)
        {
            IsDashing = true;
            DashDirection = lastMoveDirection;
            DashTimer = dashDuration;
            CooldownTimer = dashCooldown;
        }

        if (inputData.MoveDir != Vector2.zero)
            lastMoveDirection = inputData.MoveDir.normalized;

        if (IsDashing)
        {
            DashTimer -= Runner.DeltaTime;
            Vector2 dashMovement = DashDirection * dashSpeed * Runner.DeltaTime;
            MoveWithStaticCollision(dashMovement);
            if (DashTimer <= 0f)
                IsDashing = false;
        }
        else
        {
            Vector2 dir = inputData.MoveDir.normalized, targetVelocity = dir * moveSpeed;
            float changeRate = dir == Vector2.zero ? deceleration : acceleration;

            CurVelocity = Vector2.MoveTowards(CurVelocity, targetVelocity, changeRate * Runner.DeltaTime);

            Vector2 movement = CurVelocity * Runner.DeltaTime;
            MoveWithStaticCollision(movement);
        }
        if (CooldownTimer > 0f)
            CooldownTimer -= Runner.DeltaTime;
    }

    private void MoveWithStaticCollision(Vector2 movement)
    {
        Vector2 pos = transform.position;

        Vector2 horizontal = new Vector2(movement.x, 0f),
                vertical = new Vector2(0f, movement.y);

        pos += GetAllowedMovement(horizontal, pos);
        pos += GetAllowedMovement(vertical, pos);

        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

    private Vector2 GetAllowedMovement(Vector2 movement, Vector2 bodyPos)
    {
        float distance = movement.magnitude;
        if (distance <= 0f)
            return Vector2.zero;

        Vector2 dir = movement.normalized,
                scale = new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        float radius = bodyCollider.radius * Mathf.Max(scale.x, scale.y);
        Vector2 castOrigin = bodyPos + Vector2.Scale(bodyCollider.offset, scale);

        PhysicsScene2D physicsScene = Runner.GetPhysicsScene2D();
        RaycastHit2D hit = physicsScene.CircleCast(castOrigin, radius, dir, distance + collisionSkin, collisionMask);
        // if a circle of this size moved in this direction, what would it hit?

        if (hit.collider == null)
            return movement;
        float allowedDistance = Mathf.Clamp(hit.distance - collisionSkin, 0f, distance);

        return dir * allowedDistance;
    }
}