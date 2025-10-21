using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.Burst;
using Random = Unity.Mathematics.Random;

namespace AML.Survivors
{
    public struct PillarOfSaltData : IComponentData
    {
        public float AoERadius;
        public int AttackDamage;
        public float DamageFrequency;
        public float Lifetime;
        public float StartSize;
        public float EndSize;
    }

    public struct PillarOfSaltUpdateData : IComponentData
    {
        public float DamageFrequencyBucket;
        public float LifetimeBucket;
        public Random random;
    }

    public struct DamageList : IBufferElementData
    {
        public Entity Value;
    }

    public class PIllarOfSaltAuthoring : MonoBehaviour
    {
        public float Radius;
        public int Damage;
        public float Lifetime;
        public float DamageFrequency;
        public Vector2 SizeStartEnd;
        public uint randomSeed;

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
                    StartSize = authoring.SizeStartEnd.x,
                    EndSize = authoring.SizeStartEnd.y
                });
                AddComponent(entity, new PillarOfSaltUpdateData
                {
                    LifetimeBucket = authoring.Lifetime,
                    DamageFrequencyBucket = authoring.DamageFrequency,
                    random = Random.CreateFromIndex(authoring.randomSeed)
                });
                AddBuffer<DamageList>(entity);
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }


    /// <summary>
    /// //////////////////////////////////////////
    /// </summary>
    /// 


    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(AfterPhysicsSystemGroup))]
    public partial struct GrowPilarOfSaltSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, updateData, data, dmgList) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PillarOfSaltUpdateData>, RefRO<PillarOfSaltData>, DynamicBuffer<DamageList>>())
            {

                updateData.ValueRW.LifetimeBucket -= deltaTime;

                var percentOfLifetime = 1 - ((updateData.ValueRO.LifetimeBucket - 0.000001f) / data.ValueRO.Lifetime);
                var currentScale = (data.ValueRO.EndSize - data.ValueRO.StartSize) * percentOfLifetime;

                transform.ValueRW.Scale = data.ValueRO.StartSize + currentScale;


                // apply damage to enemies
                updateData.ValueRW.DamageFrequencyBucket -= deltaTime;
                if (updateData.ValueRW.DamageFrequencyBucket < 0)
                {
                    updateData.ValueRW.DamageFrequencyBucket = data.ValueRO.DamageFrequency;

                    foreach (var obj in dmgList)
                    {
                        if (entityManager.Exists(obj.Value))
                        {
                            SystemAPI.GetBufferLookup<DamageThisFrame>()[obj.Value].Add(new DamageThisFrame { Value = data.ValueRO.AttackDamage });
                        }
                    }
                    dmgList.Clear();
                }
            }
        }
    }

    public partial struct DestroyPilarOfSaltSystem : ISystem
    {
        public ComponentLookup<DestroyEntityFlag> DestroyEntityFlagLookup;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            DestroyEntityFlagLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>();
            var detlaTime = SystemAPI.Time.DeltaTime;

            foreach (var (data, entity) in SystemAPI.Query<RefRW<PillarOfSaltUpdateData>>().WithPresent<DestroyEntityFlag>().WithEntityAccess())
            {
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
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // damage frequency

            var attackJob = new PillarOfSaltAttackJob
            {
                pillarofsaltLookup = SystemAPI.GetComponentLookup<PillarOfSaltData>(true),
                pillarofsaltupdateLookup = SystemAPI.GetComponentLookup<PillarOfSaltUpdateData>(false),
                damageListLookup = SystemAPI.GetBufferLookup<DamageList>(false),
                enemyLookup = SystemAPI.GetComponentLookup<EnemyTag>(true),
                CritChance = PlayerStatSheet.instance.CriticalStrikeChance,
                CritDamage = PlayerStatSheet.instance.CriticalStrikeDamageModifier,
                DamageMod = PlayerStatSheet.instance.DamageModifier,
                time = (uint)(SystemAPI.Time.ElapsedTime)
            };

            var simSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = attackJob.Schedule(simSingleton, state.Dependency);
            state.Dependency.Complete();
        }
    }

    //////////////////////////////////////////////////////////
    public struct PillarOfSaltAttackJob : ITriggerEventsJob
    {
        [Unity.Collections.ReadOnly] public ComponentLookup<PillarOfSaltData> pillarofsaltLookup;
        public ComponentLookup<PillarOfSaltUpdateData> pillarofsaltupdateLookup;
        public BufferLookup<DamageList> damageListLookup;
        [Unity.Collections.ReadOnly] public ComponentLookup<EnemyTag> enemyLookup;
        public float CritChance;
        public float CritDamage;
        public float DamageMod;
        public float time;

        public float outputRandomNumber;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity PillarOfSaltEntity;
            Entity EnemyEntity;
            Random rand = Random.CreateFromIndex((uint)(time));

            if (pillarofsaltLookup.HasComponent(triggerEvent.EntityA) && enemyLookup.HasComponent(triggerEvent.EntityB))
            {
                PillarOfSaltEntity = triggerEvent.EntityA;
                EnemyEntity = triggerEvent.EntityB;
            }
            else if (pillarofsaltLookup.HasComponent(triggerEvent.EntityB) && enemyLookup.HasComponent(triggerEvent.EntityA))
            {
                PillarOfSaltEntity = triggerEvent.EntityB;
                EnemyEntity = triggerEvent.EntityA;
            }
            else
            {
                return;
            }


            // ** Damage
            var attackDamage = pillarofsaltLookup[PillarOfSaltEntity].AttackDamage;
            attackDamage = (int)(attackDamage * DamageMod);
            var randnum = (int)(rand.NextFloat() * 100);

            if (CritChance > randnum)
            {
                float adjustedDmg = attackDamage * CritDamage;
                if (adjustedDmg % 1.0f >= 0.5f)
                {
                    adjustedDmg++;
                }
                attackDamage = (int)adjustedDmg;
                Debug.Log("Crit! " + attackDamage + " PoS");
            }
            else
            {
                Debug.Log(attackDamage + " PoS");
            }
            //

            // var enemydamageBuffer = DamageBufferLookup[EnemyEntity];

            // ADD the enemy Entity to a buffer of enemies to apply the damage to
            bool found = false;
            foreach(var dmgObj in damageListLookup[PillarOfSaltEntity])
            {
                if( dmgObj.Value == EnemyEntity)
                {
                    found = true; break;
                }
            }
            if( !found )
            {
                damageListLookup[PillarOfSaltEntity].Add(new DamageList { Value = EnemyEntity });
            }
        }
    }
}