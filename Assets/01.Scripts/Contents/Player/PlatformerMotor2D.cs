using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(GroundSensor2D))]
public class PlatformerMotor2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _maxSpeed = 8f;
    [SerializeField] private float _acceleration = 50f;
    [SerializeField] private float _deceleration = 50f;

    [Header("Jump & Gravity")]
    [SerializeField] private float _jumpForce = 12f;
    [SerializeField] private float _fallGravityMultiplier = 2.5f;
    [SerializeField] private float _lowJumpGravityMultiplier = 3f;
    [SerializeField] private float _apexBonus = 0.5f;

    [Header("Forgiveness")]
    [SerializeField] private float _coyoteTime = 0.15f;
    [SerializeField] private float _jumpBufferTime = 0.15f;

    private Rigidbody2D _rb;
    private GroundSensor2D _sensor;

    private Vector2 _velocity;
    private float _horizontalInput;
    private float _defaultGravityScale;

    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _isJumpHeld;
    private bool _jumpRequestPending; // 점프 실행 신호 보관용

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sensor = GetComponent<GroundSensor2D>();
        _defaultGravityScale = _rb.gravityScale;
    }

    private void Update()
    {
        // 타이머 감소는 매 프레임 정확하게 진행
        _jumpBufferTimer -= Time.deltaTime;

        if (_sensor.IsGrounded) _coyoteTimer = _coyoteTime;
        else _coyoteTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        _velocity = _rb.linearVelocity;

        // 1. 점프 로직 처리 (입력 버퍼와 코요테 타임 확인)
        if (_jumpRequestPending && _coyoteTimer > 0f)
        {
            ExecuteJump();
        }

        // 2. 이동 및 중력 처리
        HandleHorizontalMovement();
        HandleGravityAndJumpApex();

        // 3. 최종 속도 적용
        _rb.linearVelocity = _velocity;
    }

    public void SetHorizontalInput(float inputX) => _horizontalInput = inputX;

    public void RequestJump()
    {
        _jumpBufferTimer = _jumpBufferTime;
        _jumpRequestPending = true; // 점프 신호 접수
        _isJumpHeld = true;
    }

    public void CancelJump()
    {
        _isJumpHeld = false;
        _jumpRequestPending = false;
    }

    private void ExecuteJump()
    {
        _jumpRequestPending = false;
        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;
        _velocity.y = _jumpForce;
    }

    private void HandleHorizontalMovement()
    {
        float targetSpeed = _horizontalInput * _maxSpeed;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? _acceleration : _deceleration;
        _velocity.x = Mathf.MoveTowards(_velocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
    }

    private void HandleGravityAndJumpApex()
    {
        if (_sensor.IsGrounded)
        {
            _rb.gravityScale = _defaultGravityScale;
            return;
        }

        if (_velocity.y > 0 && !_isJumpHeld)
            _rb.gravityScale = _defaultGravityScale * _lowJumpGravityMultiplier;
        else if (Mathf.Abs(_velocity.y) < 2f)
            _rb.gravityScale = _defaultGravityScale * _apexBonus;
        else if (_velocity.y < 0f)
            _rb.gravityScale = _defaultGravityScale * _fallGravityMultiplier;
        else
            _rb.gravityScale = _defaultGravityScale;
    }
}