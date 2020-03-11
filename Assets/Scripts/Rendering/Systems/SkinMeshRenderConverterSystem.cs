#define REMOVE_USELESS_ENTITY_FROM_SKINNED_MESH
#if REMOVE_USELESS_ENTITY_FROM_SKINNED_MESH

using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Rendering
{
    internal struct KeepTag : IComponentData
    {
    }

    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    internal class SkinnedMeshRendererConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((SkinnedMeshRenderer renderer) =>
            {
                foreach (var bone in renderer.bones)
                {
                    foreach (var boneEntity in GetEntities(bone))
                    {
                        DstEntityManager.AddComponent<Disabled>(boneEntity);
                    }
                }

                var entity = GetPrimaryEntity(renderer);
                DstEntityManager.AddComponent<KeepTag>(entity);

                var rootEntity = entity;
                do
                {
                    if (!DstEntityManager.HasComponent<Parent>(rootEntity))
                    {
                        DstEntityManager.AddComponent<KeepTag>(rootEntity);
                        if (!rootEntity.Equals(entity))
                        {
                            DstEntityManager.AddComponentData(entity, new Parent {Value = rootEntity});
                        }

                        rootEntity = Entity.Null;
                    }
                    else
                    {
                        rootEntity = DstEntityManager.GetComponentData<Parent>(rootEntity).Value;
                    }
                } while (!rootEntity.Equals(Entity.Null));
            });
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    internal class CleanupSkinnedMeshRendererSystem : JobComponentSystem
    {
        private EntityCommandBufferSystem commandBufferSystem;
        private EntityQuery entityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
            entityQuery = GetEntityQuery(ComponentType.ReadOnly<KeepTag>(),
                ComponentType.ReadOnly<LinkedEntityGroup>());
        }

        protected override JobHandle OnUpdate(JobHandle jobHandle)
        {
            jobHandle = new CleanupSkinnedMeshRendererJob
            {
                commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                keepTag = GetComponentDataFromEntity<KeepTag>(true),
            }.Schedule(entityQuery, jobHandle);
            commandBufferSystem.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }

        #region Jobs

        [RequireComponentTag(typeof(KeepTag))]
        private struct CleanupSkinnedMeshRendererJob : IJobForEachWithEntity_EB<LinkedEntityGroup>
        {
            [ReadOnly] public ComponentDataFromEntity<KeepTag> keepTag;

            public EntityCommandBuffer.Concurrent commandBuffer;

            public void Execute(Entity entity, int index, [ReadOnly] DynamicBuffer<LinkedEntityGroup> linkedGroup)
            {
                using (var linkedList = new NativeList<LinkedEntityGroup>(linkedGroup.Length, Allocator.Temp))
                {
                    foreach (var linked in linkedGroup)
                    {
                        if (keepTag.HasComponent(linked.Value))
                        {
                            commandBuffer.RemoveComponent<KeepTag>(index, linked.Value);
                            linkedList.Add(linked);
                        }
                        else
                        {
                            commandBuffer.DestroyEntity(index, linked.Value);
                        }
                    }

                    commandBuffer.SetBuffer<LinkedEntityGroup>(index, entity).CopyFrom(linkedList);
                    commandBuffer.RemoveComponent<KeepTag>(index, entity);
                }
            }
        }

        #endregion
    }
}

#endif