using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Unity.Rendering
{ 
    public struct AnimationBakedBuffer : ISharedComponentData, IEquatable<AnimationBakedBuffer>
    {
#if UNITY_EDITOR
        public readonly int Length; 
#endif
        private readonly Vector4[] Value;

        public AnimationBakedBuffer(Vector4[] value)
        {
            Value = value;
            Length = value.Length;
        } 

        public ComputeBuffer ToBuffer()
        {
            var buffer = new ComputeBuffer(Value.Length, UnsafeUtility.SizeOf(typeof(Vector4)));
            buffer.SetData(Value);
            return buffer;
        }

        public bool Equals(AnimationBakedBuffer other)
        {
            if (Value == other.Value)
                return true;
            if (Value == null || other.Value == null || Value.Length != other.Value.Length)
                return false;

            for (var i = 0; i < Value.Length; i++)
                if (!Value[i].Equals(other.Value[i]))
                    return false;
            return true;
        }
        public override bool Equals(object obj) => obj is AnimationBakedBuffer other && Equals(other);
        public override int GetHashCode()
        {
            var hash = 0;
            if (Value != null)
            {
                for (var i = 0; i < Value.Length; i++)
                    hash |= Value[i].GetHashCode() << i;
            }

            return hash;
        }
        
    }
}