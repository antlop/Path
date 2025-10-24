using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace AML.Survivors
{

    public struct FlamingSwordAbilityData : IComponentData, IEnableableComponent
    {
        public float Range;
        public float CooldownRate;
        public Entity Prefab;
    }
    public struct FlamingSwordAbilityUpdateData : IComponentData
    {
        public float CooldownBucket;
        public Random Rand;
    }

    public class FlamingSwordAbilityAuthoring : MonoBehaviour
    {
        public float Range;
        public float CooldownRate;
        public GameObject prefab;
        public uint randomSeed;

        private class Baker : Baker<FlamingSwordAbilityAuthoring>
        {
            public override void Bake(FlamingSwordAbilityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new FlamingSwordAbilityData
                {
                    CooldownRate = authoring.CooldownRate,
                    Range = authoring.Range,
                    Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
                });
                AddComponent(entity, new FlamingSwordAbilityUpdateData
                {
                    CooldownBucket = authoring.CooldownRate,
                    Rand = Random.CreateFromIndex(authoring.randomSeed)
                });
                SetComponentEnabled<FlamingSwordAbilityData>(entity, false);
            }
        }
    }

    public partial struct FlamingSwordAbilityAttackSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecbSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
            var deltaTime = SystemAPI.Time.DeltaTime;

            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>(); //only works if 1 gameobject has the 'PlayerTag'
            var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position.xz;

            foreach (var (updateData, data, transform) in SystemAPI.Query<RefRW<FlamingSwordAbilityUpdateData>, FlamingSwordAbilityData, LocalTransform>())
            {
                updateData.ValueRW.CooldownBucket -= deltaTime;

                if (updateData.ValueRO.CooldownBucket <= 0)
                {
                    updateData.ValueRW.CooldownBucket = data.CooldownRate;

                    float rand = updateData.ValueRW.Rand.NextFloat(0, 360);

                    var spawnPosition = new Unity.Mathematics.float3(playerPosition.x, 0, playerPosition.y);
                    var rotation = Quaternion.identity;
                    rotation.eulerAngles = new Vector3(0, rand, 0);

                    var newAttack = ecb.Instantiate(data.Prefab);
                    ecb.SetComponent(newAttack, LocalTransform.FromPositionRotation(spawnPosition, rotation));
                }
            }
        }
    }
}