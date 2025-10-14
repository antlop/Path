using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using System.Numerics;

namespace AML.Survivors
{
    public struct EnemyTag : IComponentData { }

    public struct EnemyAttackData : IComponentData 
    {
        public int HitPoints;
        public float CooldownTime;
    }

    public struct EnemyCooldownExpirationTimestamp : IComponentData, IEnableableComponent
    {
        public double Value;
    }

    public struct EXP_OrbPrefab : IComponentData
    {
        public Entity Value;
    }

    [RequireComponent(typeof(CharacterAuthoring))]
    public class EnemyAuthoring : MonoBehaviour
    {
        public int AttackDamage;
        public float CooldownTime;
        public GameObject expPrefab;

        private class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnemyTag>(entity);
                AddComponent(entity, new EnemyAttackData
                {
                    HitPoints = authoring.AttackDamage,
                    CooldownTime = authoring.CooldownTime
                });
                AddComponent<EnemyCooldownExpirationTimestamp>(entity);
                SetComponentEnabled<EnemyCooldownExpirationTimestamp>(entity, false);
                AddComponent(entity, new EXP_OrbPrefab
                {
                    Value = GetEntity(authoring.expPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    public partial struct EnemyMoveToPlayerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>(); //only works if 1 gameobject has the 'PlayerTag'
            var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position.xz;

            // time to make a job from the job system
            var moveToPlayerJob = new EnemyMovetoPlayerJob
            {
                PlayerPosition = playerPosition
            };

            state.Dependency = moveToPlayerJob.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

        }
    }

    [BurstCompile]
    [WithAll(typeof(EnemyTag))]
    public partial struct EnemyMovetoPlayerJob : IJobEntity
    {
        // information used by all entities
        public float2 PlayerPosition;

        private void Execute(ref CharacterMoveDirection direction, ref /*reading from*/ LocalTransform transform)
        {
            var vectorToPlayer = PlayerPosition - transform.Position.xz;
            direction.Value = math.normalize(vectorToPlayer);
            transform.Rotation.value = quaternion.LookRotation(new float3(vectorToPlayer.x, 0, vectorToPlayer.y), math.up()).value;
        }
    }

    public partial struct EnemyAttackSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;
            foreach(var (expirationTimeStamp, cooldownEnabled) in SystemAPI.Query<EnemyCooldownExpirationTimestamp, EnabledRefRW<EnemyCooldownExpirationTimestamp>>())
            {
                if (expirationTimeStamp.Value > elapsedTime) continue;
                cooldownEnabled.ValueRW = false;
            }

            var attackjob = new EnemyAttackJob
            {
                playerLookup = SystemAPI.GetComponentLookup<PlayerTag>(true),
                AttackDataLookup = SystemAPI.GetComponentLookup<EnemyAttackData>(true),
                CooldownExpirationTimestampLookup = SystemAPI.GetComponentLookup<EnemyCooldownExpirationTimestamp>(),
                DamageThisFrameLookup = SystemAPI.GetBufferLookup<DamageThisFrame>(),
                ElapsedTime = elapsedTime
            };

            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = attackjob.Schedule(simulationSingleton, state.Dependency);
        }
    }

    [GenerateTestsForBurstCompatibility]
    public struct EnemyAttackJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentLookup<PlayerTag> playerLookup;
        [ReadOnly] public ComponentLookup<EnemyAttackData> AttackDataLookup;
        public ComponentLookup<EnemyCooldownExpirationTimestamp> CooldownExpirationTimestampLookup;
        public BufferLookup<DamageThisFrame> DamageThisFrameLookup;
        public double ElapsedTime;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity playerEntity;
            Entity enemyEntity;

            if (playerLookup.HasComponent(collisionEvent.EntityA) && AttackDataLookup.HasComponent(collisionEvent.EntityB))
            {
                playerEntity = collisionEvent.EntityA;
                enemyEntity = collisionEvent.EntityB;
            } else if (playerLookup.HasComponent(collisionEvent.EntityB) && AttackDataLookup.HasComponent(collisionEvent.EntityA))
            {
                playerEntity = collisionEvent.EntityB;
                enemyEntity = collisionEvent.EntityA;
            } else
            {
                return;
            }

            if (CooldownExpirationTimestampLookup.IsComponentEnabled(enemyEntity)) return;

            var attackData = AttackDataLookup[enemyEntity];
            CooldownExpirationTimestampLookup[enemyEntity] = new EnemyCooldownExpirationTimestamp { Value = ElapsedTime + attackData.CooldownTime };
            CooldownExpirationTimestampLookup.SetComponentEnabled(enemyEntity, true);

            var playerDamageBuffer = DamageThisFrameLookup[playerEntity];
            playerDamageBuffer.Add(new DamageThisFrame { Value = attackData.HitPoints });
        }
    }
}