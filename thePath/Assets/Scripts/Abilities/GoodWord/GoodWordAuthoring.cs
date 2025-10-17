
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace AML.Survivors
{
    public struct GoodWordData : IComponentData
    {
        public float MoveSpeed;
        public int AttackDamage;
        public float AbilityCritChance;
        public Random random;
    }

    public partial class GoodWordAuthoring : MonoBehaviour
    {
        public float MoveSpeed;
        public int AttackDamage;
        public uint randomSeed;

        private class Baker : Baker<GoodWordAuthoring>
        {

            public override void Bake(GoodWordAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GoodWordData
                {
                    MoveSpeed = authoring.MoveSpeed,
                    AttackDamage = authoring.AttackDamage,
                    AbilityCritChance = 0f,
                    random = Random.CreateFromIndex(authoring.randomSeed)
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
                    DestroyEntityFlagLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>(),
                    CritChance = PlayerStatSheet.instance.CriticalStrikeChance,
                    CritDamage = PlayerStatSheet.instance.CriticalStrikeDamageModifier,
                    DamageMod = PlayerStatSheet.instance.DamageModifier
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
            public float CritChance;
            public float CritDamage;
            public float DamageMod;

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

                // ** Damage
                var attackDamage = goodWordLookup[GoodWordEntity].AttackDamage;
                attackDamage = (int)(attackDamage * DamageMod);
                var rand = goodWordLookup[GoodWordEntity].random.NextFloat() * 100;
                
                if(CritChance > rand)
                {
                    attackDamage = (int)(attackDamage * CritDamage);
                    Debug.Log("Crit! " +  attackDamage + " GW");
                } else
                {
                    Debug.Log(attackDamage);
                }
                //


                var enemydamageBuffer = DamageBufferLookup[EnemyEntity];
                enemydamageBuffer.Add(new DamageThisFrame { Value = attackDamage });

                DestroyEntityFlagLookup.SetComponentEnabled(GoodWordEntity, true);
            }
        }
    }
}