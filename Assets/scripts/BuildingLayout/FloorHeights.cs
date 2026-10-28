using THPerfection.LevelGen;

/// <summary>
/// World-space Y for a character standing on a floor. Beta authors one flat
/// elevation (<see cref="BuildingSpatialConfig.MainFloorY"/>). Basement and attic
/// use that height until vertical links assign their own.
/// </summary>
public static class FloorHeights
{
    public static float WorldY(FloorId floor, in BuildingSpatialConfig config)
    {
        switch (floor)
        {
            case FloorId.Basement:
            case FloorId.Main:
            case FloorId.Attic:
                return config.MainFloorY;
            default:
                return config.MainFloorY;
        }
    }
}
