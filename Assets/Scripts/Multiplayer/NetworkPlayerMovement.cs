using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerMovement : NetworkBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData inputData))
            return;

        Vector2 dir = inputData.MoveDir.normalized;
        Vector3 movement = new Vector3(dir.x, dir.y, 0f);

        transform.position += movement * moveSpeed * Runner.DeltaTime;
    }
}