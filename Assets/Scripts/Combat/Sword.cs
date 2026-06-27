using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Sword : MonoBehaviour
{
    [SerializeField] private float damage = 35f;
    private BoxCollider2D swordCollider;
    private GameObject owner;
    private HashSet<IDamageable> hitTargets;

    private void Awake()
    {
        swordCollider = GetComponent<BoxCollider2D>();
        swordCollider.enabled = false;
        hitTargets = new HashSet<IDamageable>();
        gameObject.SetActive(false);
    }

    public void Initialize(GameObject owner)
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));
        this.owner = owner;
    }

    public void BeginSwing()
    {
        gameObject.SetActive(true);
        hitTargets.Clear();
        swordCollider.enabled = true;
    }

    public void EndSwing()
    {
        swordCollider.enabled = false;
        gameObject.SetActive(false);
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