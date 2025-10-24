using System.ComponentModel;
using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Burst;

namespace AML.Survivors
{
    public struct XPOrbTag : IComponentData { }


    public class XPOrbAuthoring : MonoBehaviour
    {
        private class Baker : Baker<XPOrbAuthoring>
        {
            public override void Bake(XPOrbAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<XPOrbTag>(entity);
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }

    public partial struct CollectXPOrbSystem:  ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var newCollectJob = new CollectXPOrbJob
            {
                xporbLookup = SystemAPI.GetComponentLookup<XPOrbTag>(true),
                xpbsCollectedCountLookup = SystemAPI.GetComponentLookup<XPOrbsCollectedCount>(),
                destroyEntityFlagLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>(),
                updateXPOrbFlagLookup = SystemAPI.GetComponentLookup<UpdateXPOrbFlag>()
            };

            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = newCollectJob.Schedule(simulationSingleton, state.Dependency);
            state.CompleteDependency();
        }
    }

    [BurstCompile]
    public struct CollectXPOrbJob : ITriggerEventsJob
    {
        [Unity.Collections.ReadOnly] public ComponentLookup<XPOrbTag> xporbLookup;
        public ComponentLookup<DestroyEntityFlag> destroyEntityFlagLookup;
        public ComponentLookup<XPOrbsCollectedCount> xpbsCollectedCountLookup;
        public ComponentLookup<UpdateXPOrbFlag> updateXPOrbFlagLookup;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity XPEntity;
            Entity playerEntity;

            if (xporbLookup.HasComponent(triggerEvent.EntityA) && xpbsCollectedCountLookup.HasComponent(triggerEvent.EntityB))
            {
                XPEntity = triggerEvent.EntityA;
                playerEntity = triggerEvent.EntityB;
            }
            else if (xporbLookup.HasComponent(triggerEvent.EntityB) && xpbsCollectedCountLookup.HasComponent(triggerEvent.EntityA))
            {
                XPEntity = triggerEvent.EntityB;
                playerEntity = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            var orbsCollected = xpbsCollectedCountLookup[playerEntity];
            orbsCollected.Value += 1;
            xpbsCollectedCountLookup[playerEntity] = orbsCollected;

            updateXPOrbFlagLookup.SetComponentEnabled(playerEntity, true);

            destroyEntityFlagLookup.SetComponentEnabled(XPEntity, true);
        }
    }
}
