using System;
using System.Collections.Generic;
using UnityEngine;

 [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
 [RequireComponent(typeof(WeaponMovement), typeof(WeaponCombat))]
 [RequireComponent(typeof(WeaponSensor), typeof(WeaponView))]
 [RequireComponent(typeof(WeaponCapture))]
public class WeaponController : MonoBehaviour
{
    [Header("1. Dual-Radius Settings")]
    [SerializeField] private Transform _playerTransform;

    [Header("2. Combat Settings")]
    [SerializeField] private float _slowMotionScale = 0.2f;
    [SerializeField] private LayerMask _wallAndEnvironmentLayer;
    [SerializeField] private float _thrustDragThreshold = 2.0f;

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private WeaponMovement _movement;
    private WeaponCombat _combat;
    private WeaponSensor _sensor;
    private WeaponView _view;
    private HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
    private WeaponCapture _capture;
    private PlayerController _playerController;
    private float _controlRadius;

    private WeaponState _currentState = WeaponState.Grounded;
    private bool _isAttacking = false;
    private bool _isThrustAiming = false;
    private bool _isTimeSlowed = false;
    private Vector3 _originalScale;
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
        _originalScale = transform.localScale;
        _capture = GetComponent<WeaponCapture>();

        if (_playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) _playerTransform = playerObject.transform;
        }

        if (_playerTransform == null)
        {
            Debug.LogError("[WeaponController] Player object missing.");
            enabled = false;
            return;
        }

        _playerController = _playerTransform.GetComponent<PlayerController>();
        if (_playerController == null)
        {
            Debug.LogError("[WeaponController] PlayerController missing on player object.");
            enabled = false;
            return;
        }

        _controlRadius = _playerController.ControlRadius;

        _sensor.Configure(_playerTransform, _mainCamera, _controlRadius, 1.0f, 1.5f, 0f);
        _view.Configure(_sensor.GetPlayerTransform(), _controlRadius, 0f, 0f, _combat.SlashRadius);
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
        bool showConnectionLine = _currentState == WeaponState.Controlled || _isAttacking;

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
            if (_sensor.ShouldAcquireControl(transform.position, mouseWorldPos, _wallAndEnvironmentLayer))
                ChangeState(WeaponState.Controlled);
        }
        else if (!_isAttacking && _currentState == WeaponState.Controlled)
        {
            if (_sensor.ShouldReleaseControl(transform.position, mouseWorldPos, _wallAndEnvironmentLayer))
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

        if (_currentState == WeaponState.Slashing)
        {
            _combat.TryTickSpinDamage(transform.position, transform.eulerAngles.z);
            _combat.DefendProjectiles(transform.position);
            _movement.ApplySpinRotation(_combat.SpinSpeed);
        }


        bool hasEnemy = _capture.GetCapturedEnemies().Count > 0;


        bool canHover = _currentState == WeaponState.Controlled || 
                        _currentState == WeaponState.Slashing || 
                        (_currentState == WeaponState.Pinned && hasEnemy);

        if (canHover && !_isThrustAiming)
        {
            _movement.HandleHoverMovement(_sensor.GetClampedTargetPosition(_wallAndEnvironmentLayer), _currentState == WeaponState.Slashing, _wallAndEnvironmentLayer);
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

            case WeaponState.Pinned:
            case WeaponState.PinningFlight:
            case WeaponState.Returning:
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _collider.isTrigger = true;
                break;

            case WeaponState.Controlled:
            case WeaponState.Slashing:
            case WeaponState.Thrusting:
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
                _collider.isTrigger = true;
                break;
        }
    }

    private void UpdateCollisionInteractions(bool grounded)
    {
        int weaponLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int environmentLayer = LayerMask.NameToLayer("Environment");

        if (enemyLayer >= 0) Physics2D.IgnoreLayerCollision(weaponLayer, enemyLayer, true);
        if (environmentLayer >= 0) Physics2D.IgnoreLayerCollision(weaponLayer, environmentLayer, false);
    }

    private void OnPrimaryAttack(PrimaryAttackEvent evt)
    {
        if (_isThrustAiming) return;

        if (_currentState == WeaponState.Pinned)
        {
            if (_isAttacking) return; // 방어 코드: 피니셔 연출 중 중복 입력 차단

            if (evt.IsStarted)
            {
                if (!_sensor.IsPlayerInRange(transform.position)) return;

                bool hasVictim = _capture.GetCapturedEnemies().Count > 0;

                if (hasVictim)
                {
                    ExecuteSpinFinisher();
                }
                else
                {
                    UnpinAndReturn(); 
                }
                return;
            }
        }

        if (evt.IsStarted)
        {
            if (_currentState != WeaponState.Controlled || _isAttacking) return;
            _isAttacking = true;
            _combat.ResetTickTimer();
            ChangeState(WeaponState.Slashing);
            return;
        }

        _isAttacking = false;
        if (_currentState == WeaponState.Slashing) ChangeState(WeaponState.Controlled);
    }

    private void OnSecondaryAttack(SecondaryAttackEvent evt)
    {
        if (evt.IsStarted)
        {
        if (_currentState == WeaponState.Pinned)
        {
            if (_isAttacking) return; // 방어 코드: 피니셔 연출 중 중복 입력 차단

            if (!_sensor.IsPlayerInRange(transform.position)) return;

            if (_capture.GetCapturedEnemies().Count > 0)
            {
                ExecuteSpinFinisher();
            }
            else
            {
                UnpinAndReturn();
            }
            return;
        }

            if (_currentState != WeaponState.Controlled || _isAttacking) return;

            _fixedAimPos = transform.position;
            _mouseStartPos = _sensor.GetMouseWorldPosition();
            _isThrustAiming = true;
            _movement.StopFollow();
            ApplySlowMotion();
        }
        else
        {
            if (!_isThrustAiming || _currentState == WeaponState.Returning) return;

            _isThrustAiming = false;

            Vector2 mouseWorldPos = _sensor.GetMouseWorldPosition();
            float dragDistance = Vector2.Distance(_mouseStartPos, mouseWorldPos);

            _view.HideTrajectory();
            ResetTimeScale();

            if (dragDistance < _thrustDragThreshold)
            {
                ChangeState(WeaponState.Controlled);
                return;
            }

            Vector2 direction = (mouseWorldPos - _fixedAimPos).normalized;
            StartPinSequence(direction);
        }
    }

    private void StartPinSequence(Vector2 direction)
    {
        _isThrustAiming = false;
        _view.HideTrajectory();
        ResetTimeScale();
        EventBus.Instance?.Publish(new HitStopEvent { Duration = 0.15f });
        _hitTargets.Clear();
        ChangeState(WeaponState.PinningFlight);
        _isAttacking = true;
        _movement.ExecutePinFlight(direction, _combat.PinSpeed, _combat.EnemyLayer, _wallAndEnvironmentLayer, targetTransform =>
        {
            if (!_combat.PerformPinDamage(targetTransform, transform.position, direction, _hitTargets)) return false;
            if (targetTransform != null)
            {
                _capture.BindEnemy(targetTransform);
            }
            return false;
        }, 
        hitTransform => 
        {
            EventBus.Instance?.Publish(new CameraShakeEvent { Intensity = ShakeIntensity.Weak });
            _isAttacking = false;
            ChangeState(WeaponState.Pinned);
        });
    }

    private void UnpinAndReturn()
    {
        _capture.UnbindAll();
        transform.SetParent(null);
        transform.localScale = _originalScale;
        transform.rotation = Quaternion.Euler(0f, 0f, transform.eulerAngles.z);
        StartReturnSequence();
    }

    private void StartReturnSequence()
    {
        _hitTargets.Clear();
        ChangeState(WeaponState.Returning);

        _movement.ExecuteReturn(
            () => _sensor.GetClampedTargetPosition(_wallAndEnvironmentLayer),
            _controlRadius,
            (currentPos, mousePos) => _sensor.IsMouseHovering(currentPos, mousePos),
            isSuccess =>
            {
                _isAttacking = false;
                ChangeState(WeaponState.Controlled);
            });
    }

    private void ApplySlowMotion()
    {
        if (_isTimeSlowed) return;
        _isTimeSlowed = true;
        EventBus.Instance?.Publish(new SlowMotionEvent { TargetTimeScale = _slowMotionScale, Duration = 999f });
    }

    private void ResetTimeScale()
    {
        if (!_isTimeSlowed) return;
        _isTimeSlowed = false;
        TimeManager.Instance?.ResetTime();
    }

    private void OnDrawGizmos()
    {
        if (_view == null) _view = GetComponent<WeaponView>();
        if (_view != null) _view.DrawGizmos();
    }
    private void ExecuteSpinFinisher()
    {
        _isAttacking = true;
        EventBus.Instance?.Publish(new HitStopEvent { Duration = 0.2f });
        EventBus.Instance?.Publish(new CameraShakeEvent { Intensity = ShakeIntensity.Strong });
        Vector2 pivot = _sensor.GetMouseWorldPosition();
        float radius = Vector2.Distance(pivot, transform.position);
        radius = Mathf.Max(radius, _combat.SlashRadius * 1.8f);
        var captured = _capture.GetCapturedEnemies();
        List<Transform> pinnedTargets = new List<Transform>(captured.Count);
        foreach (var enemy in captured)
        {
            if (enemy != null)
                pinnedTargets.Add(enemy.transform);
        }

        _movement.ExecuteOrbitFinisher(
            pivot,
            radius,
            0.35f,
            () =>
            {
                _combat.PerformSpinFinisher(transform.position, pinnedTargets);
                _capture.UnbindAll();
            },
            () =>
            {
                _isAttacking = false;
                ChangeState(WeaponState.Controlled);
            });
    }
}
