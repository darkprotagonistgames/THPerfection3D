using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public static class RoomPlacementRules
    {
        /// <summary>
        /// True when no occupied cell lies on the grid ray from the door socket outward.
        /// A door whose ray hits another room at any distance is not a viable expansion doorway.
        /// </summary>
        public static bool HasClearExpansionRay(FloorGrid grid, int2 doorCell, DoorSide side)
        {
            int2 dir = GridTransforms.Direction(side);
            int maxSteps = MaxOccupiedStepsAlongRay(grid, doorCell, dir);

            for (int step = 1; step <= maxSteps; step++)
            {
                if (grid.IsOccupied(doorCell + dir * step))
                    return false;
            }

            return true;
        }

        static int MaxOccupiedStepsAlongRay(FloorGrid grid, int2 origin, int2 dir)
        {
            int maxStep = 0;

            foreach (int2 cell in grid.Cells.Keys)
            {
                int2 delta = cell - origin;

                if (dir.x != 0)
                {
                    if (delta.y != 0)
                        continue;

                    int signed = delta.x * dir.x;
                    if (signed <= 0)
                        continue;

                    maxStep = math.max(maxStep, signed);
                }
                else
                {
                    if (delta.x != 0)
                        continue;

                    int signed = delta.y * dir.y;
                    if (signed <= 0)
                        continue;

                    maxStep = math.max(maxStep, signed);
                }
            }

            return math.max(maxStep, 1);
        }

        /// <summary>
        /// True when the cell immediately outside the door is occupied by another room
        /// that has no matching door on the shared edge (a wall contact).
        /// </summary>
        public static bool FacesAdjacentWall(FloorGrid grid, int2 doorCell, DoorSide side)
        {
            int2 neighbor = doorCell + GridTransforms.Direction(side);

            if (!grid.IsOccupied(neighbor))
                return false;

            if (!grid.TryGet(neighbor, out OccupiedCell occupied))
                return true;

            return !GridTransforms.HasDoor(occupied.Doors, GridTransforms.Opposite(side));
        }

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

            if (ctx.RequiresExpansionDoor
                && CountNewOpenDoorways(ctx, template, origin, rotation) == 0)
            {
                failure = PlacementFailure.DeadEndWhenFrontierLow;
                return false;
            }

            return true;
        }

        public static int CountNewOpenDoorways(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation)
        {
            int count = 0;

            foreach (DoorSocket socket in RoomPlacementMath.GetWorldDoorSockets(template, origin, rotation))
            {
                if (IsConnectedMainOrTargetDoor(ctx, template, origin, rotation, in socket))
                    continue;

                // Only clear-ray doors count toward the minimum expansion doorway budget.
                if (HasClearExpansionRay(ctx.Grid, socket.Cell, socket.Side))
                    count++;
            }

            return count;
        }

        static bool IsConnectedMainOrTargetDoor(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation,
            in DoorSocket socket)
        {
            if (!ctx.HasTargetDoorway)
                return false;

            DoorwaySlot target = ctx.TargetDoorway.Value;
            if (socket.Cell.Equals(target.Cell) && socket.Side == target.Side)
                return true;

            DoorSocket mainDoor = RoomPlacementMath.GetWorldMainDoor(template, origin, rotation);
            return socket.Cell.Equals(mainDoor.Cell) && socket.Side == mainDoor.Side;
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
