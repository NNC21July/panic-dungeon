using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float speed = 10f, damageAmount = 20f, lifetime = 3f;
    private Rigidbody2D rb;
    private Vector2 travelDirection;
    private float remLifetime;
    private bool isInit = false, hasHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!isInit)
            return;

        remLifetime -= Time.deltaTime;
        if (remLifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void FixedUpdate()
    {
        if (!isInit)
            return;

        rb.MovePosition(rb.position + travelDirection * speed * Time.fixedDeltaTime);
    }

    public void Initialize(Vector2 direction)
    {
        if (direction == Vector2.zero)
            throw new ArgumentException("Arrow direction cannot be zero");
        travelDirection = direction.normalized;
        remLifetime = lifetime;
        float angle = Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg;
        rb.SetRotation(angle);
        isInit = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            hasHit = true;
            Destroy(gameObject);
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        bool damaged = damageable.TakeDamage(new DamageInfo(damageAmount, gameObject, DamageType.Arrow));
        if (damaged)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}