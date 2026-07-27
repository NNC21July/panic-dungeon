using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CircleCollider2D))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f, acceleration = 50f, deceleration = 50f, collisionSkin = 0.02f;
    [SerializeField] private LayerMask collisionMask;
    [Networked] private Vector2 CurVelocity { get; set; }
    private CircleCollider2D bodyCollider;

    private void Awake()
    {
        bodyCollider = GetComponent<CircleCollider2D>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData inputData))
            return;

        Vector2 dir = inputData.MoveDir.normalized,
            targetVelocity = dir * moveSpeed;
        float changeRate = dir == Vector2.zero ? deceleration : acceleration;

        CurVelocity = Vector2.MoveTowards(CurVelocity, targetVelocity, changeRate * Runner.DeltaTime);

        Vector2 movement = CurVelocity * Runner.DeltaTime;
        MoveWithStaticCollision(movement);
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

        RaycastHit2D hit = Physics2D.CircleCast(castOrigin, radius, dir, distance + collisionSkin, collisionMask);
        // if a circle of this size moved in this direction, what would it hit?

        if (hit.collider == null)
            return movement;
        float allowedDistance = Mathf.Clamp(hit.distance - collisionSkin, 0f, distance);

        return dir * allowedDistance;
    }
}