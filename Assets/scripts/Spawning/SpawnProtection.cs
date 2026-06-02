/// <summary>
/// Spawn placement uses the spawnProtection layer exclusively — configure the physics
/// layer matrix so spawnProtection only collides with spawnProtection.
/// </summary>
public static class SpawnProtection
{
    public const uint LayerMask = 1u << (int)Layers.spawnProtection;
}
