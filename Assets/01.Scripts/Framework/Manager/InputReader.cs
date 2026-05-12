using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-140)]
[RequireComponent(typeof(PlayerInput))]
/// <summary>
/// Input System 액션을 읽어 공통 입력 이벤트로 발행하는 매니저입니다.
/// </summary>
public class InputReader : Singleton<InputReader>
{
    [Header("Input Actions")]
    [SerializeField] private string _playerActionMapName = "Player";
    [SerializeField] private string _systemActionMapName = "System";
    [SerializeField] private string _clickActionName = "Click";
    [SerializeField] private string _rightClickActionName = "RightClick";
    [SerializeField] private string _rotateActionName = "Rotate";
    [SerializeField] private string _pointActionName = "Point";
    [SerializeField] private string _scrollActionName = "Scroll";
    [SerializeField] private string _pauseActionName = "Pause";

    [Header("Behavior")]
    [SerializeField] private bool _useGameStateInputGate = true;

    private PlayerInput _playerInput;

    private InputActionMap _playerMap;
    private InputActionMap _systemMap;

    private InputAction _clickAction;
    private InputAction _rightClickAction;
    private InputAction _rotateAction;
    private InputAction _pointAction;
    private InputAction _scrollAction;
    private InputAction _pauseAction;

    public bool IsPointerOverUI { get; private set; }

    private bool _isInputBlocked = false;

    public bool IsInputBlocked => _isInputBlocked;

    protected override void OnBootstrap()
    {
        _playerInput = GetComponent<PlayerInput>();

        if (_playerInput == null || _playerInput.actions == null)
        {
            Debug.LogError("[InputReader] PlayerInput 또는 InputActionAsset이 없습니다.");
            return;
        }

        _playerMap = _playerInput.actions.FindActionMap(_playerActionMapName, false);
        _systemMap = _playerInput.actions.FindActionMap(_systemActionMapName, false);

        if (_playerMap == null)
            Debug.LogWarning($"[InputReader] ActionMap '{_playerActionMapName}'을 찾지 못했습니다.");

        if (_systemMap == null)
            Debug.LogWarning($"[InputReader] ActionMap '{_systemActionMapName}'을 찾지 못했습니다.");

        _clickAction = _playerMap?.FindAction(_clickActionName, false);
        _rightClickAction = _playerMap?.FindAction(_rightClickActionName, false);
        _rotateAction = _playerMap?.FindAction(_rotateActionName, false);
        _pointAction = _playerMap?.FindAction(_pointActionName, false);
        _scrollAction = _playerMap?.FindAction(_scrollActionName, false);
        _pauseAction = _systemMap?.FindAction(_pauseActionName, false);

        BindEvents();
        _playerMap?.Enable();
        _systemMap?.Enable();

        if (_useGameStateInputGate)
        {
            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }
    }

    private void OnDisable()
    {
        UnbindEvents();

        if (_useGameStateInputGate)
        {
            EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        _playerMap?.Disable();
        _systemMap?.Disable();
    }


    private void Update()
    {
        if (EventSystem.current != null)
        {
            IsPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        }
    }

    private void BindEvents()
    {
        if (_clickAction != null)
        {
            _clickAction.started += OnClickStarted;
            _clickAction.canceled += OnClickCanceled;
        }

        if (_rightClickAction != null)
        {
            _rightClickAction.started += OnRightClickStarted;
            _rightClickAction.canceled += OnRightClickCanceled;
        }

        if (_rotateAction != null)
        {
            _rotateAction.performed += OnRotatePerformed;
        }

        if (_scrollAction != null)
        {
            _scrollAction.performed += OnScrollPerformed;
        }

        if (_pauseAction != null)
        {
            _pauseAction.performed += OnPausePerformed;
        }
    }

    private void UnbindEvents()
    {
        if (_clickAction != null)
        {
            _clickAction.started -= OnClickStarted;
            _clickAction.canceled -= OnClickCanceled;
        }

        if (_rightClickAction != null)
        {
            _rightClickAction.started -= OnRightClickStarted;
            _rightClickAction.canceled -= OnRightClickCanceled;
        }

        if (_rotateAction != null)
        {
            _rotateAction.performed -= OnRotatePerformed;
        }

        if (_scrollAction != null)
        {
            _scrollAction.performed -= OnScrollPerformed;
        }

        if (_pauseAction != null)
        {
            _pauseAction.performed -= OnPausePerformed;
        }
    }

    private void OnClickStarted(InputAction.CallbackContext _)
    {
        PublishIfAllowed(new ClickEvent { IsStarted = true });
    }

    private void OnClickCanceled(InputAction.CallbackContext _)
    {
        PublishIfAllowed(new ClickEvent { IsStarted = false });
    }

    private void OnRightClickStarted(InputAction.CallbackContext _)
    {
        PublishRightClickIfAllowed(true);
    }

    private void OnRightClickCanceled(InputAction.CallbackContext _)
    {
        PublishRightClickIfAllowed(false);
    }

    private void OnRotatePerformed(InputAction.CallbackContext _)
    {
        PublishIfAllowed(new RotateEvent());
    }

    private void OnScrollPerformed(InputAction.CallbackContext ctx)
    {
        float scrollValue = ctx.ReadValue<Vector2>().y;
        if (Mathf.Abs(scrollValue) > 0.01f)
        {
            PublishIfAllowed(new ScrollEvent { Delta = scrollValue });
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext _)
    {
        if (_isInputBlocked) return;
        EventBus.Instance.Publish(new PausePressedEvent());
    }

    private void PublishIfAllowed<T>(T evt) where T : struct
    {
        if (_isInputBlocked) return;
        EventBus.Instance.Publish(evt);
    }

    private void PublishRightClickIfAllowed(bool isStarted)
    {
        if (_isInputBlocked) return;

        EventBus.Instance.Publish(new RightClickEvent { IsStarted = isStarted });
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        if (_playerMap == null) return;

        if (evt.NewState == GameState.Playing) _playerMap.Enable();
        else _playerMap.Disable();
    }

    public void SetInputBlocked(bool blocked)
    {
        _isInputBlocked = blocked;
    }

    public Vector2 GetMousePosition() => _pointAction?.ReadValue<Vector2>() ?? Vector2.zero;
    public Vector2 GetMouseDelta() => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
}
