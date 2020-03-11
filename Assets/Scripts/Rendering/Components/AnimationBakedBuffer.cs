using System;
using System.Linq;
using AnimBakery.Cook;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Unity.Rendering
{ 
    public struct AnimationBakedBuffer : ISharedComponentData, IEquatable<AnimationBakedBuffer>
    {
        private readonly Vector4[] Value;

        public int Length => Value?.Length ?? 0;

        public AnimationBakedBuffer(Vector4[] value)
        {
            Value = value;
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

        public void SetPixels(Texture2D texture)
        {
            for (var index=0; index<Length; ++index)
            {
                var xy = BakeryUtils.To2D(index, texture.width);
                texture.SetPixel(xy.x, xy.y, Value[index]);
            }
        }
    }
}