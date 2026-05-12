using UnityEngine;
using System.Collections;

[DefaultExecutionOrder(-150)]
/// <summary>
/// 인게임 세부 상태 흐름(Prepare/Wave/Result)을 이벤트 기반으로 관리합니다.
/// </summary>
public class GameFlowManager : Singleton<GameFlowManager>
{
    [Header("Wave Wait Settings")]
    [SerializeField] private float _defaultWaveWaitDuration = 15f;

    private Coroutine _waveWaitCoroutine;
    private Coroutine _transitionCoroutine;

    public InGameState CurrentInGameState { get; private set; } = InGameState.None;
    public float CurrentWaveWaitRemainingTime { get; private set; } = 0f;
    public bool IsWaitingForNextWave => _waveWaitCoroutine != null;
    public float DefaultWaveWaitDuration => _defaultWaveWaitDuration;

    protected override void OnBootstrap()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Ready)
        {
            ChangeFlowState(InGameState.None);
        }

        if (EventBus.Instance != null)
        {
            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGlobalStateChanged);
            EventBus.Instance.Subscribe<StageLoadedEvent>(OnStageLoaded);
            EventBus.Instance.Subscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Instance.Subscribe<WaveEndedEvent>(OnWaveEnded);
        }
    }

    private void OnDisable()
    {
        StopAllFlowCoroutines(resetTimeScale: true);

        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGlobalStateChanged);
            EventBus.Instance.Unsubscribe<StageLoadedEvent>(OnStageLoaded);
            EventBus.Instance.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Instance.Unsubscribe<WaveEndedEvent>(OnWaveEnded);
        }
    }

    private void OnGlobalStateChanged(GameStateChangedEvent evt)
    {
        if (evt.NewState == GameState.Ready || evt.NewState == GameState.GameOver || evt.NewState == GameState.GameClear)
        {
            StopAllFlowCoroutines(resetTimeScale: false);
            ChangeFlowState(InGameState.None);
        }
    }

    private void OnStageLoaded(StageLoadedEvent evt)
    {
        StopWaveWaitRoutine(publishInterruptedEvent: false);
        StopTransitionRoutine(resetTimeScale: false);

        ChangeFlowState(InGameState.Prepare);

        CurrentWaveWaitRemainingTime = 0f;
    }

    private void OnWaveStarted(WaveStartedEvent evt)
    {
        StopWaveWaitRoutine(publishInterruptedEvent: false);
        ChangeFlowState(InGameState.WavePlaying);
    }

    private void OnWaveEnded(WaveEndedEvent evt)
    {
        if (CurrentInGameState != InGameState.WavePlaying)
        {
            return;
        }

        ChangeFlowState(InGameState.WaveEnded);

        if (!evt.IsWin)
        {
            StartTransitionRoutine(evt.IsWin);
            return;
        }

        if (evt.IsFinalWave)
        {
            StartTransitionRoutine(evt.IsWin);
            return;
        }

        float waitDuration = GetNextWaveWaitDuration();
        ChangeFlowState(InGameState.Prepare);
        StartWaveWait(waitDuration);
    }

    private float GetNextWaveWaitDuration()
    {
        return Mathf.Max(0f, _defaultWaveWaitDuration);
    }

    public void RequestImmediateNextWaveStart()
    {
        if (!IsWaitingForNextWave)
        {
            Debug.LogWarning("[GameFlowManager] 현재 즉시 시작할 웨이브 대기 상태가 아닙니다.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[GameFlowManager] GameManager가 없어 즉시 시작 요청을 처리할 수 없습니다.");
            return;
        }

        if (GameManager.Instance.CurrentState != GameState.Playing)
        {
            Debug.LogWarning("[GameFlowManager] 게임이 Playing 상태가 아니어서 즉시 시작 요청을 무시합니다.");
            return;
        }

        StopWaveWaitRoutine(publishInterruptedEvent: true);
        ChangeFlowState(InGameState.WavePlaying);
    }

    private void StartWaveWait(float duration)
    {
        StopWaveWaitRoutine(publishInterruptedEvent: false);
        CurrentWaveWaitRemainingTime = Mathf.Max(0f, duration);

        if (CurrentWaveWaitRemainingTime <= 0f)
        {
            PublishWaveWaitTick(0f);
            ChangeFlowState(InGameState.WavePlaying);
            return;
        }

        _waveWaitCoroutine = StartCoroutine(WaveWaitRoutine(CurrentWaveWaitRemainingTime));
    }

    private IEnumerator WaveWaitRoutine(float duration)
    {
        float remainingTime = Mathf.Max(0f, duration);
        int lastReportedSecond = Mathf.CeilToInt(remainingTime);

        PublishWaveWaitTick(remainingTime);

        while (remainingTime > 0f)
        {
            if (!isActiveAndEnabled)
            {
                StopWaveWaitRoutine(publishInterruptedEvent: false);
                yield break;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("[GameFlowManager] GameManager가 없어 웨이브 대기를 중단합니다.");
                StopWaveWaitRoutine(publishInterruptedEvent: false);
                yield break;
            }

            if (CurrentInGameState != InGameState.Prepare)
            {
                StopWaveWaitRoutine(publishInterruptedEvent: false);
                yield break;
            }

            if (GameManager.Instance.CurrentState != GameState.Playing)
            {
                yield return null;
                continue;
            }

            remainingTime -= Time.unscaledDeltaTime;
            CurrentWaveWaitRemainingTime = Mathf.Max(0f, remainingTime);

            int currentSecond = Mathf.CeilToInt(CurrentWaveWaitRemainingTime);
            if (currentSecond != lastReportedSecond)
            {
                lastReportedSecond = currentSecond;
                PublishWaveWaitTick(CurrentWaveWaitRemainingTime);
            }

            yield return null;
        }

        StopWaveWaitRoutine(publishInterruptedEvent: false);
        PublishWaveWaitTick(0f);
        ChangeFlowState(InGameState.WavePlaying);
    }

    private void StartTransitionRoutine(bool isWin)
    {
        StopTransitionRoutine(resetTimeScale: false);
        _transitionCoroutine = StartCoroutine(SlowMotionTransitionRoutine(isWin));
    }

    private void StopWaveWaitRoutine(bool publishInterruptedEvent)
    {
        bool hadPendingWave = _waveWaitCoroutine != null || CurrentWaveWaitRemainingTime > 0f;

        if (_waveWaitCoroutine != null)
        {
            StopCoroutine(_waveWaitCoroutine);
            _waveWaitCoroutine = null;
        }
        CurrentWaveWaitRemainingTime = 0f;

        if (publishInterruptedEvent && hadPendingWave && EventBus.Instance != null)
        {
            EventBus.Instance.Publish(new WaveWaitInterruptedEvent());
            EventBus.Instance.Publish(new WaveWaitTimerTickEvent { RemainingTime = 0f });
        }
    }

    private void StopTransitionRoutine(bool resetTimeScale)
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }

        if (resetTimeScale)
        {
            Time.timeScale = 1f;
        }
    }

    private void StopAllFlowCoroutines(bool resetTimeScale)
    {
        StopWaveWaitRoutine(publishInterruptedEvent: false);
        StopTransitionRoutine(resetTimeScale);
    }

    private void ChangeFlowState(InGameState newState)
    {
        if (CurrentInGameState == newState)
        {
            return;
        }

        if (newState != InGameState.None)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[GameFlowManager] GameManager가 없어 인게임 상태를 변경할 수 없습니다.");
                return;
            }

            if (GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }
        }

        InGameState previousState = CurrentInGameState;
        CurrentInGameState = newState;

        Debug.Log($"[GameFlowManager] Flow State: {previousState} -> {CurrentInGameState}");

        if (EventBus.Instance != null)
        {
            EventBus.Instance.Publish(new InGameStateChangedEvent { NewState = CurrentInGameState });
        }
    }

    private void PublishWaveWaitTick(float remainingTime)
    {
        CurrentWaveWaitRemainingTime = Mathf.Max(0f, remainingTime);

        if (EventBus.Instance != null)
        {
            EventBus.Instance.Publish(new WaveWaitTimerTickEvent
            {
                RemainingTime = CurrentWaveWaitRemainingTime
            });
        }
    }

    private IEnumerator SlowMotionTransitionRoutine(bool isWin)
    {
        Time.timeScale = 0.3f;
        yield return new WaitForSecondsRealtime(1.5f);
        Time.timeScale = 1f;

        _transitionCoroutine = null;

        if (isWin)
        {
            ChangeFlowState(InGameState.StageCleared);

            if (EventBus.Instance != null)
            {
                EventBus.Instance.Publish(new StageClearedEvent
                {
                    StageIndex = 0,
                    IsFinalStage = true
                });
            }
        }
        else
        {
            ChangeFlowState(InGameState.StageFailed);

            if (EventBus.Instance != null)
            {
                EventBus.Instance.Publish(new StageFailedEvent
                {
                    StageIndex = 0
                });
            }
        }
    }
}
