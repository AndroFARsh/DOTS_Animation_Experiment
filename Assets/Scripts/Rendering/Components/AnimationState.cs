using Unity.Entities;

namespace Unity.Rendering
{ 
    public struct AnimationState : IComponentData
    {
        public int CurrentAnimationId;
        public int NewAnimationId;
        public bool ClipFinished;
        public float Time;
        public float NormalizedTime;
    }
}