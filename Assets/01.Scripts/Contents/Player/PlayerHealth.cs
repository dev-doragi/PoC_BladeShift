using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;
    private float _currentHealth;

    public TeamType Team => TeamType.Player;
    public bool IsDead => _currentHealth <= 0f;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(DamageData damageData)
    {
        if (IsDead) return;

        _currentHealth -= damageData.Damage;
        Debug.Log($"[PlayerHealth] 피해를 입었습니다! 남은 체력: {_currentHealth}");

        if (IsDead)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] 플레이어 사망!");
        EventBus.Instance.Publish(new StageFailedEvent { StageIndex = 0 });
    }
}
