using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace AML.Survivors
{
    public struct PillarOfSaltAbilityData : IComponentData
    {
        public float Range;
        public float CooldownRate;
    }
    public struct PillarOfSaltAbilityUpdateData : IComponentData
    {
        public float CooldownBucket;
    }

    public class PillarOfSaltAbilityAuthoring : MonoBehaviour
    {
        public float Range;
        public float CooldownRate;

        private class Baker : Baker<PillarOfSaltAbilityAuthoring>
        {
            public override void Bake(PillarOfSaltAbilityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PillarOfSaltAbilityData
                {
                    CooldownRate = authoring.CooldownRate,
                    Range = authoring.Range
                });
                AddComponent(entity, new PillarOfSaltAbilityUpdateData
                {
                    CooldownBucket = authoring.CooldownRate
                });
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }

    //var newAttack = ecb.Instantiate(attackData.AttackPrefab);

    public partial struct PillarOfSaltAbilityAttackSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach(var (updateData, data, transform) in SystemAPI.Query<RefRW<PillarOfSaltAbilityUpdateData>, PillarOfSaltAbilityData,LocalTransform>())
            {

            }
        }
    }
}