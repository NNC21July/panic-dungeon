using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Sword : MonoBehaviour
{
    [SerializeField] private float damage = 35f;
    [SerializeField] private TrailRenderer swingTrail;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D swordCollider;
    private GameObject owner;
    private HashSet<IDamageable> hitTargets;

    private void Awake()
    {
        SerializedFieldValidator.Validate(this);

        swordCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        swordCollider.enabled = false;
        spriteRenderer.enabled = false;
        swingTrail.emitting = false;
        swingTrail.Clear();
        hitTargets = new HashSet<IDamageable>();
    }

    public void Initialize(GameObject owner)
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));
        this.owner = owner;
    }

    public void BeginSwing()
    {
        spriteRenderer.enabled = true;
        swordCollider.enabled = true;
        swingTrail.Clear();
        swingTrail.emitting = true;

        hitTargets.Clear();
    }

    public void EndSwing()
    {
        swordCollider.enabled = false;
        spriteRenderer.enabled = false;
        swingTrail.emitting = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;
        if (hitTargets.Contains(damageable))
            return;

        damageable.TakeDamage(new DamageInfo(damage, owner, DamageType.Melee));
        hitTargets.Add(damageable);
    }
}