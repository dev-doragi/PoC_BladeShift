using System.Collections.Generic;
using UnityEngine;

public class WeaponCombat : MonoBehaviour
{
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _projectileLayer;
    [SerializeField] private float _knockbackPower = 15f;
    [SerializeField] private float _slashDamage = 15f;
    [SerializeField] private float _slashRadius = 3.5f;
    [SerializeField] private float _tickDamageInterval = 0.2f;
    [SerializeField] private float _pinDamage = 30f;
    [SerializeField] private float _spinSpeed = 720f;
    [SerializeField] private float _pinSpeed = 24f;

    private float _lastTickTime;

    public float SlashRadius => _slashRadius;
    public float SpinSpeed => _spinSpeed;
    public float PinSpeed => _pinSpeed;
    public LayerMask EnemyLayer => _enemyLayer;

    public void PerformSlashDamage(Vector3 position, float radius, float damage, float angleZ)
    {
        Vector2 boxSize = new Vector2(radius * 2.5f, radius * 1.5f);
        Collider2D[] targets = Physics2D.OverlapBoxAll(position, boxSize, angleZ, _enemyLayer);
        foreach (var col in targets)
        {
            if (col.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector2 hitPoint = col.ClosestPoint(position);
                Vector2 knockbackDirection = ((Vector2)col.transform.position - (Vector2)position).normalized;
                damageable.TakeDamage(new DamageData
                {
                    Damage = damage,
                    AttackerTeam = TeamType.Player,
                    HitPoint = hitPoint,
                    KnockbackForce = knockbackDirection * _knockbackPower,
                    IsPiercing = false
                });
            }
        }
    }

    public void TryTickSpinDamage(Vector3 position, float angleZ)
    {
        if (Time.time < _lastTickTime + _tickDamageInterval) return;

        PerformSlashDamage(position, _slashRadius, _slashDamage, angleZ);
        _lastTickTime = Time.time;
    }

    public void DefendProjectiles(Vector3 position)
    {
        float radius = _slashRadius * 1.2f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, _projectileLayer);
        if (hits == null || hits.Length == 0) return;

        HashSet<GameObject> despawned = new HashSet<GameObject>();
        foreach (var col in hits)
        {
            if (col == null) continue;

            GameObject projectile = col.gameObject;
            if (!despawned.Add(projectile)) continue;

            if (PoolManager.Instance != null)
                PoolManager.Instance.Despawn(projectile);
            else
                Object.Destroy(projectile);
        }
    }

    public void ResetTickTimer()
    {
        _lastTickTime = Time.time - _tickDamageInterval;
    }

    public bool PerformPinDamage(Transform targetTransform, Vector3 position, Vector2 direction, HashSet<IDamageable> hitTargets)
    {
        if (targetTransform == null) return false;
        if (!targetTransform.TryGetComponent<IDamageable>(out var damageable)) return false;
        if (damageable.IsDead) return false;
        if (hitTargets != null && !hitTargets.Add(damageable)) return !damageable.IsDead;

        Collider2D targetCollider = targetTransform.GetComponent<Collider2D>();
        if (targetCollider == null) return false;

        Vector2 knockbackDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        Vector2 hitPoint = targetCollider.ClosestPoint(position);
        damageable.TakeDamage(new DamageData
        {
            Damage = _pinDamage,
            AttackerTeam = TeamType.Player,
            HitPoint = hitPoint,
            KnockbackForce = knockbackDirection * (_knockbackPower * 3f),
            IsPiercing = true
        });

        return !damageable.IsDead;
    }
    public void PerformExplosiveFinisher(Vector2 center, Vector2 forward, EnemyBase pinnedTarget)
    {
        float radius = 7f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, _enemyLayer);
        foreach (var col in hits)
        {
            if (!col.TryGetComponent<EnemyBase>(out var enemy) || enemy.IsDead)
                continue;

            if (enemy == pinnedTarget)
            {
                // 즉사 + 무기 정면 15배 넉백
                enemy.TakeDamage(new DamageData
                {
                    Damage = 9999f,
                    AttackerTeam = TeamType.Player,
                    HitPoint = col.ClosestPoint(center),
                    KnockbackForce = forward.normalized * _knockbackPower * 15f,
                    IsPiercing = true
                });
            }
            else
            {
                // 방사형 5배 넉백
                Vector2 dir = ((Vector2)enemy.transform.position - center).normalized;
                enemy.TakeDamage(new DamageData
                {
                    Damage = _slashDamage,
                    AttackerTeam = TeamType.Player,
                    HitPoint = col.ClosestPoint(center),
                    KnockbackForce = dir * _knockbackPower * 5f,
                    IsPiercing = false
                });
            }
        }
    }
    public void PerformSpinFinisher(Vector3 position, List<Transform> pinnedTargets)
    {
        float radius = 7f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, _enemyLayer);
        foreach (var col in hits)
        {
            if (!col.TryGetComponent<EnemyBase>(out var other) || other.IsDead)
                continue;
            Vector2 dir = ((Vector2)col.transform.position - (Vector2)position).normalized;
            float angleOffset = Random.Range(-8f, 8f) * Mathf.Deg2Rad;
            Vector2 knockbackDir = new Vector2(
                dir.x * Mathf.Cos(angleOffset) - dir.y * Mathf.Sin(angleOffset),
                dir.x * Mathf.Sin(angleOffset) + dir.y * Mathf.Cos(angleOffset));
            if (pinnedTargets != null && pinnedTargets.Contains(col.transform))
            {
                other.TakeDamage(new DamageData
                {
                    Damage = 9999f,
                    AttackerTeam = TeamType.Player,
                    HitPoint = col.ClosestPoint(position),
                    KnockbackForce = knockbackDir * _knockbackPower * 10f,
                    IsPiercing = true
                });
            }
            else
            {
                other.TakeDamage(new DamageData
                {
                    Damage = _slashDamage,
                    AttackerTeam = TeamType.Player,
                    HitPoint = col.ClosestPoint(position),
                    KnockbackForce = knockbackDir * _knockbackPower * 5f,
                    IsPiercing = false
                });
            }
        }
    }
}
