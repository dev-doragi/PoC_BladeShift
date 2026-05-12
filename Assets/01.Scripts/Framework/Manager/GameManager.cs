using UnityEngine;

/// <summary>
/// 게임의 전역 생명주기 및 최고 수준의 상태(GameState)를 관리하는 코어 매니저입니다.
/// </summary>
/// <remarks>
/// [주요 역할]
/// - Ready, Playing, Paused, GameOver, GameClear 상태 전환 및 관리
/// - 상태 변화에 따른 Time.timeScale 전역 제어
///
/// [이벤트 흐름]
/// - Subscribe: StageLoadedEvent, StageClearedEvent, StageFailedEvent
/// - Publish: GameStateChangedEvent
/// </remarks>

[DefaultExecutionOrder(-170)]
public class GameManager : Singleton<GameManager>
{
    public GameState CurrentState { get; private set; } = GameState.Ready;

    protected override void OnBootstrap()
    {
        Application.targetFrameRate = 60;

        if (EventBus.Instance != null)
        {
            EventBus.Instance.Subscribe<StageLoadedEvent>(OnStageLoaded);
            EventBus.Instance.Subscribe<StageClearedEvent>(OnStageCleared);
            EventBus.Instance.Subscribe<StageFailedEvent>(OnStageFailed);
        }
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<StageLoadedEvent>(OnStageLoaded);
            EventBus.Instance.Unsubscribe<StageClearedEvent>(OnStageCleared);
            EventBus.Instance.Unsubscribe<StageFailedEvent>(OnStageFailed);
        }
    }

    private void OnStageLoaded(StageLoadedEvent evt)
    {
        ChangeState(GameState.Playing);
    }

    private void OnStageCleared(StageClearedEvent evt)
    {
        if (evt.IsFinalStage)
        {
            SoundManager.Instance?.StopBGM();
            ChangeState(GameState.GameClear);
        }
    }

    private void OnStageFailed(StageFailedEvent evt)
    {
        SoundManager.Instance?.StopBGM();
        ChangeState(GameState.GameOver);
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        Debug.Log($"[GameManager] State Changed: {CurrentState} -> {newState}");
        CurrentState = newState;

        Time.timeScale = (CurrentState == GameState.Paused || CurrentState == GameState.GameOver || CurrentState == GameState.GameClear) ? 0f : 1f;

        EventBus.Instance.Publish(new GameStateChangedEvent { NewState = CurrentState });
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
