using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
[RequireComponent(typeof(WeaponMovement), typeof(WeaponCombat))]
[RequireComponent(typeof(WeaponSensor), typeof(WeaponView))]
public class WeaponController : MonoBehaviour
{
    [Header("1. Dual-Radius Settings")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float _controlRadius = 10f;

    [Header("2. Follow Settings")]
    [SerializeField] private float _followSmoothTime = 0.1f;

    [Header("3. Combat Settings")]
    [SerializeField] private float _slashDamage = 15f;
    [SerializeField] private float _slashRadius = 3.5f;
    [SerializeField] private float _slashDuration = 0.25f;
    [SerializeField] private float _thrustDamage = 30f;
    [SerializeField] private float _thrustRadius = 1.2f;
    [SerializeField] private float _thrustSpeed = 20f;
    [SerializeField] private float _slowMotionScale = 0.2f;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _wallAndEnvironmentLayer;

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private WeaponMovement _movement;
    private WeaponCombat _combat;
    private WeaponSensor _sensor;
    private WeaponView _view;

    private WeaponState _currentState = WeaponState.Grounded;
    private bool _isAttacking = false;
    private bool _isThrustAiming = false;
    private bool _isTimeSlowed = false;
    private Vector2 _fixedAimPos;
    private Vector2 _mouseStartPos;
    private Camera _mainCamera;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _movement = GetComponent<WeaponMovement>();
        _combat = GetComponent<WeaponCombat>();
        _sensor = GetComponent<WeaponSensor>();
        _view = GetComponent<WeaponView>();
        _mainCamera = Camera.main;

        if (_playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) _playerTransform = playerObject.transform;
        }

        _sensor.Configure(_playerTransform, _mainCamera, _controlRadius, 0f, 0f, 0f);
        _combat.Configure(_enemyLayer);
        _view.Configure(_sensor.GetPlayerTransform(), _controlRadius, 0f, 0f, _slashRadius);
        _movement.CacheRigidbody(_rb);

