/// <summary>
/// 피아 식별을 위한 팀 카테고리입니다. (IDamageable 등에서 사용)
/// </summary>
public enum TeamType
{
    None = 0,
    Player = 1,
    Enemy = 2
}

/// <summary>
/// [BladeShift PoC] 무기의 현재 제어 상태를 정의합니다.
/// </summary>
public enum WeaponState
{
    Grounded,    // 바닥에 떨어져 물리 엔진의 영향을 받는 상태 (통제 불능)
    Controlled,  // 마우스를 부드럽게 따라다니는 이기어검 상태 (호버링)
    Slashing,    // 좌클릭으로 고속 회전하며 주변을 베는 상태
    Thrusting,   // 우클릭으로 지정된 타겟이나 방향을 향해 사출되는 상태
    PinningFlight,
    Pinned,
    Returning
}

public enum ShakeIntensity
{
    Weak,
    Medium,
    Strong
}