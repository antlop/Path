using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Unity.Burst;

namespace AML.Survivors
{


    public struct SpiritualHedgeAbilityData : IComponentData, IEnableableComponent
    {
        public float CooldownRate;
        public Entity Prefab;
    }
    public struct SpiritualHedgeAbilityUpdateData : IComponentData
    {
        public float CooldownBucket;
        public Random Rand;
    }

    public class SpiritualHedgeAbilityAuthoring : MonoBehaviour
    {
        public float CooldownRate;
        public GameObject prefab;
        public uint randomSeed;

        private class Baker : Baker<SpiritualHedgeAbilityAuthoring>
        {
            public override void Bake(SpiritualHedgeAbilityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpiritualHedgeAbilityData
                {
                    CooldownRate = authoring.CooldownRate,
                    Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
                });
                AddComponent(entity, new SpiritualHedgeAbilityUpdateData
                {
                    CooldownBucket = authoring.CooldownRate,
                    Rand = Random.CreateFromIndex(authoring.randomSeed)
                });
                SetComponentEnabled<SpiritualHedgeAbilityData>(entity, false);
            }
        }
    }

    public partial struct SpiritualHedgeAbilityAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
            var deltaTime = SystemAPI.Time.DeltaTime;

            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>(); //only works if 1 gameobject has the 'PlayerTag'
            var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position.xz;

            foreach (var (updateData, data, transform) in SystemAPI.Query<RefRW<SpiritualHedgeAbilityUpdateData>, SpiritualHedgeAbilityData, LocalTransform>())
            {
                updateData.ValueRW.CooldownBucket -= deltaTime;

                if (updateData.ValueRO.CooldownBucket <= 0)
                {
                    updateData.ValueRW.CooldownBucket = data.CooldownRate;
                    var spawnPosition = new Unity.Mathematics.float3(playerPosition.x, 0, playerPosition.y);

                    var newAttack = ecb.Instantiate(data.Prefab);
                    var rot = quaternion.Euler(new float3(90,0,0));
                    ecb.SetComponent(newAttack, LocalTransform.FromPositionRotation(spawnPosition, rot));
                }
            }
        }
    }
}