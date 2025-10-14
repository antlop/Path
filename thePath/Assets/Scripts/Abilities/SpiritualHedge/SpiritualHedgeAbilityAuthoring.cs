using Unity.Entities;
using UnityEngine;

namespace AML.Survivors
{


    public struct SpiritualHedgeAbilityData : IComponentData, IEnableableComponent
    {
        public float CooldownRate;
    }
    public struct SpiritualHedgeAbilityUpdateData : IComponentData
    {
        public float CooldownBucket;
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
                });
                AddComponent(entity, new SpiritualHedgeAbilityUpdateData
                {
                });
                SetComponentEnabled<SpiritualHedgeAbilityData>(entity, false);
            }
        }
    }

}