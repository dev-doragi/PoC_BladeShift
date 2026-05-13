using System.Collections;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [SerializeField] protected float _maxHealth = 50f;
    protected float _currentHealth;

    protected Rigidbody2D _rb;
    protected SpriteRenderer _spriteRenderer;
    protected Collider2D _collider;

    protected Color _originalColor;
    protected Coroutine _blinkRoutine;
    protected bool _hasHitWallAfterDeath = false;

    public virtual TeamType Team => TeamType.Enemy;
    public virtual bool IsDead => _currentHealth <= 0f;

    protected virtual void Awake()
    {
        _currentHealth = _maxHealth;
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();

        if (_spriteRenderer != null)
            _originalColor = _spriteRenderer.color;

        if (_rb != null)
        {
            _rb.freezeRotation = true;
            _rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            _rb.linearDamping = 10f;
        }
    }

    public virtual void TakeDamage(DamageData damageData)
    {
        if (IsDead) return;

        _currentHealth -= damageData.Damage;

        if (_rb != null && damageData.KnockbackForce.sqrMagnitude > 0.0001f)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(damageData.KnockbackForce, ForceMode2D.Impulse);
        }

        if (EventBus.Instance != null)
        {
            EventBus.Instance.Publish(new CameraShakeEvent { Intensity = ShakeIntensity.Weak });
            //EventBus.Instance.Publish(new HitStopEvent { Duration = 0.3f });
        }

        if (_blinkRoutine == null && gameObject.activeInHierarchy)
            _blinkRoutine = StartCoroutine(BlinkRoutine());

        if (_currentHealth <= 0f)
        {
            Die(damageData.KnockbackForce);
        }
    }

    protected virtual void Die(Vector2 knockbackForce)
    {
        _hasHitWallAfterDeath = false;

        int weaponLayer = LayerMask.NameToLayer("Weapon");
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject.layer == weaponLayer)
            {
                child.SetParent(null);
            }
        }

        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic; 
            _rb.freezeRotation = false; 
            _rb.linearDamping = 1.5f;
            _rb.angularDamping = 1.0f;
            _rb.linearVelocity = Vector2.zero;
            _rb.AddForce(knockbackForce, ForceMode2D.Impulse); 
            float torqueDir = knockbackForce.x > 0 ? -1f : 1f;
            _rb.AddTorque(torqueDir * 40f, ForceMode2D.Impulse);
        }

        int corpseLayer = LayerMask.NameToLayer("Corpse");
        if (corpseLayer != -1) gameObject.layer = corpseLayer;

        StartCoroutine(DeathSequenceRoutine());
    }

    protected virtual IEnumerator DeathSequenceRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        if (_rb != null)
        {
            float timeout = 4f;
            while (_rb.linearVelocity.sqrMagnitude > 0.5f && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }
        yield return new WaitForSeconds(0.5f);
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
        if (_collider != null) _collider.enabled = false;

        float fadeTime = 1.2f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * 0.8f;
        Color startColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;

            if (_spriteRenderer != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                _spriteRenderer.color = c;
            }

            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    protected virtual void OnDisable()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.color = _originalColor;

        transform.rotation = Quaternion.identity;
        _blinkRoutine = null;
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsDead && !_hasHitWallAfterDeath && _rb != null && _rb.bodyType == RigidbodyType2D.Dynamic)
        {
            if (collision.relativeVelocity.sqrMagnitude > 25f)
            {
                _hasHitWallAfterDeath = true;

                if (EventBus.Instance != null)
                {
                    EventBus.Instance.Publish(new CameraShakeEvent { Intensity = ShakeIntensity.Medium });
                    EventBus.Instance.Publish(new HitStopEvent { Duration = 0.35f });
                }
            }
        }
    }

    protected virtual IEnumerator BlinkRoutine()
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.white;
            yield return new WaitForSecondsRealtime(0.05f);
            _spriteRenderer.color = _originalColor;
        }
        _blinkRoutine = null;
    }
}
