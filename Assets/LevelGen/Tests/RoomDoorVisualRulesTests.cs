using NUnit.Framework;

namespace THPerfection.LevelGen.Tests
{
    public sealed class RoomDoorVisualRulesTests
    {
        [Test]
        public void Gameplay_showsOnlyConnectedDoorsOpen()
        {
            Assert.IsTrue(RoomDoorVisualRules.ShouldShowOpen(
                CellDoorState.Connected, RoomDoorVisualPhase.Gameplay));
            Assert.IsFalse(RoomDoorVisualRules.ShouldShowOpen(
                CellDoorState.Open, RoomDoorVisualPhase.Gameplay));
            Assert.IsFalse(RoomDoorVisualRules.ShouldShowOpen(
                CellDoorState.Closed, RoomDoorVisualPhase.Gameplay));
        }

        [Test]
        public void Spawning_showsConnectedAndFrontierDoorsOpen()
        {
            Assert.IsTrue(RoomDoorVisualRules.ShouldShowOpen(
                CellDoorState.Connected, RoomDoorVisualPhase.Spawning));
            Assert.IsTrue(RoomDoorVisualRules.ShouldShowOpen(
                CellDoorState.Open, RoomDoorVisualPhase.Spawning));
            Assert.IsFalse(RoomDoorVisualRules.ShouldShowOpen(
                CellDoorState.Closed, RoomDoorVisualPhase.Spawning));
        }
    }
}
