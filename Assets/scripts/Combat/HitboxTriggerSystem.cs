using THPerfection.GeneratedEvents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

/// <summary>
/// Hitbox-led trigger handling: when a hitbox overlaps a hurtbox, emits
/// <see cref="damageEvent"/> on the hurtbox owner (future health entity).
/// </summary>
[BurstCompile]
[UpdateInGroup(typeof(PhysicsSimulationGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct HitboxTriggerSystem : ISystem
{
    private NativeParallelHashMap<long, byte> _processedPairs;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<HitboxData>();
        _processedPairs = new NativeParallelHashMap<long, byte>(128, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (_processedPairs.IsCreated)
            _processedPairs.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _processedPairs.Clear();

        var ecb = SystemAPI
            .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        var hitboxDataLookup = SystemAPI.GetComponentLookup<HitboxData>(true);
        var hurtboxDataLookup = SystemAPI.GetComponentLookup<HurtboxData>(true);
        var hitboxOwnerLookup = SystemAPI.GetComponentLookup<HitboxOwner>(true);
        var hurtboxOwnerLookup = SystemAPI.GetComponentLookup<HurtboxOwner>(true);
        hitboxDataLookup.Update(ref state);
        hurtboxDataLookup.Update(ref state);
        hitboxOwnerLookup.Update(ref state);
        hurtboxOwnerLookup.Update(ref state);

        var job = new HitboxTriggerJob
        {
            HitboxDataLookup = hitboxDataLookup,
            HurtboxDataLookup = hurtboxDataLookup,
            HitboxOwnerLookup = hitboxOwnerLookup,
            HurtboxOwnerLookup = hurtboxOwnerLookup,
            ProcessedPairs = _processedPairs.AsParallelWriter(),
            Ecb = ecb.AsParallelWriter(),
        };

        state.Dependency = job.Schedule(
            SystemAPI.GetSingleton<SimulationSingleton>(),
            state.Dependency);
    }

    [BurstCompile]
    struct HitboxTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<HitboxData> HitboxDataLookup;
        [ReadOnly] public ComponentLookup<HurtboxData> HurtboxDataLookup;
        [ReadOnly] public ComponentLookup<HitboxOwner> HitboxOwnerLookup;
        [ReadOnly] public ComponentLookup<HurtboxOwner> HurtboxOwnerLookup;
        public NativeParallelHashMap<long, byte>.ParallelWriter ProcessedPairs;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            bool aIsHitbox = HitboxDataLookup.HasComponent(entityA);
            bool bIsHitbox = HitboxDataLookup.HasComponent(entityB);

            if (!aIsHitbox && !bIsHitbox)
                return;

            if (aIsHitbox && bIsHitbox)
                return;

            Entity hitboxEntity;
            Entity hurtboxEntity;

            if (aIsHitbox)
            {
                hitboxEntity = entityA;
                hurtboxEntity = entityB;
            }
            else
            {
                hitboxEntity = entityB;
                hurtboxEntity = entityA;
            }

            if (!HurtboxDataLookup.HasComponent(hurtboxEntity))
                return;

            if (!HitboxOwnerLookup.HasComponent(hitboxEntity) || !HurtboxOwnerLookup.HasComponent(hurtboxEntity))
                return;

            HitboxData hitbox = HitboxDataLookup[hitboxEntity];
            HurtboxData hurtbox = HurtboxDataLookup[hurtboxEntity];

            Entity hitboxOwner = HitboxOwnerLookup[hitboxEntity].Value;
            Entity hurtboxOwner = HurtboxOwnerLookup[hurtboxEntity].Value;

            if (hitboxOwner == hurtboxOwner)
                return;

            long pairKey = MakePairKey(hitboxEntity, hurtboxEntity);
            if (!ProcessedPairs.TryAdd(pairKey, 1))
                return;

            hurtboxOwner.CreatedamageEvent(Ecb, 0, hitbox.Damage, hitbox.WeaponType, hurtbox.Category);
        }

        static long MakePairKey(Entity hitboxEntity, Entity hurtboxEntity)
        {
            int a = hitboxEntity.Index;
            int b = hurtboxEntity.Index;
            if (a > b)
                (a, b) = (b, a);

            return ((long)a << 32) | (uint)b;
        }
    }
}
