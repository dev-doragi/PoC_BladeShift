using System.Collections.Generic;
using UnityEngine;

public class WeaponCombat : MonoBehaviour
{
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _slashDamage = 15f;
    [SerializeField] private float _slashRadius = 3.5f;
    [SerializeField] private float _tickDamageInterval = 0.2f;
    [SerializeField] private float _pinDamage = 30f;
    [SerializeField] private float _pinRadius = 1.2f;
    [SerializeField] private float _spinSpeed = 720f;
    [SerializeField] private float _pinSpeed = 24f;

    private float _lastTickTime;

    public float SlashRadius => _slashRadius;
    public float SpinSpeed => _spinSpeed;
    public float PinSpeed => _pinSpeed;

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

    public void TryTickSpinDamage(Vector3 position, float angleZ)
    {
        if (Time.time < _lastTickTime + _tickDamageInterval) return;

        PerformSlashDamage(position, _slashRadius, _slashDamage, angleZ);
        _lastTickTime = Time.time;
    }

    public void ResetTickTimer()
    {
        _lastTickTime = Time.time - _tickDamageInterval;
    }

    public void PerformPinDamage(Vector3 position, HashSet<IDamageable> hitTargets)
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(position, _pinRadius, _enemyLayer);
        foreach (Collider2D col in targets)
        {
            if (!col.TryGetComponent<IDamageable>(out var damageable)) continue;
            if (hitTargets != null && !hitTargets.Add(damageable)) continue;

            damageable.TakeDamage(new DamageData
            {
                Damage = _pinDamage,
                AttackerTeam = TeamType.Player,
                IsPiercing = true
            });
        }
    }
}
