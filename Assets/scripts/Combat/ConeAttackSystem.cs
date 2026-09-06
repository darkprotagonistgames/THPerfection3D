using Unity.Burst;

using Unity.Collections;

using Unity.Entities;

using Unity.Mathematics;

using Unity.Transforms;



/// <summary>

/// When an entity's cone-attack cooldown has elapsed, scans hurtboxes on matching physics layers in

/// range and forward cone; on the first match, instantiates <see cref="ConeAttackData.ConeAttackPrefab"/>

/// at the spawner's <see cref="LocalTransform"/> with no ongoing link to the spawner.

/// </summary>

[BurstCompile]

[UpdateInGroup(typeof(SimulationSystemGroup))]

[UpdateAfter(typeof(TransformSystemGroup))]

public partial struct ConeAttackSystem : ISystem

{

    private EntityQuery _hurtboxTargetQuery;

    private struct PendingSpawn

    {

        public Entity Prefab;

        public LocalTransform Transform;

        public Entity GroupOwner;

    }



    [BurstCompile]

    public void OnCreate(ref SystemState state)

    {

        _hurtboxTargetQuery = CombatAttackTargeting.CreateHurtboxTargetQuery(ref state);
        state.RequireForUpdate<ConeAttackData>();

    }



    public void OnUpdate(ref SystemState state)

    {

        var targets = new NativeList<CombatAttackTargetCandidate>(Allocator.Temp);

        CombatAttackTargeting.CollectHurtboxTargets(_hurtboxTargetQuery, targets);



        var pending = new NativeList<PendingSpawn>(4, Allocator.Temp);

        float deltaTime = SystemAPI.Time.DeltaTime;



        foreach (var (coneAttack, transform, entity) in SystemAPI

                     .Query<RefRW<ConeAttackData>, RefRO<LocalTransform>>()

                     .WithEntityAccess())

        {

            ref ConeAttackData data = ref coneAttack.ValueRW;



            if (data.CooldownRemaining > 0f)

            {

                data.CooldownRemaining = math.max(0f, data.CooldownRemaining - deltaTime);

                continue;

            }



            if (data.ConeAttackPrefab == Entity.Null || data.Range <= 0f)

                continue;



            if (!CombatAttackTargeting.TryFindConeTarget(

                    in transform.ValueRO,

                    entity,

                    in targets,

                    data.TargetLayerMask,

                    data.Range,

                    data.HalfAngleRadians,

                    out _))

                continue;



            pending.Add(new PendingSpawn

            {

                Prefab = data.ConeAttackPrefab,

                Transform = transform.ValueRO,

                GroupOwner = entity,

            });

            data.CooldownRemaining = data.Cooldown;

        }



        if (pending.Length > 0)

        {

            var em = state.EntityManager;

            for (int i = 0; i < pending.Length; i++)

            {

                PendingSpawn spawn = pending[i];

                LinkedEntityGroupUtility.InstantiateAsSpawnChild(

                    em,

                    spawn.GroupOwner,

                    spawn.Prefab,

                    spawn.Transform);

            }

        }



        pending.Dispose();

        targets.Dispose();

    }

}


