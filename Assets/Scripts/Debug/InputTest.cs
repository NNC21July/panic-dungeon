using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    [SerializeField] private Health damageTarget;
    [SerializeField] private float debugDamage = 25f;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.hKey.wasPressedThisFrame)
            DamageTarget();

        if (Keyboard.current.eKey.wasPressedThisFrame)
            SpikeWarning();
    }

    private void DamageTarget()
    {
        if (damageTarget == null)
            return;

        DamageInfo damage = new DamageInfo(debugDamage, gameObject, DamageType.Normal);

        damageTarget.TakeDamage(damage);

        Debug.Log(damageTarget.GetCurrentHealth());
    }

    private void SpikeWarning()
    {
        Spike[] spikes = FindObjectsByType<Spike>();

        foreach (Spike spike in spikes)
            spike.Warning();
    }
}