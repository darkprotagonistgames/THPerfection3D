using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public static class RoomPlacementRules
    {
        public static bool TryValidate(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation,
            out PlacementFailure failure)
        {
            failure = PlacementFailure.None;

            if ((template.AllowedFloors & GridTransforms.ToMask(ctx.Floor)) == 0)
            {
                failure = PlacementFailure.FloorNotAllowed;
                return false;
            }

            if (HasOverlap(ctx.Grid, template.Cells, origin, rotation))
            {
                failure = PlacementFailure.Overlap;
                return false;
            }

            if (ctx.HasTargetDoorway)
            {
                if (!RoomPlacementMath.MainDoorAlignsWith(template, origin, rotation, ctx.TargetDoorway.Value))
                {
                    failure = PlacementFailure.MainDoorMisaligned;
                    return false;
                }
            }

            if (HasDoorIntoWall(ctx, template, origin, rotation))
            {
                failure = PlacementFailure.DoorIntoWall;
                return false;
            }

            return true;
        }

        public static float ApplyHardRules(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation,
            float baseWeight)
        {
            return TryValidate(ctx, template, origin, rotation, out _)
                ? baseWeight
                : 0f;
        }

        static bool HasOverlap(FloorGrid grid, int2[] localCells, int2 origin, Rotation90 rotation)
        {
            foreach (int2 worldCell in RoomPlacementMath.GetWorldCells(localCells, origin, rotation))
            {
                if (grid.IsOccupied(worldCell))
                    return true;
            }

            return false;
        }

        static bool HasDoorIntoWall(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation)
        {
            foreach (DoorSocket worldSocket in RoomPlacementMath.GetWorldDoorSockets(template, origin, rotation))
            {
                if (!IsDoorConnectionValid(ctx, worldSocket))
                    return true;
            }

            return false;
        }

        static bool IsDoorConnectionValid(in PlacementContext ctx, in DoorSocket worldSocket)
        {
            int2 neighbor = worldSocket.Cell + GridTransforms.Direction(worldSocket.Side);

            if (!ctx.Grid.IsOccupied(neighbor))
                return true;

            if (ctx.HasTargetDoorway && IsTargetMainDoorConnection(ctx.TargetDoorway.Value, worldSocket, neighbor))
                return true;

            if (!ctx.Grid.TryGet(neighbor, out OccupiedCell occupied))
                return false;

            return GridTransforms.HasDoor(occupied.Doors, GridTransforms.Opposite(worldSocket.Side));
        }

        static bool IsTargetMainDoorConnection(
            in DoorwaySlot targetDoorway,
            in DoorSocket worldSocket,
            int2 neighbor) =>
            neighbor.Equals(targetDoorway.Cell)
            && worldSocket.Side == GridTransforms.Opposite(targetDoorway.Side);
    }
}
