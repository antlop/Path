using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace AML.Survivors
{

    public class EnemyGameObjectPrefab : IComponentData
    {
        public GameObject Value;
    }

    public class EnemyAnimatorReference : ICleanupComponentData
    {
        public Animator Value;
    }

    public class EnemyAnimatorAuthoring : MonoBehaviour
    {

        public GameObject EnemyGameObjectPrefab;

        public class EnemyGameObjectPrefabBaker : Baker<EnemyAnimatorAuthoring>
        {

            public override void Bake(EnemyAnimatorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new EnemyGameObjectPrefab { Value = authoring.EnemyGameObjectPrefab});
            }
        }
       
    }

    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    public partial struct EnemyAnimateSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach( var(localtransform,enemyGameObjectPrefab, entity) in SystemAPI.Query<LocalTransform,EnemyGameObjectPrefab>().WithNone<EnemyAnimatorReference>().WithEntityAccess())
            {
                var newCompanionGameObject = Object.Instantiate(enemyGameObjectPrefab.Value, localtransform.Position, localtransform.Rotation);
                var newAnimatorReference = new EnemyAnimatorReference
                {
                    Value = newCompanionGameObject.GetComponent<Animator>()
                };
                ecb.AddComponent(entity, newAnimatorReference);
            }

            foreach(var (transform, animatorReference, enemytag) in SystemAPI.Query<LocalTransform, EnemyAnimatorReference, EnemyTag>())
            {
                animatorReference.Value.SetTrigger("Walk");
                animatorReference.Value.transform.position = transform.Position;
                animatorReference.Value.transform.rotation = transform.Rotation;
            }

            foreach(var (animatorRef, entity) in SystemAPI.Query<EnemyAnimatorReference>().WithNone<EnemyGameObjectPrefab, LocalTransform>().WithEntityAccess())
            {
                Object.Destroy(animatorRef.Value.gameObject);
                ecb.RemoveComponent<EnemyAnimatorReference>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
