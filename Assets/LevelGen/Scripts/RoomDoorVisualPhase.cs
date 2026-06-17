namespace THPerfection.LevelGen
{
    /// <summary>
    /// Whether spawned room doors reflect gameplay (closed frontiers) or an active spawn/expansion pass.
    /// Logical <see cref="CellDoorState"/> on <see cref="RoomInstance"/> is unchanged.
    /// </summary>
    public enum RoomDoorVisualPhase
    {
        Gameplay,
        Spawning,
    }

    public static class RoomDoorVisualRules
    {
        /// <summary>
        /// Connected passages stay open in both phases. Open frontier edges show open only while spawning.
        /// </summary>
        public static bool ShouldShowOpen(CellDoorState logical, RoomDoorVisualPhase phase) =>
            phase switch
            {
                RoomDoorVisualPhase.Spawning => logical is CellDoorState.Open or CellDoorState.Connected,
                _ => logical == CellDoorState.Connected,
            };
    }
}
