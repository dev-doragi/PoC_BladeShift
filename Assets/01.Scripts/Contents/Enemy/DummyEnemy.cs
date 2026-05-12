using UnityEngine;

public class DummyEnemy : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 50f;
    private float _currentHealth;

    public TeamType Team => TeamType.Enemy;
    public bool IsDead => _currentHealth <= 0f;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(DamageData damageData)
    {
        if (IsDead) return;

        _currentHealth -= damageData.Damage;
        Debug.Log($"<color=red>[Enemy]</color> 피격! 남은 체력: {_currentHealth} | 타격 위치: {damageData.HitPoint}");

        if (IsDead)
        {
            Debug.Log("<color=red>[Enemy]</color> 사망!");
            gameObject.SetActive(false); // 일단 비활성화
        }
    }
}