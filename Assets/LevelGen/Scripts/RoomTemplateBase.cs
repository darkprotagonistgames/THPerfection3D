using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen
{
    public abstract class RoomTemplateBase : ScriptableObject
    {
        [Min(0f)]
        public float BaseWeight = 1f;

        public float EvaluatePlacement(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation)
        {
            float weight = RoomPlacementRules.ApplyHardRules(
                ctx, template, origin, rotation, ResolveBaseWeight(template));

            if (weight <= 0f)
                return 0f;

            return ApplySoftWeights(ctx, template, origin, rotation, weight);
        }

        protected virtual float ResolveBaseWeight(in RoomTemplateDefinition template) =>
            template.BaseWeight > 0f ? template.BaseWeight : BaseWeight;

        protected abstract float ApplySoftWeights(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation,
            float weight);
    }
}
