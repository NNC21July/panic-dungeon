using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class ZombieAI : MonoBehaviour
{
    [SerializeField] private ZombieTuning zombieTuning;
    [SerializeField, Min(0.1f)] private float deathFadeDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float deathBrightness = 0.6f;
    private ZombieDeathVisual deathVisual;
    private float deathElapsed;
    private Rigidbody2D rb;
    private Transform target;
    private Health health, targetHealth;
    private float attackCooldownTimer;
    private bool canAttack = true;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        deathVisual = GetComponent<ZombieDeathVisual>();
        if (deathVisual == null)
            deathVisual = gameObject.AddComponent<ZombieDeathVisual>();
        health.OnDeath += HandleDeath;
    }

    private void Update()
    {
        if (health.IsDead)
        {
            deathElapsed += Time.deltaTime;
            deathVisual.ShowDeath(deathBrightness, 1f - deathElapsed / Mathf.Max(0.1f, deathFadeDuration));
            if (deathElapsed >= Mathf.Max(0.1f, deathFadeDuration))
                Destroy(gameObject);
            return;
        }
        if (target == null)
        {
            target = GetTarget();
            if (target != null)
                targetHealth = target.gameObject.GetComponent<Health>();
        }
        if (targetHealth != null && targetHealth.IsDead)
        {
            target = null;
            targetHealth = null;
        }
        if (!canAttack)
        {
            attackCooldownTimer = ZombieRules.AdvanceCooldown(attackCooldownTimer, Time.deltaTime);
            if (attackCooldownTimer <= 0f)
                canAttack = true;
        }
    }

    private void FixedUpdate()
    {
        if (health.IsDead)
            return;
        if (target != null && targetHealth != null && !targetHealth.IsDead)
        {
            if (!ZombieRules.IsInAttackRange(rb.position, target.position, zombieTuning.AttackRange))
                rb.MovePosition(ZombieRules.MoveTowards(rb.position, target.position, zombieTuning.MoveSpeed, Time.fixedDeltaTime));
            else if (canAttack)
            {
                rb.linearVelocity = Vector2.zero;
                Attack();
            }
        }
    }

    private void Attack()
    {
        canAttack = false;
        attackCooldownTimer = zombieTuning.AttackCooldown;
        if (targetHealth != null && !targetHealth.IsDead)
        {
            ZombieRules.ApplyAttack(targetHealth, zombieTuning.AttackDamage, gameObject);
            if (targetHealth.IsDead)
            {
                target = null;
                targetHealth = null;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void HandleDeath(DamageInfo damageInfo)
    {
        target = null;
        targetHealth = null;
        canAttack = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        foreach (Collider2D bodyCollider in GetComponentsInChildren<Collider2D>())
            bodyCollider.enabled = false;
        deathElapsed = 0f;
        deathVisual.ShowDeath(deathBrightness, 1f);
    }

    private void OnDestroy()
    {
        health.OnDeath -= HandleDeath;
    }

    private Transform GetTarget()
    {
        Transform[] allObjects = FindObjectsByType<Transform>(
            FindObjectsInactive.Exclude
        );

        foreach (Transform t in allObjects)
        {
            GameObject obj = t.gameObject;
            if (obj.layer == GameLayers.Player)
            {
                Health targetHealth = obj.GetComponent<Health>();
                if (targetHealth != null && !targetHealth.IsDead)
                    return t;
            }
        }
        return null;
    }
}
