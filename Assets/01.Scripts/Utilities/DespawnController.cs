using UnityEngine;

/// <summary>
/// 풀링된 파티클 이펙트가 재생 끝 시 자동으로 풀에 반환하는 유틸리티 컴포넌트입니다.
/// </summary>
public class DespawnController : MonoBehaviour
{
    private ParticleSystem _particleSystem;

    public void Setup(ParticleSystem particleSystem)
    {
        _particleSystem = particleSystem;
    }

    private void Update()
    {
        if (_particleSystem == null) return;

        if (!_particleSystem.isPlaying)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