        ChangeState(WeaponState.Grounded);
    }

    private void OnEnable()
    {
        if (EventBus.Instance == null) return;
        EventBus.Instance.Subscribe<PrimaryAttackEvent>(OnPrimaryAttack);
        EventBus.Instance.Subscribe<SecondaryAttackEvent>(OnSecondaryAttack);
    }

    private void OnDisable()
    {
        if (EventBus.Instance == null) return;
        EventBus.Instance.Unsubscribe<PrimaryAttackEvent>(OnPrimaryAttack);
        EventBus.Instance.Unsubscribe<SecondaryAttackEvent>(OnSecondaryAttack);
        ResetTimeScale();
    }

    private void Update()
    {
        Vector2 mouseWorldPos = _sensor.GetMouseWorldPosition();
        bool showConnectionLine = _currentState == WeaponState.Controlled;

        _view.RenderConnectionLine(_playerTransform.position, transform.position, showConnectionLine);

        if (_isThrustAiming)
        {
            Vector2 dir = mouseWorldPos - (Vector2)transform.position;
            if (dir.sqrMagnitude > 0f)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
        }

        if (_isThrustAiming && _currentState == WeaponState.Controlled)
            _view.ShowTrajectory(transform.position, mouseWorldPos);
        else
            _view.HideTrajectory();

        if (!_isAttacking && _currentState == WeaponState.Grounded)
        {
            if (_sensor.ShouldAcquireControl(transform.position, mouseWorldPos))
                ChangeState(WeaponState.Controlled);
        }
        else if (!_isAttacking && _currentState == WeaponState.Controlled)
        {
            if (_sensor.ShouldReleaseControl(transform.position, mouseWorldPos))
                ChangeState(WeaponState.Grounded);
        }
    }

    private void FixedUpdate()
    {
        if (_isThrustAiming && _currentState == WeaponState.Controlled)
        {
            _rb.MovePosition(_fixedAimPos);
            return;
        }

        bool isMobileState = _currentState == WeaponState.Controlled || _currentState == WeaponState.Slashing;
        bool isThrusting = _currentState == WeaponState.Thrusting;

        if (isMobileState && !isThrusting && !_isThrustAiming)
        {
            _movement.FollowMouseHover(_sensor.GetMouseWorldPosition(), _followSmoothTime);
        }
    }

    private void ChangeState(WeaponState newState)
    {
        if (_currentState == newState) return;
        if (newState == WeaponState.Grounded) ResetTimeScale();

        _currentState = newState;
        EventBus.Instance?.Publish(new WeaponStateChangeEvent { NewState = _currentState });

        bool isGrounded = _currentState == WeaponState.Grounded;
        UpdateCollisionInteractions(isGrounded);

        switch (_currentState)
        {
            case WeaponState.Grounded:
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _collider.isTrigger = false;
                _movement.TransferVelocityToPhysics();
                _isAttacking = false;
                _isThrustAiming = false;
                _isTimeSlowed = false;
                _view.HideTrajectory();
                break;

            case WeaponState.Controlled:
            case WeaponState.Slashing:
            case WeaponState.Thrusting:
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _collider.isTrigger = true;
                break;
        }
    }

    private void UpdateCollisionInteractions(bool grounded)
    {
        int weaponLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int environmentLayer = LayerMask.NameToLayer("Environment");

        if (enemyLayer >= 0) Physics2D.IgnoreLayerCollision(weaponLayer, enemyLayer, !grounded);
        if (environmentLayer >= 0) Physics2D.IgnoreLayerCollision(weaponLayer, environmentLayer, !grounded);
    }

    #region [Combat: Slash & Thrust]
    private void OnPrimaryAttack(PrimaryAttackEvent evt)
    {
        if (_currentState != WeaponState.Controlled || !evt.IsStarted || _isAttacking) return;
        StartCoroutine(SlashRoutine());
    }

    private IEnumerator SlashRoutine()
    {
        _isAttacking = true;
        ChangeState(WeaponState.Slashing);
        _combat.PerformSlashDamage(transform.position, _slashRadius, _slashDamage, transform.eulerAngles.z);

        float elapsed = 0f;
        float startAngle = transform.eulerAngles.z;
        while (elapsed < _slashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _slashDuration);
            _view.SetRotationZ(startAngle + (360f * t));
            yield return null;
        }
        _view.SetRotationZ(startAngle + 360f);

        if (_currentState != WeaponState.Grounded) ChangeState(WeaponState.Controlled);
        _isAttacking = false;
    }

    private void OnSecondaryAttack(SecondaryAttackEvent evt)
    {
        if (_currentState != WeaponState.Controlled || _isAttacking) return;

        if (evt.IsStarted)
        {
            _fixedAimPos = transform.position;
            _mouseStartPos = _sensor.GetMouseWorldPosition();
            _isThrustAiming = true;
            _movement.StopFollow();
            ApplySlowMotion();
        }
        else
        {
            Vector2 mouseWorldPos = _sensor.GetMouseWorldPosition();
            float dragDistance = Vector2.Distance(_mouseStartPos, mouseWorldPos);

            _isThrustAiming = false;
            _view.HideTrajectory();
            ResetTimeScale();

            if (dragDistance < 0.5f)
            {
                ChangeState(WeaponState.Controlled);
                return;
            }

            Vector2 direction = (mouseWorldPos - _fixedAimPos).normalized;
            StartCoroutine(ThrustRoutine(direction));
        }
    }

    private IEnumerator ThrustRoutine(Vector2 direction)
    {
        ChangeState(WeaponState.Thrusting);
        _isAttacking = true;

        if (direction.sqrMagnitude <= 0f)
        {
            ChangeState(WeaponState.Grounded);
            _isAttacking = false;
            yield break;
        }

        HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        Vector2 thrustDirection = direction.normalized;
        float thrustSpeed = _thrustSpeed;
        int wallMask = _wallAndEnvironmentLayer.value != 0 ? _wallAndEnvironmentLayer.value : LayerMask.GetMask("Ground", "Wall", "Ceiling");

        while (true)
        {
            yield return new WaitForFixedUpdate();

            float moveDistance = thrustSpeed * Time.fixedDeltaTime;
            RaycastHit2D wallHit = Physics2D.Raycast(transform.position, thrustDirection, moveDistance, wallMask);
            if (wallHit.collider != null)
            {
                _movement.MoveThrust(thrustDirection * wallHit.distance);
                ChangeState(WeaponState.Grounded);
                _isAttacking = false;
                yield break;
            }

            _movement.MoveThrust(thrustDirection * moveDistance);
            _combat.PerformThrustDamage(transform.position, _thrustRadius, _thrustDamage, hitTargets);

            if (!_sensor.IsPlayerInRange(transform.position))
                break;

            yield return null;
        }

        ChangeState(WeaponState.Grounded);
        _isAttacking = false;
    }

    private void ApplySlowMotion()
    {
        if (_isTimeSlowed) return;

        _isTimeSlowed = true;
        Time.timeScale = _slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void ResetTimeScale()
    {
        if (!_isTimeSlowed && Time.timeScale == 1f) return;

        _isTimeSlowed = false;
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (_view == null) _view = GetComponent<WeaponView>();
        if (_view != null) _view.DrawGizmos();
    }
}