using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Unity.Rendering
{
    public class UpdateAnimationStateSystem : JobComponentSystem
    {
#if UNITY_EDITOR
        public bool DebugAnimation;
        public float DebugNormalizedTime;
#endif

        private EntityQuery entityQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new []
                {
                    ComponentType.ReadWrite<AnimationState>(), ComponentType.ReadWrite<BoneIndexOffset>(),
                    ComponentType.ReadOnly<AnimationClipInfo>()
                },
                Any = new [] { ComponentType.ReadOnly<BonesCount>(), ComponentType.ReadOnly<AnimationTimeMutator>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle jobHandle)
        {
            jobHandle = new UpdateAnimationStateJob
            {
#if UNITY_EDITOR
                debugAnimation = DebugAnimation,
                debugNormalizedTime = DebugNormalizedTime,
#endif
                dt = Time.DeltaTime,
                clipType = GetArchetypeChunkBufferType<AnimationClipInfo>(true),
                bonesCountType = GetArchetypeChunkComponentType<BonesCount>(true),
                animationTimeMutatorType = GetArchetypeChunkComponentType<AnimationTimeMutator>(true),
                animationStateType = GetArchetypeChunkComponentType<AnimationState>(),
                boneIndexOffsetType = GetArchetypeChunkComponentType<BoneIndexOffset>(),
            }.Schedule(entityQuery, jobHandle);
            return jobHandle;
        }

        #region Jobs

        private struct UpdateAnimationStateJob : IJobChunk
        {
            [ReadOnly] public bool debugAnimation;
            [ReadOnly] public float debugNormalizedTime;
            
            [ReadOnly] public float dt;
            [ReadOnly] public ArchetypeChunkBufferType<AnimationClipInfo> clipType;
            [ReadOnly] public ArchetypeChunkComponentType<BonesCount> bonesCountType;
            [ReadOnly] public ArchetypeChunkComponentType<AnimationTimeMutator> animationTimeMutatorType;
            
            public ArchetypeChunkComponentType<AnimationState> animationStateType;
            public ArchetypeChunkComponentType<BoneIndexOffset> boneIndexOffsetType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var clipBufferAccessor = chunk.GetBufferAccessor(clipType);
                var bonesCountArray = chunk.GetNativeArray(bonesCountType);
                var animationStateArray = chunk.GetNativeArray(animationStateType);
                
                var hasBoneIndexOffset = chunk.Has(boneIndexOffsetType);
                if (!hasBoneIndexOffset) 
                    return;
                var boneIndexOffsetArray = chunk.GetNativeArray(boneIndexOffsetType);

                var hasAnimationTimeMutator = chunk.Has(animationTimeMutatorType);
                var animationTimeMutatorArray = hasAnimationTimeMutator 
                    ? chunk.GetNativeArray(animationTimeMutatorType)
                    : default;
                for (var i = 0; i < chunk.Count; ++i)
                {
                    var state = animationStateArray[i];
                    var bonesCount = bonesCountArray[i];
                    
                    var clips = clipBufferAccessor[i];
                    
                    if (state.CurrentAnimationId != state.NewAnimationId)
                    {
                        // TODO: implement animation state transition 
                        state.CurrentAnimationId = state.NewAnimationId;
                        state.ClipFinished = false;
                        state.Time = 0f;
                    }

                    var clip = clips[state.CurrentAnimationId];
                    if (!state.ClipFinished)
                    {
                        state.Time += dt;
                        if (hasAnimationTimeMutator)
                            state.Time += dt * animationTimeMutatorArray[i].Value;
                    }

                    if (debugAnimation)
                        state.Time = clip.ClipLength * debugNormalizedTime;

                    var currentTime = state.Time;
                    switch (clip.WrapMode)
                    {
                        case AnimationClipInfo.Mode.PingPong:
                            if (2 * clip.ClipLength <= state.Time)
                                currentTime = state.Time %= clip.ClipLength;
                            else if (clip.ClipLength <= state.Time)
                                currentTime = 2 * clip.ClipLength - state.Time;
                            break;
                        case AnimationClipInfo.Mode.OnceEndForever:
                            if (clip.ClipLength - state.Time < 0.0)
                            {
                                currentTime = state.Time = clip.ClipLength;
                                state.ClipFinished = true;
                            }

                            break;
                        case AnimationClipInfo.Mode.OnceStartForever:
                            if (clip.ClipLength - state.Time < 0.0)
                            {
                                currentTime = state.Time = 0;
                                state.ClipFinished = true;
                            }

                            break;
                        case AnimationClipInfo.Mode.Loop:
                        default:
                            if (clip.ClipLength - state.Time < 0.0)
                            {
                                currentTime = state.Time %= clip.ClipLength;
                            }

                            break;
                    }

                    state.NormalizedTime = currentTime / clip.ClipLength;
                    animationStateArray[i] = state;
                    
                    var frame = (int) ((clip.FrameCount - 1) * state.NormalizedTime);
                    var frameIndex = (clip.Offset + frame) * bonesCount.Value * 3.0f;

                    boneIndexOffsetArray[i] = new BoneIndexOffset{ Value = frameIndex };
                }
            }
        }

        #endregion
    }
}