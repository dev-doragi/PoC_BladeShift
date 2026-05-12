public enum TeamType
{
    None = 0,
    Player = 1,
    Enemy = 2
}

public enum UnitCategory
{
    None = 0,
    Wheel,
    Core,
    Attack,
    Defense,
    Support,
    All = ~0
}

public enum PlacementRule
{
    InitialOnly,
    NeedsFoundationBelow,
    NeedsAdjacent
}

public enum SupportTargetRoleType
{
    None = 0,
    All = 1,
    Attack = 2,
    Defense = 3
}

public enum SupportStatType
{
    None = 0,
    AttackDamage = 1,
    AttackSpeed = 2,
    PenetrationRate = 3,
    DefenseRate = 4
}

public enum AttackTrajectoryType
{
    None = 0,
    Direct = 1,    Arc = 2
}

public enum ModifierType
{
    None = 0,
    Flat = 1,
    Percent = 2
}

public enum AttackTrajectory { Direct, Arc, Special, Collision }
public enum TargetingPolicy { Closest, TowardCore, PriorityAttacker }
public enum AreaType { Single, Splash, Piercing }