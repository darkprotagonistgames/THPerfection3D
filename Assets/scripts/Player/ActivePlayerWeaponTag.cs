using Unity.Entities;

/// <summary>
/// Marks the player's currently equipped weapon-system entity spawned at runtime.
/// Only one entity should carry this tag at a time; see <see cref="PlayerInventoryManager"/>.
/// </summary>
public struct ActivePlayerWeaponTag : IComponentData { }
