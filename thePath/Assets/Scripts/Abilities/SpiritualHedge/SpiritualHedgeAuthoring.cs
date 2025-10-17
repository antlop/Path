using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using UnityEditor.PackageManager;

namespace AML.Survivors
{
    public struct SpiritualHedgeFlag : IComponentData, IEnableableComponent { }

    public struct SpiritualHedgeData : IComponentData
    {
        public int DamageReduction;
        public float Lifetime;
    }

    public struct SpiritualHedgeUpdateData : IComponentData
    {
        public float LifetimeBucket;
    }

    public class SpiritualHedgeAuthoring : MonoBehaviour
    {
        public int DamageReduction;
        public float Lifetime;

        private class Baker : Baker<SpiritualHedgeAuthoring>
        {
            public override void Bake(SpiritualHedgeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpiritualHedgeData
                {
                    DamageReduction = authoring.DamageReduction,
                    Lifetime = authoring.Lifetime
                });
                AddComponent(entity, new SpiritualHedgeUpdateData
                {
                    LifetimeBucket = authoring.Lifetime
                });
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);

                PlayerStatSheet.instance.DamageReduction = authoring.DamageReduction;
            }
        }
    }

    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct MoveHedgeWithPlayerSystem : ISystem
    {

        public void OnUpdate(ref SystemState state)
        {
            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>(); //only works if 1 gameobject has the 'PlayerTag'
            var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

            foreach (var hedgeTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<SpiritualHedgeUpdateData>())
            {
                hedgeTransform.ValueRW.Position = playerPosition;
            }
        }
    }


    public partial struct SpiritualHedgeSystem : ISystem
    {
        public ComponentLookup<SpiritualHedgeFlag> HedgeFlagLookup;
        public ComponentLookup<DestroyEntityFlag> DestroyEntityFlagLookup;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            HedgeFlagLookup = SystemAPI.GetComponentLookup<SpiritualHedgeFlag>();
            DestroyEntityFlagLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>();
            var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (updateData, data, entity) in SystemAPI.Query<RefRW<SpiritualHedgeUpdateData>, RefRO<SpiritualHedgeData>>().WithEntityAccess())
            {
                updateData.ValueRW.LifetimeBucket -= deltaTime;
                if( updateData.ValueRO.LifetimeBucket < 0 )
                {
                    // destroy the spiritualhedge and set the flag to false
                    HedgeFlagLookup.SetComponentEnabled(playerEntity, false);
                    DestroyEntityFlagLookup.SetComponentEnabled(entity, true);

                    PlayerStatSheet.instance.DamageReduction -= data.ValueRO.DamageReduction;
                }
            }
        }
    }
}