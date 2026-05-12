using UnityEngine;

/// <summary>
/// 게임 내 일시정지 요청을 수신하고 상태를 토글하는 매니저입니다.
/// </summary>
/// <remarks>
/// [주요 역할]
/// - 키보드 입력(Input System) 또는 UI 버튼에 의한 일시정지 트리거 처리
/// - GameManager에 상태 변경(Paused <-> Playing) 요청
///
/// [이벤트 흐름]
/// - Subscribe: PausePressedEvent, GameStateChangedEvent
/// </remarks>
[DefaultExecutionOrder(-140)]
public class PauseManager : Singleton<PauseManager>
{
    private bool _isPaused = false;

    private void OnEnable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Subscribe<PausePressedEvent>(OnPausePressed);
            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<PausePressedEvent>(OnPausePressed);
            EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }
    }

    private void OnPausePressed(PausePressedEvent evt)
    {
        GameState currentState = GameManager.Instance.CurrentState;

        if (currentState == GameState.Playing)
        {
            TogglePause(true);
        }
        else if (currentState == GameState.Paused)
        {
            TogglePause(false);
        }
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        if (evt.NewState != GameState.Paused)
        {
            _isPaused = false;
        }
    }

    public void TogglePause(bool pause)
    {
        _isPaused = pause;
        GameManager.Instance.ChangeState(_isPaused ? GameState.Paused : GameState.Playing);
    }
}
