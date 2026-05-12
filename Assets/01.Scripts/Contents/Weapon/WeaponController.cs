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
    [SerializeField] private float _mouseCaptureRadius = 2.5f;

    [Header("2. Hover & Follow Settings")]
    [SerializeField] private float _followSmoothTime = 0.12f;
    [SerializeField] private float _breakMouseSpeed = 100f;

    [Header("3. Combat Settings")]
    [SerializeField] private float _slashDamage = 15f;
    [SerializeField] private float _slashRadius = 3.5f;
    [SerializeField] private float _slashDuration = 0.25f;
    [SerializeField] private float _thrustDamage = 30f;
    [SerializeField] private float _thrustRadius = 1.2f;
    [SerializeField] private float _thrustSpeed = 25f;
    [SerializeField] private float _slowMotionScale = 0.2f;
    [SerializeField] private LayerMask _enemyLayer;

    private Rigidbody2D _rb;
    private Collider2D _collider; // 물리-트리거 스위칭용
    private WeaponMovement _movement;
    private WeaponCombat _combat;
    private WeaponSensor _sensor;
    private WeaponView _view;

    private WeaponState _currentState = WeaponState.Grounded;
    private bool _isAttacking = false;
    private bool _isThrustAiming = false;
    private Camera _mainCamera;
    private float _mouseCaptureMaintainRadius;

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

        _mouseCaptureMaintainRadius = _mouseCaptureRadius * 1.2f;

        // 모듈 초기화
        _sensor.Configure(_playerTransform, _mainCamera, _controlRadius, _mouseCaptureRadius, _mouseCaptureMaintainRadius, _breakMouseSpeed);
        _combat.Configure(_enemyLayer);
        _view.Configure(_sensor.GetPlayerTransform(), _controlRadius, _mouseCaptureRadius, _mouseCaptureMaintainRadius, _slashRadius);
        _movement.CacheRigidbody(_rb);

        // [핵심] 시작 상태 설정 (isTrigger = false 로직 포함)
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

        if (_isThrustAiming)
        {
            Vector2 dir = (mouseWorldPos - (Vector2)transform.position).normalized;
            if (dir.sqrMagnitude > 0f)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
        }

        // 1. 조준선 표시 로직
        if (_isThrustAiming && _currentState == WeaponState.Controlled)
            _view.ShowTrajectory(transform.position, mouseWorldPos);
        else
            _view.HideTrajectory();

        // 2. 상태 전환 트리거 (Sensor 모듈 위임)
        if (_currentState == WeaponState.Grounded)
        {
            if (_sensor.ShouldAcquireControl(transform.position, mouseWorldPos))
                ChangeState(WeaponState.Controlled);
        }
        else if (_currentState == WeaponState.Controlled && !_isAttacking)
        {
            if (_sensor.ShouldReleaseControl(transform.position, mouseWorldPos))
                ChangeState(WeaponState.Grounded);
        }
    }

    private void FixedUpdate()
    {
        if ((_currentState == WeaponState.Controlled || _currentState == WeaponState.Slashing) && !_isThrustAiming)
            _movement.FollowMouseHover(_sensor.GetMouseWorldPosition(), _followSmoothTime);
    }

    private void ChangeState(WeaponState newState)
    {
        if (_currentState == newState) return;
        if (newState == WeaponState.Grounded) ResetTimeScale();

        _currentState = newState;
        EventBus.Instance?.Publish(new WeaponStateChangeEvent { NewState = _currentState });

        switch (_currentState)
        {
            case WeaponState.Grounded:
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _collider.isTrigger = false; // [핵심] 바닥에 충돌 가능하게 변경
                _movement.TransferVelocityToPhysics();
                _isAttacking = false;
                _isThrustAiming = false;
                _view.HideTrajectory();
                break;

            case WeaponState.Controlled:
            case WeaponState.Slashing:
            case WeaponState.Thrusting:
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _collider.isTrigger = true; // [핵심] 조종/공격 시엔 모든 것을 통과(트리거)
                break;
        }
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
        Debug.Log($"Secondary Attack Event Received: {evt.IsStarted}, Current State: {_currentState}");
        if (_currentState != WeaponState.Controlled || _isAttacking) return;

        if (evt.IsStarted)
        {
            _isThrustAiming = true;
            Time.timeScale = _slowMotionScale; // 경고 해결: 고정값 0.2f 대신 변수 사용
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
        else
        {
            _isThrustAiming = false;
            _view.HideTrajectory();
            ResetTimeScale();
            Vector2 mousePos = _sensor.GetMouseWorldPosition();
            Vector2 fixedWeaponPosition = transform.position;
            Vector2 direction = (mousePos - fixedWeaponPosition).normalized;
            StartCoroutine(ThrustRoutine(direction));
        }
    }

    private IEnumerator ThrustRoutine(Vector2 direction)
    {
        _isAttacking = true;
        ChangeState(WeaponState.Thrusting);

        float traveledDistance = 0f;
        float maxThrustDist = _controlRadius * 1.5f;
        HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        int wallMask = LayerMask.GetMask("Wall", "Environment");

        while (traveledDistance < maxThrustDist)
        {
            Vector2 moveStep = direction * _thrustSpeed * Time.deltaTime;

            if (wallMask != 0)
            {
                RaycastHit2D wallHit = Physics2D.Raycast(transform.position, direction, moveStep.magnitude, wallMask);
                if (wallHit.collider != null)
                {
                    break;
                }
            }

            _movement.MoveThrust(moveStep);
            traveledDistance += moveStep.magnitude;

            _combat.PerformThrustDamage(transform.position, _thrustRadius, _thrustDamage, hitTargets);

            if (!_sensor.IsPlayerInRange(transform.position)) break;
            yield return null;
        }

        ChangeState(WeaponState.Grounded);
        _isAttacking = false;
    }

    private void ResetTimeScale() { Time.timeScale = 1.0f; Time.fixedDeltaTime = 0.02f; }
    #endregion

    private void OnDrawGizmos()
    {
        if (_view == null) _view = GetComponent<WeaponView>();
        if (_view != null) _view.DrawGizmos();
    }
}