using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Unity.Rendering
{
    public struct AnimationTimeMutator : IComponentData
    {
        public float Value;
    }

    public struct AnimationTimeMutatorRange : IComponentData
    {
        public float Min;
        public float Max;
    }

    namespace Authoring
    {
        public class AnimationTimeMutatorRange : UnityEngine.MonoBehaviour
        {
            [UnityEngine.Range(-1, 0)] public float min;
            [UnityEngine.Range(0, 1)] public float max;
        }
    }
}