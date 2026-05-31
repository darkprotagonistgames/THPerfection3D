using Unity.Entities;
using UnityEngine;

/// <summary>
/// Bakes a companion Animator reference for hybrid locomotion playback on the XZ plane.
/// </summary>
public class MecanimLocomotionAuthoring : MonoBehaviour
{
    [Tooltip("Optional explicit Animator. Defaults to GetComponentInChildren if unset.")]
    public Animator TargetAnimator;

    [Tooltip("Animator state name used while moving.")]
    public string WalkStateName = "zombi_walk";

    public class Baker : Baker<MecanimLocomotionAuthoring>
    {
        public override void Bake(MecanimLocomotionAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            Animator animator = authoring.TargetAnimator != null
                ? authoring.TargetAnimator
                : authoring.GetComponentInChildren<Animator>();

            if (animator != null)
                AddComponentObject(entity, animator);

            AddComponent(entity, new MecanimLocomotionTag());
            AddComponent(entity, new MecanimLocomotionState
            {
                WalkStateHash = Animator.StringToHash(authoring.WalkStateName),
            });
        }
    }
}

public struct MecanimLocomotionState : IComponentData
{
    public int WalkStateHash;
}
