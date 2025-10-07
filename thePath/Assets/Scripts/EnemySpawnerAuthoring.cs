using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace AML.Survivors
{
    public struct EnemySpawnData : IComponentData
    {
        public Entity EnemyPrefab;
        public float SpawnInterval;
        public float SpawnDistance;
    }

    public struct EnemySpawnState : IComponentData
    {
        public float SpawnTimer;
        public Random Random;
    }

    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject EnemyPrefab;
        public float SpawnInterval;
        public float SpawnDistance;
        public uint randomSeed;

        private class Baker : Baker<EnemySpawnerAuthoring>
        {
            public override void Bake(EnemySpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EnemySpawnData
                {
                    EnemyPrefab = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic),
                    SpawnInterval = authoring.SpawnInterval,
                    SpawnDistance = authoring.SpawnDistance
                });
                AddComponent(entity, new EnemySpawnState
                {
                    SpawnTimer = 0,
                    Random = Random.CreateFromIndex(authoring.randomSeed)
                });
            }
        }
    }

    public partial struct EnemySpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

            foreach (var (spawnState, spawnData) in SystemAPI.Query<RefRW<EnemySpawnState>, EnemySpawnData>())
            {
                spawnState.ValueRW.SpawnTimer -= deltaTime;
                if (spawnState.ValueRO.SpawnTimer > 0) { continue; }

                spawnState.ValueRW.SpawnTimer = spawnData.SpawnInterval;

                var newEnemy = ecb.Instantiate(spawnData.EnemyPrefab);
                var spawnAngle = spawnState.ValueRW.Random.NextFloat(0, math.TAU);
                var spawnPoint = new float3
                {
                    x = math.sin(spawnAngle),
                    y = 0,
                    z = math.cos(spawnAngle)
                };
                spawnPoint *= spawnData.SpawnDistance;
                spawnPoint += playerPosition;

                ecb.SetComponent(newEnemy, LocalTransform.FromPosition(spawnPoint));
            }
        }
    }
}
