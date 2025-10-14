
using System.ComponentModel;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using System;

namespace AML.Survivors
{
    public struct GoodWordData : IComponentData
    {
        public float MoveSpeed;
        public int AttackDamage;
    }
    public partial class GoodWordAuthoring : MonoBehaviour
    {
        public float MoveSpeed;
        public int AttackDamage;

        private class Baker : Baker<GoodWordAuthoring>
        {
            public override void Bake(GoodWordAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GoodWordData
                {
                    MoveSpeed = authoring.MoveSpeed,
                    AttackDamage = authoring.AttackDamage
                });
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity,false);
            }
        }

        public partial struct MoveGoodWordSystem : ISystem
        {
            public void OnUpdate(ref SystemState state)
            {
                var detlaTime = SystemAPI.Time.DeltaTime;
                foreach( var (transform, data) in SystemAPI.Query<RefRW<LocalTransform>, GoodWordData>())
                {
                    transform.ValueRW.Position += (transform.ValueRO.Right()) * data.MoveSpeed * detlaTime;
                }
            }
        }

        [UpdateInGroup(typeof(PhysicsSystemGroup))]
        [UpdateAfter(typeof(PhysicsSimulationGroup))]
        [UpdateBefore(typeof(AfterPhysicsSystemGroup))]
        public partial struct GoodWordAttackSystem : ISystem
        {
            public void OnUpdate(ref SystemState state)
            {
                var attackJob = new GoodWordAttackJob
                {
                    goodWordLookup = SystemAPI.GetComponentLookup<GoodWordData>(true),
                    enemyLookup = SystemAPI.GetComponentLookup<EnemyTag>(true),
                    DamageBufferLookup = SystemAPI.GetBufferLookup<DamageThisFrame>(),
                    DestroyEntityFlagLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>()
                };

                var simSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
                state.Dependency = attackJob.Schedule(simSingleton, state.Dependency);
                state.Dependency.Complete();
            }
        }

        public struct GoodWordAttackJob : ITriggerEventsJob
        {
            [Unity.Collections.ReadOnly] public ComponentLookup<GoodWordData> goodWordLookup;
            [Unity.Collections.ReadOnly] public ComponentLookup<EnemyTag> enemyLookup;
            public BufferLookup<DamageThisFrame> DamageBufferLookup;
            public ComponentLookup<DestroyEntityFlag> DestroyEntityFlagLookup;
            public void Execute(TriggerEvent triggerEvent)
            {
                Entity GoodWordEntity;
                Entity EnemyEntity;

                if(goodWordLookup.HasComponent(triggerEvent.EntityA) && enemyLookup.HasComponent(triggerEvent.EntityB))
                {
                    GoodWordEntity = triggerEvent.EntityA;
                    EnemyEntity = triggerEvent.EntityB;
                } else if (goodWordLookup.HasComponent(triggerEvent.EntityB) && enemyLookup.HasComponent(triggerEvent.EntityA))
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

                DestroyEntityFlagLookup.SetComponentEnabled(GoodWordEntity, true);
            }
        }
    }
}