using Unity.Entities;
using UnityEngine;
using Unity.Transforms;

namespace AML.Survivors
{
    public class PlayerGameObjectPrefab : IComponentData
    {
        public GameObject Value;
    }

    public class PlayerAnimatorReference : ICleanupComponentData
    {
        public Animator Value;
    }

    public class PlayerAnimationAuthoring : MonoBehaviour
    {
        public GameObject PlayerGameObjectPrefab;

        public class PlayerGameObjectPrefabBaker : Baker<PlayerAnimationAuthoring>
        {
            public override void Bake(PlayerAnimationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new PlayerGameObjectPrefab { Value = authoring.PlayerGameObjectPrefab });
            }
        }
    }

  //  /*
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    public partial struct PlayerAnimateSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach( var(localtransform,playerGameObjectPrefab, entity) in SystemAPI.Query<LocalTransform,PlayerGameObjectPrefab>().WithNone<PlayerAnimatorReference>().WithEntityAccess())
            {
                var newCompanionGameObject = Object.Instantiate(playerGameObjectPrefab.Value, localtransform.Position, localtransform.Rotation);
                var newAnimatorReference = new PlayerAnimatorReference
                {
                    Value = newCompanionGameObject.GetComponent<Animator>()
                };
                ecb.AddComponent(entity, newAnimatorReference);
            }

            foreach(var (transform, animatorReference, player) in SystemAPI.Query<LocalTransform, PlayerAnimatorReference, PlayerTag>())
            {
                /*animatorReference.Value.SetTrigger("Walk");*/

                animatorReference.Value.transform.position = transform.Position;
                animatorReference.Value.transform.rotation = transform.Rotation;
            }

            foreach(var (animatorRef, entity) in SystemAPI.Query<PlayerAnimatorReference>().WithNone<PlayerGameObjectPrefab, LocalTransform>().WithEntityAccess())
            {
                Object.Destroy(animatorRef.Value.gameObject);
                ecb.RemoveComponent<EnemyAnimatorReference>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }//*/
}