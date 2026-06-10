using Unity.Mathematics;
using UnityEngine;

namespace THPerfection.LevelGen
{
    [CreateAssetMenu(fileName = "DefaultRoomEvaluator", menuName = "TH Perfection/Level Gen/Default Room Evaluator")]
    public sealed class DefaultRoomEvaluator : RoomTemplateBase
    {
        protected override float ApplySoftWeights(
            in PlacementContext ctx,
            in RoomTemplateDefinition template,
            int2 origin,
            Rotation90 rotation,
            float weight) => weight;
    }
}
