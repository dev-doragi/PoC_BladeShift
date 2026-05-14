using UnityEngine;

using System.Collections.Generic;

public class WeaponCapture : MonoBehaviour
{
    private readonly List<EnemyBase> _capturedEnemies = new List<EnemyBase>();
    private Transform _weaponTransform;

    private void Awake()
    {
        _weaponTransform = transform;
    }

    public void BindEnemy(Transform enemyTransform)
    {
        if (enemyTransform == null) return;
        enemyTransform.SetParent(_weaponTransform);
        enemyTransform.localPosition = new Vector3(
            UnityEngine.Random.Range(-0.8f, 0.8f),
            enemyTransform.localPosition.y,
            enemyTransform.localPosition.z
        );
        if (enemyTransform.TryGetComponent<EnemyBase>(out var enemy))
        {
            if (!_capturedEnemies.Contains(enemy))
                _capturedEnemies.Add(enemy);
        }
        if (enemyTransform.TryGetComponent<Rigidbody2D>(out var rb))
            rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void UnbindAll()
    {
        foreach (var enemy in _capturedEnemies)
        {
            if (enemy != null)
                enemy.transform.SetParent(null);
        }
        _capturedEnemies.Clear();
    }

    public IReadOnlyList<EnemyBase> GetCapturedEnemies() => _capturedEnemies;
}
