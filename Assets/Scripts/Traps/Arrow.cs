using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float speed = 10f, damageAmount = 20f, lifetime = 3f;
    [SerializeField] private AudioClip hitSfx;
    private Rigidbody2D rb;
    private Vector2 travelDirection;
    private float remLifetime;
    private bool isInit = false, hasHit = false;
    private GameObject shooter;
    protected GameObject Shooter => shooter;

    protected virtual void Awake()
    {
        SerializedFieldValidator.Validate(this);
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

    public void Initialize(Vector2 direction, GameObject shooter)
    {
        if (direction == Vector2.zero)
            throw new ArgumentException("Arrow direction cannot be zero");
        if (shooter == null)
            throw new ArgumentNullException("Shooter game object cannot be null");
        travelDirection = direction.normalized;
        remLifetime = lifetime;
        float angle = Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg;
        rb.SetRotation(angle);
        this.shooter = shooter;
        isInit = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        if (other.gameObject.layer == GameLayers.Wall || other.gameObject.layer == GameLayers.Obstacle)
        {
            hasHit = true;
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(hitSfx, 0.15f);
            Destroy(gameObject);
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        bool damaged = damageable.TakeDamage(new DamageInfo(damageAmount, shooter, DamageType.Arrow));
        if (damaged)
        {
            OnSuccessfulHit(other);
            hasHit = true;
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(hitSfx, 0.15f);
            Destroy(gameObject);
        }
    }

    protected virtual void OnSuccessfulHit(Collider2D other) { }
}
