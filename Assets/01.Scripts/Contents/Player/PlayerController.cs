using UnityEngine;

[RequireComponent(typeof(PlatformerMotor2D))]
public class PlayerController : MonoBehaviour
{
    private PlatformerMotor2D _motor;
    [SerializeField] private float _controlRadius = 10f;
    [SerializeField] private bool _showControlRadiusGizmo = true;

    public float ControlRadius => _controlRadius;

    private void Awake()
    {
        _motor = GetComponent<PlatformerMotor2D>();
        _controlRadius = Mathf.Max(0f, _controlRadius);
    }

    private void OnEnable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Subscribe<MoveInputEvent>(OnMoveInput);
            EventBus.Instance.Subscribe<JumpInputEvent>(OnJumpInput);
        }
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<MoveInputEvent>(OnMoveInput);
            EventBus.Instance.Unsubscribe<JumpInputEvent>(OnJumpInput);
        }
    }

    private void OnMoveInput(MoveInputEvent evt)
    {
        _motor.SetHorizontalInput(evt.Direction.x);
    }

    private void OnJumpInput(JumpInputEvent evt)
    {
        if (evt.IsStarted) _motor.RequestJump();
        else _motor.CancelJump();
    }

    private void OnDrawGizmos()
    {
        if (!_showControlRadiusGizmo) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _controlRadius);
    }
}
