using Unity.Mathematics;

namespace THPerfection.LevelGen
{
    public interface IRoomPlacementEvaluator
    {
        float EvaluatePlacement(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation);
    }

    public sealed class HardRulesOnlyEvaluator : IRoomPlacementEvaluator
    {
        public static readonly HardRulesOnlyEvaluator Instance = new();

        public float EvaluatePlacement(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation) =>
            RoomPlacementRules.ApplyHardRules(ctx, template, origin, rotation, template.BaseWeight);
    }

    public sealed class ScriptableRoomEvaluator : IRoomPlacementEvaluator
    {
        readonly RoomTemplateBase _evaluator;

        public ScriptableRoomEvaluator(RoomTemplateBase evaluator) => _evaluator = evaluator;

        public float EvaluatePlacement(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation) =>
            _evaluator.EvaluatePlacement(ctx, template, origin, rotation);
    }
}
