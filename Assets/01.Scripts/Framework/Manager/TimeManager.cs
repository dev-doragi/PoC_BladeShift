using UnityEngine;

[DefaultExecutionOrder(-160)]
public class TimeManager : Singleton<TimeManager>
{
    private bool _isSystemPaused;
    private float _hitStopTimer;
    private float _slowMoTimer;
    private float _slowMoTargetTimeScale = 1f;

    [SerializeField, Range(0f, 0.1f)] private float _hitStopTimeScale = 0.2f;

    protected override void OnBootstrap()
    {
        _isSystemPaused = IsSystemPausedState(GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Ready);

        if (EventBus.Instance != null)
        {
            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Instance.Subscribe<HitStopEvent>(OnHitStop);
            EventBus.Instance.Subscribe<SlowMotionEvent>(OnSlowMotion);
        }

        CalculateTimeScale();
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Instance.Unsubscribe<HitStopEvent>(OnHitStop);
            EventBus.Instance.Unsubscribe<SlowMotionEvent>(OnSlowMotion);
        }
    }

    private void Update()
    {
        if (_hitStopTimer > 0f)
        {
            _hitStopTimer = Mathf.Max(0f, _hitStopTimer - Time.unscaledDeltaTime);
        }

        if (_slowMoTimer > 0f)
        {
            _slowMoTimer = Mathf.Max(0f, _slowMoTimer - Time.unscaledDeltaTime);
        }

        CalculateTimeScale();
    }

    public void ResetTime()
    {
        _hitStopTimer = 0f;
        _slowMoTimer = 0f;
        _slowMoTargetTimeScale = 1f;
        CalculateTimeScale();
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        _isSystemPaused = IsSystemPausedState(evt.NewState);
        CalculateTimeScale();
    }

    private void OnHitStop(HitStopEvent evt)
    {
        _hitStopTimer = Mathf.Max(0f, evt.Duration);
        CalculateTimeScale();
    }

    private void OnSlowMotion(SlowMotionEvent evt)
    {
        _slowMoTargetTimeScale = Mathf.Clamp(evt.TargetTimeScale, 0f, 1f);
        _slowMoTimer = Mathf.Max(0f, evt.Duration);
        CalculateTimeScale();
    }

    private void CalculateTimeScale()
    {
        float newTimeScale = 1f;

        if (_isSystemPaused)
        {
            newTimeScale = 0f;
        }
        else if (_hitStopTimer > 0f)
        {
            newTimeScale = _hitStopTimeScale;
        }
        else if (_slowMoTimer > 0f)
        {
            newTimeScale = _slowMoTargetTimeScale;
        }

        if (!Mathf.Approximately(Time.timeScale, newTimeScale))
        {
            Time.timeScale = newTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    private bool IsSystemPausedState(GameState state)
    {
        return state == GameState.Paused || state == GameState.GameOver || state == GameState.GameClear;
    }
}
