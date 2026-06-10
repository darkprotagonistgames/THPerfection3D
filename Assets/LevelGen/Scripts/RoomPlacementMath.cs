using System.Collections.Generic;
using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public readonly struct AlignedPlacement
    {
        public readonly int2 Origin;
        public readonly Rotation90 Rotation;

        public AlignedPlacement(int2 origin, Rotation90 rotation)
        {
            Origin   = origin;
            Rotation = rotation;
        }
    }

    public static class RoomPlacementMath
    {
        public static IEnumerable<int2> GetWorldCells(int2[] localCells, int2 origin, Rotation90 rotation)
        {
            for (int i = 0; i < localCells.Length; i++)
                yield return origin + GridTransforms.RotateCell(localCells[i], rotation);
        }

        public static IEnumerable<DoorSocket> GetWorldDoorSockets(
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation)
        {
            for (int i = 0; i < template.DoorSockets.Length; i++)
            {
                DoorSocket local = template.DoorSockets[i];
                yield return new DoorSocket(
                    origin + GridTransforms.RotateCell(local.Cell, rotation),
                    GridTransforms.RotateSide(local.Side, rotation),
                    local.IsMainDoor);
            }
        }

        public static DoorSocket GetWorldMainDoor(
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation) =>
            new(
                origin + GridTransforms.RotateCell(template.MainDoorCell, rotation),
                GridTransforms.RotateSide(template.MainDoorSide, rotation),
                isMainDoor: true);

        public static IEnumerable<AlignedPlacement> FindAlignedPlacements(
            in RoomTemplateDefinition template,
            in DoorwaySlot targetDoorway)
        {
            int2 expansionCell = targetDoorway.ExpansionCell;
            DoorSide requiredMainSide = GridTransforms.Opposite(targetDoorway.Side);

            for (int r = 0; r < 4; r++)
            {
                var rotation = (Rotation90)r;
                int2 rotatedMainCell = GridTransforms.RotateCell(template.MainDoorCell, rotation);
                int2 origin = expansionCell - rotatedMainCell;

                DoorSide worldMainSide = GridTransforms.RotateSide(template.MainDoorSide, rotation);
                if (worldMainSide != requiredMainSide)
                    continue;

                yield return new AlignedPlacement(origin, rotation);
            }
        }

        public static bool MainDoorAlignsWith(
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation,
            in DoorwaySlot targetDoorway)
        {
            DoorSocket worldMain = GetWorldMainDoor(template, origin, rotation);
            return worldMain.Cell.Equals(targetDoorway.ExpansionCell)
                && worldMain.Side == GridTransforms.Opposite(targetDoorway.Side);
        }
    }
}
