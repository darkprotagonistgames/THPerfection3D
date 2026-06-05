using Unity.Entities;

/// <summary>
/// Counts down each simulation frame; when <see cref="SecondsRemaining"/> reaches zero,
/// <see cref="TtlSystem"/> destroys the entity this component is on.
/// </summary>
public struct TtlData : IComponentData
{
    public float SecondsRemaining;
}
