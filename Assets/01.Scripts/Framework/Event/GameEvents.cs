using UnityEngine;

/// <summary>
/// [BladeShift PoC] 씬/상태/입력/오디오/전투 흐름에서 사용하는 공통 이벤트 정의입니다.
/// </summary>

#region [1. Core State Events (코어 상태)]
public struct GameStateChangedEvent
{
    public GameState NewState;
}

public struct InGameStateChangedEvent
{
    public InGameState NewState;
}

public struct SlowMotionEvent
{
    public float TargetTimeScale;
    public float Duration;
}
#endregion

#region [2. Stage & Wave Flow Events (스테이지 및 웨이브 흐름)]
public struct StageLoadedEvent
{
    public int StageIndex;
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

public struct WaveWaitInterruptedEvent { }

public struct WaveWaitTimerTickEvent
{
    public float RemainingTime;
}
#endregion

#region [3. Input Events (조작 및 입력)]
public struct MoveInputEvent
{
    public Vector2 Direction;
}

public struct JumpInputEvent
{
    public bool IsStarted;
}

public struct PrimaryAttackEvent
{
    public bool IsStarted; // true: 클릭/홀드 시작, false: 해제
}

public struct SecondaryAttackEvent
{
    public bool IsStarted; // true: 홀드 시작(태깅), false: 해제(사출)
}

public struct RotateEvent { }

public struct ScrollEvent
{
    public float Delta;
}

public struct PausePressedEvent { }
#endregion

#region [4. Combat & Weapon Events (전투 및 메카닉)]
public struct WeaponStateChangeEvent
{
    public WeaponState NewState;
}

public struct HitStopEvent
{
    public float Duration;
}

public struct CameraShakeEvent
{
    public ShakeIntensity Intensity;
}
#endregion

#region [5. Audio & Camera Events (시청각 연출)]
public struct PlaySFXEvent
{
    public AudioClip Clip;
    public float Volume;
}

public struct CameraManipulationEvent { }
#endregion