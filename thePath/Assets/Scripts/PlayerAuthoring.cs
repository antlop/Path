using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace AML.Survivors
{

    public struct PlayerTag : IComponentData { }

    public struct CameraTarget : IComponentData 
    { 
        public UnityObjectRef<Transform> CameraTransform;
    }

    public struct InitializeCameraTargetTag : IComponentData { }

    public struct PlayerAttackData : IComponentData
    {
        public Entity AttackPrefab;
        public float CooldownTime;
        public float3 DetectionSize;
        public CollisionFilter CollisionFilter;
    }

    public struct PlayerCooldownExpirationTimestamp : IComponentData
    {
        public double Value;
    }

    public struct XPOrbsCollectedCount : IComponentData
    {
        public int Value;
    }

    public struct UpdateXPOrbFlag : IComponentData, IEnableableComponent { }

    public class PlayerAuthoring : MonoBehaviour
    {
        public GameObject AttackPrefab;
        public float CooldownTime;
        public float DetectionSize;

        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerTag>(entity);
                AddComponent<InitializeCameraTargetTag>(entity);
                AddComponent<CameraTarget>(entity);

                var enemyLayer = LayerMask.NameToLayer("Enemy");
                var enemyLayerMask = (uint)math.pow(2, enemyLayer);

                var attackCollisionFilter = new CollisionFilter
                {
                    BelongsTo = uint.MaxValue,
                    CollidesWith = enemyLayerMask
                };

                AddComponent(entity, new PlayerAttackData
                {
                    AttackPrefab = GetEntity(authoring.AttackPrefab, TransformUsageFlags.Dynamic),
                    CooldownTime = authoring.CooldownTime,
                    DetectionSize = new float3(authoring.DetectionSize),
                    CollisionFilter = attackCollisionFilter
                }); 
                AddComponent<PlayerCooldownExpirationTimestamp>(entity);
                AddComponent<XPOrbsCollectedCount>(entity);
                AddComponent<UpdateXPOrbFlag>(entity);
                AddComponent<SpiritualHedgeFlag>(entity);
                SetComponentEnabled<SpiritualHedgeFlag>(entity,false);
            }
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))] 
    public partial struct CameraInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InitializeCameraTargetTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (CameraTargetSingleton.instance == null) return;
            var cameraTargetTransform = CameraTargetSingleton.instance.transform;

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator); //temp, tempjob, persistant
            foreach(var (cameraTarget, entity) in SystemAPI.Query<RefRW<CameraTarget>>().WithAll<InitializeCameraTargetTag, PlayerTag>().WithEntityAccess())
            {
                cameraTarget.ValueRW.CameraTransform = cameraTargetTransform;
                ecb.RemoveComponent<InitializeCameraTargetTag>(entity);
            }

            // the commands given to the ecb don't happen until this call. we are just telling ecb what we 'want' it to do later
            ecb.Playback(state.EntityManager);
        }
    }

    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct MoveCameraSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach(var (transform, cameraTarget) in SystemAPI.Query<LocalToWorld, CameraTarget>().WithAll<PlayerTag>().WithNone<InitializeCameraTargetTag>())
            {
                cameraTarget.CameraTransform.Value.position = transform.Position;
            }
        }
    }

    public partial class PlayerInputSystem : SystemBase
    {
        private SurvivorInput _input;

        protected override void OnCreate()
        {
            _input = new SurvivorInput();
            _input.Enable();
        }

        protected override void OnUpdate()
        {
            var currentInput = (float2)_input.Player.Move.ReadValue<Vector2>();
            foreach(var direction in SystemAPI.Query<RefRW<CharacterMoveDirection>>().WithAll<PlayerTag>())
            {
                direction.ValueRW.Value = currentInput;
            }
        }
    }

    public partial struct  PlayerAttackSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = SystemAPI.Time.ElapsedTime;

            var ecbSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            foreach( var (expirationTimeStamp, attackData, transform) in SystemAPI.Query<RefRW<PlayerCooldownExpirationTimestamp>, PlayerAttackData, LocalTransform>()) 
            {
                if (expirationTimeStamp.ValueRO.Value > elapsedTime) continue;

                var spawnPosition = transform.Position;
                var mindetectposition = spawnPosition - attackData.DetectionSize;
                var maxdetectposition = spawnPosition + attackData.DetectionSize;

                // AABB
                var aabbInput = new OverlapAabbInput
                {
                    Aabb = new Aabb
                    {
                        Min = mindetectposition,
                        Max = maxdetectposition
                    },
                    Filter = attackData.CollisionFilter
                };
                var overlapHits = new NativeList<int>(state.WorldUpdateAllocator);
                if(!physicsWorldSingleton.OverlapAabb(aabbInput, ref overlapHits))
                {
                    continue;
                }

                var maxDistanceSq = float.MaxValue;
                var closestEnemyPosition = float3.zero;
                foreach( var overlapHit in overlapHits )
                {
                    var curEnemyPosition = physicsWorldSingleton.Bodies[overlapHit].WorldFromBody.pos;
                    var distanceToPlayerSq = math.distancesq(spawnPosition.xz, curEnemyPosition.xz);
                    if(distanceToPlayerSq < maxDistanceSq)
                    {
                        maxDistanceSq = distanceToPlayerSq;
                        closestEnemyPosition = curEnemyPosition;
                    }
                }

                var vectorToClosestEnemy = closestEnemyPosition.xz - spawnPosition.xz;
                var angleToClosestEnemy = math.atan2(-vectorToClosestEnemy.y, vectorToClosestEnemy.x);
                var spawnOreintation = quaternion.Euler(0f, angleToClosestEnemy, 0f);

                var newAttack = ecb.Instantiate(attackData.AttackPrefab);
                ecb.SetComponent(newAttack, LocalTransform.FromPositionRotation(spawnPosition, spawnOreintation));

                expirationTimeStamp.ValueRW.Value = elapsedTime + attackData.CooldownTime;
            }
        }
    }

    public partial struct UpdateOrbUISystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach(var (orbCount, shouldUpdateUI) in SystemAPI.Query<XPOrbsCollectedCount, EnabledRefRW<UpdateXPOrbFlag>>())
            {
                GameUIController.instance.UpdateEXOrbsCollectedText(orbCount.Value);
                shouldUpdateUI.ValueRW = false;
            }
        }
    }
}