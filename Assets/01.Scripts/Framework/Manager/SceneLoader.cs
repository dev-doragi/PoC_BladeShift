using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 공통 씬 전환을 담당하는 프레임워크 매니저입니다.
/// </summary>
/// <remarks>
/// 프로젝트 전용 컨텍스트 없이 씬 이름 기반으로만 동작합니다.
/// 씬 이동 전 공통 상태(TimeScale, 입력 차단)를 초기화합니다.
/// </remarks>

[DefaultExecutionOrder(-180)]
public class SceneLoader : Singleton<SceneLoader>
{
    [Header("Scene Settings")]
    [SerializeField] private string _lobbySceneName = "01.LobbyScene";
    [SerializeField] private string _tutorialSceneName = "02.TutorialScene";
    [SerializeField] private string _stageSelectSceneName = "03.StageSelectScene";
    [SerializeField] private string _inGameSceneName = "04.InGameScene";

    private void ResetGlobalState()
    {
        Time.timeScale = 1f;

        if (InputReader.Instance != null)
        {
            InputReader.Instance.SetInputBlocked(false);
        }
    }

    private void PreserveBGMForNextSceneLoad()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.RequestSkipNextSceneLoadedBGMStop();
        }
    }

    public void GoToLobby()
    {
        ResetGlobalState();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Ready);
        }
        SceneManager.LoadScene(_lobbySceneName);
    }

    public void GoToStageSelect()
    {
        ResetGlobalState();
        PreserveBGMForNextSceneLoad();
        SceneManager.LoadScene(_stageSelectSceneName);
    }

    public void EnterTutorial()
    {
        ResetGlobalState();
        PreserveBGMForNextSceneLoad();
        SceneManager.LoadScene(_tutorialSceneName);
    }

    public void EnterInGameFromTutorial(int stageIndex)
    {
        ResetGlobalState();
        PreserveBGMForNextSceneLoad();
        SceneManager.LoadScene(_inGameSceneName);
    }

    public void EnterInGame(int stageIndex)
    {
        ResetGlobalState();
        SceneManager.LoadScene(_inGameSceneName);
    }

    public void ReloadCurrentScene()
    {
        ResetGlobalState();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Playing);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideAllPanels();
        }

        string currentSceneName = SceneManager.GetActiveScene().name;

        SceneManager.LoadScene(currentSceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
