using UnityEngine;

/// <summary>
/// 상태 이벤트에 따라 공통 UI 패널 가시성을 제어하는 매니저입니다.
/// </summary>
/// <remarks>
/// GameState/InGameState를 기반으로 패널 표시를 결정합니다.
/// 버튼 핸들러는 PauseManager/SceneLoader 같은 공통 매니저로 위임합니다.
/// </remarks>
[DefaultExecutionOrder(-100)]
public class UIManager : Singleton<UIManager>
{
    [Header("Main UI Panels")]
    [SerializeField] private GameObject _inGamePanel;
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private GameObject _gameClearPanel;
    [SerializeField] private GameObject _pausePanel;

    [Header("Tutorial")]
    [SerializeField] private bool _autoShowInGamePanelOnPlaying = true;

    [Header("TutorialClear Panel Elements")]
    [SerializeField] private GameObject _gameClearText;
    [SerializeField] private GameObject _stageClearText;
    [SerializeField] private GameObject _resumeButton;

    private void OnEnable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Instance.Subscribe<InGameStateChangedEvent>(OnInGameStateChanged);
            EventBus.Instance.Subscribe<TutorialCompletedEvent>(OnTutorialCompleted);
        }
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Instance.Unsubscribe<InGameStateChangedEvent>(OnInGameStateChanged);
            EventBus.Instance.Unsubscribe<TutorialCompletedEvent>(OnTutorialCompleted);
        }
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        switch (evt.NewState)
        {
            case GameState.Playing:
                HideAllPanels();
                if (_autoShowInGamePanelOnPlaying && _inGamePanel != null) _inGamePanel.SetActive(true);
                break;
            case GameState.Paused:
                if (_pausePanel != null) _pausePanel.SetActive(true);
                break;
            case GameState.GameOver:
                if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
                break;
            case GameState.GameClear:
                ShowClearPanel(isAllGameClear: true);
                break;
            case GameState.Ready:
                HideAllPanels();
                break;
        }
    }

    private void OnInGameStateChanged(InGameStateChangedEvent evt)
    {
        if (evt.NewState == InGameState.StageCleared)
        {
            if (GameManager.Instance.CurrentState != GameState.GameClear)
            {
                ShowClearPanel(isAllGameClear: false);
            }
        }
    }

    private void ShowClearPanel(bool isAllGameClear)
    {
        if (_gameClearPanel == null) return;
        _gameClearPanel.SetActive(true);

        if (isAllGameClear)
        {
            if (_gameClearText != null) _gameClearText.SetActive(true);
            if (_stageClearText != null) _stageClearText.SetActive(false);
            if (_resumeButton != null) _resumeButton.SetActive(false);
        }
        else
        {
            if (_gameClearText != null) _gameClearText.SetActive(false);
            if (_stageClearText != null) _stageClearText.SetActive(true);
            if (_resumeButton != null) _resumeButton.SetActive(true);
        }
    }

    public void HideAllPanels()
    {
        if (_inGamePanel != null) _inGamePanel.SetActive(false);
        if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
        if (_gameClearPanel != null) _gameClearPanel.SetActive(false);
        if (_pausePanel != null) _pausePanel.SetActive(false);
    }

    public void ShowInGamePanel()
    {
        HideAllPanels();
        if (_inGamePanel != null) _inGamePanel.SetActive(true);
    }

    public void SetInGamePanelVisible(bool isVisible)
    {
        if (isVisible)
        {
            ShowInGamePanel();
            return;
        }

        if (_inGamePanel != null) _inGamePanel.SetActive(false);
    }

    // Button handlers

    public void OnPauseClicked()
    {
        PauseManager.Instance.TogglePause(true);
    }

    public void OnResumeClicked()
    {
        PauseManager.Instance.TogglePause(false);
    }

    public void OnGoToLobbyClicked()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.GoToLobby();
        }
    }

    public void OnRetryClicked()
    {
        HideAllPanels();
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.ReloadCurrentScene();
        }
    }

    public void OnNextStageClicked()
    {
        HideAllPanels();
        if (_inGamePanel != null) _inGamePanel.SetActive(true);
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.ReloadCurrentScene();
        }
    }

    public void OnGoToStageSelectClicked()
    {
        HideAllPanels();
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.GoToStageSelect();
        }
    }

    private void OnTutorialCompleted(TutorialCompletedEvent evt)
    {
        HideAllPanels();
        if (_resumeButton != null) _resumeButton.SetActive(false);

        Time.timeScale = 0f;
        if (InputReader.Instance != null) InputReader.Instance.SetInputBlocked(true);
    }

    protected override void OnBootstrap()
    {
        if (GameManager.Instance != null)
        {
            OnGameStateChanged(new GameStateChangedEvent { NewState = GameManager.Instance.CurrentState });
        }

        if (GameFlowManager.Instance != null)
        {
            OnInGameStateChanged(new InGameStateChangedEvent { NewState = GameFlowManager.Instance.CurrentInGameState });
        }
    }
}
