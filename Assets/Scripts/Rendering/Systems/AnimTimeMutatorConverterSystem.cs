using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Rendering
{
    [UpdateAfter(typeof(AnimationConversionSystem))]
    [UpdateAfter(typeof(AnimatorConversionSystem))]
    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    internal class AnimationTimeMutatorRangeConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Authoring.AnimationTimeMutatorRange rangeMutator) =>
            {
                var mutator = new Unity.Rendering.AnimationTimeMutatorRange { Min = rangeMutator.min, Max = rangeMutator.max };
                var renderers = rangeMutator.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
                foreach (var renderer in renderers)
                {
                    foreach (var rendererEntity in GetEntities(renderer))
                    {
                        if (DstEntityManager.HasComponent<AnimatedTag>(rendererEntity))
                        {
                            DstEntityManager.AddComponentData(rendererEntity, mutator);
                        }
                    }
                }
            });
        }
    }


    [UpdateInGroup(typeof(InitializationSystemGroup))]
    internal class AnimationTimeMutatorRangeToAnimationTimeMutatorSystem : JobComponentSystem
    {
        private static readonly Random Random = new Random(0x6E774EB7u);
        private EndInitializationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            commandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle jobHandle)
        {
            var random = Random;
            var commandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent();

            jobHandle = Entities
                .WithNone<AnimationTimeMutator>()
                .ForEach((Entity entity, int entityInQueryIndex, ref AnimationTimeMutatorRange range) =>
                {
                    commandBuffer.AddComponent(entityInQueryIndex, entity,
                        new AnimationTimeMutator {Value = random.NextFloat(range.Min, range.Max)});
                })
                .Schedule(jobHandle);
            commandBufferSystem.AddJobHandleForProducer(jobHandle);
            return jobHandle;
        }
    }
}