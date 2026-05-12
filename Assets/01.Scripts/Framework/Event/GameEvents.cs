using UnityEngine;

/// <summary>
/// 씬/상태/입력/오디오 흐름에서 사용하는 공통 이벤트 정의입니다.
/// </summary>
public struct StageLoadedEvent
{
    public int StageIndex;
}

public struct WaveStartedEvent
{
    public int StageIndex;
    public int WaveIndex;
}

public struct WaveEndedEvent
{
    public int StageIndex;
    public int WaveIndex;
    public bool IsWin;
    public bool IsFinalWave;
}

public struct StageClearedEvent
{
    public int StageIndex;
    public bool IsFinalStage;
}

public struct StageFailedEvent
{
    public int StageIndex;
}

public struct GameStateChangedEvent
{
    public GameState NewState;
}

public struct InGameStateChangedEvent
{
    public InGameState NewState;
}

public struct PlaySFXEvent
{
    public AudioClip Clip;
    public float Volume;
}

public struct ClickEvent
{
    public bool IsStarted;
}

public struct RightClickEvent
{
    public bool IsStarted;
}

public struct RotateEvent { }

public struct ScrollEvent
{
    public float Delta;
}

public struct PausePressedEvent { }

public struct WaveWaitInterruptedEvent { }

public struct WaveWaitTimerTickEvent
{
    public float RemainingTime;
}

public struct TutorialCompletedEvent
{
    public int RewardStageIndex;
}

public struct CameraManipulationEvent { }
