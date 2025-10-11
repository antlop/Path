using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using static AML.Survivors.GoodWordAuthoring;

namespace AML.Survivors
{
    public struct PillarOfSaltData : IComponentData
    {
        public float AoERadius;
        public int AttackDamage;
        public float DamageFrequency;
        public float Lifetime;
    }

    public struct PillarOfSaltUpdateData : IComponentData
    {
        public float DamageFrequencyBucket;
        public float LifetimeBucket;
    }

    public class PIllarOfSaltAuthoring : MonoBehaviour
    {
        public float Radius;
        public int Damage;
        public float Lifetime;
        public float DamageFrequency;

        private class Baker : Baker<PIllarOfSaltAuthoring>
        {
            public override void Bake(PIllarOfSaltAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PillarOfSaltData
                {
                    AoERadius = authoring.Radius,
                    AttackDamage = authoring.Damage,
                    Lifetime = authoring.Lifetime,
                    DamageFrequency = authoring.DamageFrequency,
                });
                AddComponent(entity, new PillarOfSaltUpdateData
                {
                    LifetimeBucket = authoring.Lifetime,
                    DamageFrequencyBucket = authoring.DamageFrequency
                });
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }


    /// <summary>
    /// //////////////////////////////////////////
    /// </summary>
    /// 


    public partial struct GrowPilarOfSaltSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var detlaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, data) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PillarOfSaltUpdateData>>())
            {
                transform.ValueRW.Scale += detlaTime;
                data.ValueRW.LifetimeBucket -= detlaTime;
                data.ValueRW.DamageFrequencyBucket -= detlaTime;
            }
        }
    }

    public partial struct DestroyPilarOfSaltSystem : ISystem
    {
        public ComponentLookup<DestroyEntityFlag> DestroyEntityFlagLookup;

        public void OnUpdate(ref SystemState state)
        {
            DestroyEntityFlagLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>();
            var detlaTime = SystemAPI.Time.DeltaTime;

            foreach (var (data, entity) in SystemAPI.Query<RefRW<PillarOfSaltUpdateData>>().WithPresent<DestroyEntityFlag>().WithEntityAccess())
            {
                data.ValueRW.LifetimeBucket -= detlaTime;
                if (data.ValueRO.LifetimeBucket <= 0)
                {
                    DestroyEntityFlagLookup.SetComponentEnabled(entity, true);
                }
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(AfterPhysicsSystemGroup))]
    public partial struct PillarOfSaltAttackSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var attackJob = new PillarOfSaltAttackJob
            {
                goodWordLookup = SystemAPI.GetComponentLookup<PillarOfSaltData>(true),
                enemyLookup = SystemAPI.GetComponentLookup<EnemyTag>(true),
                DamageBufferLookup = SystemAPI.GetBufferLookup<DamageThisFrame>()
            };

            var simSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = attackJob.Schedule(simSingleton, state.Dependency);
        }
    }

    //////////////////////////////////////////////////////////
    public struct PillarOfSaltAttackJob : ITriggerEventsJob
    {
        [Unity.Collections.ReadOnly] public ComponentLookup<PillarOfSaltData> goodWordLookup;
        [Unity.Collections.ReadOnly] public ComponentLookup<EnemyTag> enemyLookup;
        public BufferLookup<DamageThisFrame> DamageBufferLookup;
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity GoodWordEntity;
            Entity EnemyEntity;

            if (goodWordLookup.HasComponent(triggerEvent.EntityA) && enemyLookup.HasComponent(triggerEvent.EntityB))
            {
                GoodWordEntity = triggerEvent.EntityA;
                EnemyEntity = triggerEvent.EntityB;
            }
            else if (goodWordLookup.HasComponent(triggerEvent.EntityB) && enemyLookup.HasComponent(triggerEvent.EntityA))
            {
                GoodWordEntity = triggerEvent.EntityB;
                EnemyEntity = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            var attackDamage = goodWordLookup[GoodWordEntity].AttackDamage;
            var enemydamageBuffer = DamageBufferLookup[EnemyEntity];
            enemydamageBuffer.Add(new DamageThisFrame { Value = attackDamage });

            // This will be done over time
           // DestroyEntityFlagLookup.SetComponentEnabled(GoodWordEntity, true);
        }
    }
}