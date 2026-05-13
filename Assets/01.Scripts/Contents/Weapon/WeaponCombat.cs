using System.Collections.Generic;
using UnityEngine;

public class WeaponCombat : MonoBehaviour
{
    [SerializeField] private LayerMask _enemyLayer;

    public void Configure(LayerMask enemyLayer)
    {
        _enemyLayer = enemyLayer;
    }

    public void PerformSlashDamage(Vector3 position, float radius, float damage, float angleZ)
    {
        Vector2 boxSize = new Vector2(radius * 2.5f, radius * 1.5f);
        Collider2D[] targets = Physics2D.OverlapBoxAll(position, boxSize, angleZ, _enemyLayer);
        foreach (var col in targets)
        {
            if (col.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(new DamageData
                {
                    Damage = damage,
                    AttackerTeam = TeamType.Player
                });
            }
        }
    }

    public void PerformThrustDamage(Vector3 position, float radius, float damage, HashSet<IDamageable> hitTargets)
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(position, radius, _enemyLayer);
        foreach (Collider2D col in targets)
        {
            if (!col.TryGetComponent<IDamageable>(out var damageable)) continue;
            if (hitTargets != null && !hitTargets.Add(damageable)) continue;

            damageable.TakeDamage(new DamageData
            {
                Damage = damage,
                AttackerTeam = TeamType.Player,
                IsPiercing = true
            });
        }
    }
}
