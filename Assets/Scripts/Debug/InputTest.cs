#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    [SerializeField] private Health damageTarget;
    [SerializeField] private RoundManager roundManager;
    [SerializeField] private PlayerReset playerReset;
    [SerializeField] private float debugDamage = 25f;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.hKey.wasPressedThisFrame)
            DamageTarget();

        if (Keyboard.current.eKey.wasPressedThisFrame)
            roundManager.Begin();

        if (Keyboard.current.rKey.wasPressedThisFrame)
            playerReset.ResetAt(new Vector2(0, 0));
    }

    private void DamageTarget()
    {
        if (damageTarget == null)
            return;

        DamageInfo damage = new DamageInfo(debugDamage, gameObject, DamageType.Normal);

        damageTarget.TakeDamage(damage);

        Debug.Log(damageTarget.CurrentHealth);
    }
}
#endif
