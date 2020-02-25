using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Unity.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class UpdateAnimationStateSystem : JobComponentSystem
    {
#if UNITY_EDITOR
        public bool DebugAnimation;
        public float DebugNormalizedTime;
#endif
        
        protected override JobHandle OnUpdate(JobHandle jobHandle)
        {
            var dt = Time.DeltaTime;
#if UNITY_EDITOR
            var debugAnimation = DebugAnimation;
            var debugNormalizedTime = DebugNormalizedTime;
#endif            
            return Entities
                .ForEach((DynamicBuffer<AnimationClipInfo> clips,
                ref BonesCount bonesCount, 
                ref AnimationState state, 
                ref BoneIndexOffset offset) =>
            {
                if (state.CurrentAnimationId != state.NewAnimationId)
                {
                    // TODO: implement animation state transition 
                    state.CurrentAnimationId = state.NewAnimationId;
                    state.ClipFinished = false;
                    state.Time = 0f;
                }

                var clip = clips[state.CurrentAnimationId];
                if (!state.ClipFinished)
                    state.Time += dt;
                
#if UNITY_EDITOR
                if (debugAnimation)
                    state.Time = debugNormalizedTime;
#endif            
    
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
                
                var frame = (int) ((clip.FrameCount - 1) * state.NormalizedTime);
                var frameIndex = (clip.Offset  + frame) * bonesCount.Value * 3.0f;

                offset.Value = frameIndex;
            }).Schedule(jobHandle);
        }
    }
}