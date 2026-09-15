using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float speed = 10f, damageAmount = 20f;
    [SerializeField] private AudioClip hitSfx;
    private Rigidbody2D rb;
    private Vector2 travelDirection;
    private bool isInit = false, hasHit = false;
    private GameObject shooter;
    protected GameObject Shooter => shooter;
    public bool SimulateLocally { get; set; } = true;
    public Action<Arrow, bool> FinishHandler { get; set; }

    protected virtual void Awake()
    {
        SerializedFieldValidator.Validate(this);
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (!SimulateLocally || !isInit)
            return;

        rb.linearVelocity = travelDirection * speed;
    }

    public void Initialize(Vector2 direction, GameObject shooter)
    {
        if (direction == Vector2.zero)
            throw new ArgumentException("Arrow direction cannot be zero");
        if (shooter == null)
            throw new ArgumentNullException("Shooter game object cannot be null");
        travelDirection = direction.normalized;
        float angle = Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg;
        rb.SetRotation(angle);
        this.shooter = shooter;
        hasHit = false;
        isInit = true;
    }

    private void Finish(bool hitSomething)
    {
        if (hasHit)
            return;

        hasHit = true;
        isInit = false;
        rb.linearVelocity = Vector2.zero;
        if (FinishHandler != null)
        {
            FinishHandler(this, hitSomething);
            return;
        }
        if (hitSomething)
            PlayHitSound();

        Destroy(gameObject);
    }

    public void PlayHitSound()
    {
        AudioManager.Instance?.PlayTrapSfx(hitSfx, 0.15f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!SimulateLocally || !isInit || hasHit)
            return;

        int otherLayer = other.gameObject.layer;

        if (otherLayer == GameLayers.Wall || otherLayer == GameLayers.Obstacle)
        {
            Finish(true);
            return;
        }

        if (otherLayer != GameLayers.Player && otherLayer != GameLayers.Enemy)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        bool damaged = damageable.TakeDamage(new DamageInfo(damageAmount, shooter, DamageType.Arrow));
        if (damaged)
        {
            OnSuccessfulHit(other);
            Finish(true);
        }
    }

    protected virtual void OnSuccessfulHit(Collider2D other) { }
}
