using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen
{
    [CreateAssetMenu(fileName = "DefaultRoomEvaluator", menuName = "TH Perfection/Level Gen/Default Room Evaluator")]
    public sealed class DefaultRoomEvaluator : RoomTemplateBase
    {
        /// <summary>
        /// Base evaluator: hard placement rules + template base weight, no extra soft rules.
        /// Subclass this (override <see cref="ApplySoftWeights"/>) for room-type or event-specific weighting.
        /// </summary>
        protected override float ApplySoftWeights(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation,
            float weight) => weight;
    }
}
