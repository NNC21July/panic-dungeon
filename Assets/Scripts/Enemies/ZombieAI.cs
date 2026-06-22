using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class ZombieAI : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f, attackRange = 0.8f, attackDamage = 15f, attackCooldown = 1f;
    private Rigidbody2D rb;
    private Transform target;
    private Health health, targetHealth;

    private void Awake()
    {
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
    }

    private void FixedUpdate()
    {
        if (target != null && !targetHealth.IsDead && ((Vector2)target.position - rb.position).sqrMagnitude > attackRange * attackRange)
            rb.MovePosition(Vector2.MoveTowards(rb.position, target.position, moveSpeed * Time.fixedDeltaTime));
    }

    private void HandleDeath(DamageInfo damageInfo)
    {
        gameObject.SetActive(false);
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
            if (obj.layer == LayerMask.NameToLayer("Player"))
            {
                Health targetHealth = obj.GetComponent<Health>();
                if (targetHealth != null && !targetHealth.IsDead)
                    return t;
            }
        }
        return null;
    }
}