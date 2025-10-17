using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace AML.Survivors
{
    public struct InitializeCharacterFlag : IComponentData, IEnableableComponent { }

    public struct CharacterMoveDirection: IComponentData
    {
        public float2 Value;
    }
    public struct CharacterMoveSpeed : IComponentData
    {
        public float Value;
    }

    public struct  CharacterMaxHitPoints : IComponentData
    {
        public int Value;
    }

    public struct CharacterCurrentHitPoints : IComponentData
    {
        public int Value;
    }

    public struct DamageThisFrame : IBufferElementData //data type is like a list
    {
        public int Value;
    }

    public class CharacterAuthoring : MonoBehaviour
    {
        public float MoveSpeed;
        public int HitPoints;
        private class Baker : Baker<CharacterAuthoring>
        {
            public override void Bake(CharacterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<InitializeCharacterFlag>(entity);
                AddComponent<CharacterMoveDirection>(entity); 
                AddComponent(entity, new CharacterMoveSpeed { Value = authoring.MoveSpeed });
                AddComponent(entity, new CharacterMaxHitPoints { Value = authoring.HitPoints });
                AddComponent(entity, new CharacterCurrentHitPoints { Value = authoring.HitPoints });
                /*var asdf = */AddBuffer<DamageThisFrame>(entity);
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))] 
    public partial struct CharacterInitializationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach(var (mass, shouldInitialize) in SystemAPI.Query<RefRW<PhysicsMass>, EnabledRefRW<InitializeCharacterFlag>>())
            {
                mass.ValueRW.InverseInertia = float3.zero;
                shouldInitialize.ValueRW = false;
            }
        }
    }

    public partial struct CharacterMoveSystem : ISystem
    {
        [BurstCompile]
         public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach(var (velocity, direction, speed) in SystemAPI.Query<RefRW<PhysicsVelocity>, CharacterMoveDirection, CharacterMoveSpeed>())
            {
                var moveStep2d = direction.Value * speed.Value;
                //transform.ValueRW.Position += new float3(moveStep2d.x, 0f, moveStep2d.y);
                velocity.ValueRW.Linear = new float3(moveStep2d.x, 0f, moveStep2d.y);
            }
        }
    }

    [BurstCompile]
    public partial struct ProcessDamageThisFrameSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach(var (hitpoints, damagethisframe, entity) in SystemAPI.Query<RefRW<CharacterCurrentHitPoints>, DynamicBuffer<DamageThisFrame>>().WithPresent<DestroyEntityFlag>().WithEntityAccess())
            {
                if(damagethisframe.IsEmpty) continue;
                int dmgReduction = 0;
                if (SystemAPI.HasComponent<SpiritualHedgeFlag>(entity) && SystemAPI.IsComponentEnabled<SpiritualHedgeFlag>(entity))
                {
                    dmgReduction = PlayerStatSheet.instance.DamageReduction;
                }
                foreach (var damage in damagethisframe)
                {
                    hitpoints.ValueRW.Value -= (damage.Value - dmgReduction);
                }
                damagethisframe.Clear();

                if(hitpoints.ValueRO.Value <= 0)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
            }
        }
    }
}
