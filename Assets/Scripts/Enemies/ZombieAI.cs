using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class ZombieAI : MonoBehaviour
{
    [SerializeField] private ZombieTuning zombieTuning;
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
        health.OnDeath += HandleDeath;
    }

    private void Update()
    {
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
        Destroy(gameObject);
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
