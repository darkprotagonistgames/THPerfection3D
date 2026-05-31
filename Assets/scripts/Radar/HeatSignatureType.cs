using System;

/// <summary>
/// Bitmask categories for heat-emitting entities on the radar.
/// Combine flags to let a single entity represent multiple types.
/// </summary>
[Flags]
public enum HeatSignatureType
{
    None     = 0,
    Monster  = 1 << 0,   // 1
    Treasure = 1 << 1,   // 2
}
