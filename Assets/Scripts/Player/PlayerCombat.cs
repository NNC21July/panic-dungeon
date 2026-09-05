using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private PlayerTuning playerTuning;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform swordPivot;
    [SerializeField] private Sword sword;
    private Health health;
    private bool isAttacking = false, canAttack = true;
    private Coroutine swingCoroutine;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        health = GetComponent<Health>();
        health.OnDeath += HandleDeath;

        sword.Initialize(gameObject);
    }

    public void Attack(InputAction.CallbackContext callbackContext)
    {
        if (!callbackContext.performed || !canAttack || isAttacking || health.IsDead)
            return;

        isAttacking = true;
        canAttack = false;

        Vector2 mousePos = worldCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
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
        float timer = playerTuning.SwingDuration;
        while (timer >= 0f)
        {
            timer -= Time.deltaTime;
            float progress = 1f - timer / playerTuning.SwingDuration,
                  currentAngle = SwordSwingMotion.CalculateAngle(
                      direction,
                      progress,
                      playerTuning.SwingAngle,
                      playerTuning.SwingCurve
                  );

            swordPivot.rotation = Quaternion.Euler(0f, 0f, currentAngle);

            yield return null;
        }
        float endAngle = SwordSwingMotion.CalculateAngle(
            direction,
            1f,
            playerTuning.SwingAngle,
            playerTuning.SwingCurve
        );
        swordPivot.rotation = Quaternion.Euler(0f, 0f, endAngle);
        isAttacking = false;
        sword.EndSwing();

        yield return new WaitForSeconds(
            Mathf.Max(0f, playerTuning.AttackCooldown - playerTuning.SwingDuration)
        );
        canAttack = true;
        swingCoroutine = null;
    }
}
