using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Transform swordPivot;
    [SerializeField] private Sword sword;
    [SerializeField] private float swingAngle = 120f, swingDuration = 0.15f, attackCooldown = 0.45f;
    private Health health;
    private bool isAttacking = false, canAttack = true;
    private Coroutine swingCoroutine;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        health = GetComponent<Health>();
        health.OnDeath += HandleDeath;
    }

    public void Attack(InputAction.CallbackContext callbackContext)
    {
        if (!callbackContext.performed || !canAttack || isAttacking || health.IsDead)
            return;

        isAttacking = true;
        canAttack = false;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 attackDir = (mousePos - (Vector2)transform.position).normalized;
        if (swingCoroutine != null)
            StopCoroutine(swingCoroutine);
        swingCoroutine = StartCoroutine(Swing(attackDir));
        sword.BeginSwing();
    }

    public void ResetCombat()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }
        sword.EndSwing();
        isAttacking = false;
        canAttack = true;
    }

    private void OnDestroy()
    {
        health.OnDeath -= HandleDeath;
    }

    private void HandleDeath(DamageInfo damageInfo)
    {
        ResetCombat();
        canAttack = false;
    }

    private IEnumerator Swing(Vector2 direction)
    {
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - swingAngle / 2f;
        float endAngle = baseAngle + swingAngle / 2f;

        float timer = swingDuration;
        while (timer >= 0f)
        {
            timer -= Time.deltaTime;
            float t = Mathf.Clamp01(1f - timer / swingDuration);

            swordPivot.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngle, endAngle, t));

            yield return null;
        }
        swordPivot.rotation = Quaternion.Euler(0f, 0f, endAngle);
        isAttacking = false;
        sword.EndSwing();

        yield return new WaitForSeconds(Mathf.Max(0f, attackCooldown - swingDuration));
        canAttack = true;
        swingCoroutine = null;
    }
}