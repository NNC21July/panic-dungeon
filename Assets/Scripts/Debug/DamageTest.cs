using UnityEngine;
using UnityEngine.InputSystem;

public class DamageTest : MonoBehaviour
{
    [SerializeField] private Health target;
    [SerializeField] private float debugDamage = 25f;

    // Update is called once per frame
    void Update()
    {
        if (target == null)
            return;
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            DamageInfo damage = new DamageInfo(debugDamage, gameObject, DamageType.Normal);

            target.TakeDamage(damage);

            Debug.Log(target.GetCurrentHealth());
        }
    }
}
