using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.Burst;
using Random = Unity.Mathematics.Random;

namespace AML.Survivors
{
    public struct FlamingSwordData : IComponentData
    {
        public int AttackDamage;
        public float Lifetime;
    }

    public class FlamingSwordVisual : IComponentData
    {
        public GameObject Value;
    }

    public struct FlamingSwordUpdateData : IComponentData
    {
        public float LifetimeBucket;
        public Random random;
    }

    public class FlamingSwordAuthoring : MonoBehaviour
    {
        public int Damage;
        public float Lifetime;
        public uint randomSeed;
        public GameObject Prefab;

        private class Baker : Baker<FlamingSwordAuthoring>
        {
            public override void Bake(FlamingSwordAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<FlamingSwordData>(entity, new FlamingSwordData
                {
                    AttackDamage = authoring.Damage,
                    Lifetime = authoring.Lifetime
                });
                AddComponentObject(entity, new FlamingSwordVisual
                {
                    Value = authoring.Prefab
                });
                AddComponent<FlamingSwordUpdateData>(entity, new FlamingSwordUpdateData
                {
                    LifetimeBucket = authoring.Lifetime,
                    random = Random.CreateFromIndex(authoring.randomSeed)
                });
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }

    public partial struct DestroyFlamingSwordSystem : ISystem
    {
        public ComponentLookup<DestroyEntityFlag> DestroyEntityFlagLookup;

        public void OnUpdate(ref SystemState state)
        {
            DestroyEntityFlagLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>();
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (data, entity) in SystemAPI.Query<RefRW<FlamingSwordUpdateData>>().WithPresent<DestroyEntityFlag>().WithEntityAccess())
            {
                data.ValueRW.LifetimeBucket -= deltaTime;
                if (data.ValueRO.LifetimeBucket <= 0 && DestroyEntityFlagLookup.EntityExists(entity))
                {
                    DestroyEntityFlagLookup.SetComponentEnabled(entity, true);
                }
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(AfterPhysicsSystemGroup))]
    public partial struct FlamingSwordAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // damage frequency

            var attackJob = new FireSwordAttackJob
            {
                FlamingSwordLookup = SystemAPI.GetComponentLookup<FlamingSwordData>(true),
                FireSwordupdateLookup = SystemAPI.GetComponentLookup<FlamingSwordUpdateData>(false),
                //damageListLookup = SystemAPI.GetBufferLookup<DamageList>(false),
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
    public struct FireSwordAttackJob : ITriggerEventsJob
    {
        [Unity.Collections.ReadOnly] public ComponentLookup<FlamingSwordData> FlamingSwordLookup;
        public ComponentLookup<FlamingSwordUpdateData> FireSwordupdateLookup;
        [Unity.Collections.ReadOnly] public ComponentLookup<EnemyTag> enemyLookup;
        public float CritChance;
        public float CritDamage;
        public float DamageMod;
        public float time;

        public float outputRandomNumber;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity FlamingSwordEntity;
            Entity EnemyEntity;
            Random rand = Random.CreateFromIndex((uint)(time));

            if (FlamingSwordLookup.HasComponent(triggerEvent.EntityA) && enemyLookup.HasComponent(triggerEvent.EntityB))
            {
                FlamingSwordEntity = triggerEvent.EntityA;
                EnemyEntity = triggerEvent.EntityB;
            }
            else if (FlamingSwordLookup.HasComponent(triggerEvent.EntityB) && enemyLookup.HasComponent(triggerEvent.EntityA))
            {
                FlamingSwordEntity = triggerEvent.EntityB;
                EnemyEntity = triggerEvent.EntityA;
            }
            else
            {
                return;
            }


            // ** Damage
            var attackDamage = FlamingSwordLookup[FlamingSwordEntity].AttackDamage;
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
            /* bool found = false;
             foreach (var dmgObj in damageListLookup[PillarOfSaltEntity])
             {
                 if (dmgObj.Value == EnemyEntity)
                 {
                     found = true; break;
                 }
             }
             if (!found)
             {
                 damageListLookup[PillarOfSaltEntity].Add(new DamageList { Value = EnemyEntity });
             }*/
        }
    }
}