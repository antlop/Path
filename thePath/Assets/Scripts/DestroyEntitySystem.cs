using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace AML.Survivors
{
    public struct DestroyEntityFlag : IComponentData, IEnableableComponent { }


    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial struct DestroyEntitySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state) 
        { 
            var endEcbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var endEcb = endEcbSystem.CreateCommandBuffer(state.WorldUnmanaged);
            var beginEcbSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var beginecb = beginEcbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            foreach ( var (_,entity) in SystemAPI.Query<DestroyEntityFlag>().WithEntityAccess())
            {
                if(SystemAPI.HasComponent<PlayerTag>(entity))
                {
                    // access a Unity Monobehaviour singleton objects method
                }

                if(SystemAPI.HasComponent<EXP_OrbPrefab>(entity))
                {
                    var xpprefab = SystemAPI.GetComponent<EXP_OrbPrefab>(entity).Value;
                    var neworb = beginecb.Instantiate(xpprefab);

                    var spawnPos = SystemAPI.GetComponent<LocalToWorld>(entity).Position;
                    beginecb.SetComponent(neworb, LocalTransform.FromPosition(spawnPos));
                }

                endEcb.DestroyEntity(entity);
            }
        }
    }
}
