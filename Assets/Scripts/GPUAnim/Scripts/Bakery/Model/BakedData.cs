using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace AnimBakery.Cook.Model
{
    [Serializable]
    public class BakedData
    {
        private readonly Vector4[] buffer;
        private readonly float frameRate;
        private readonly int bonesCount;
        private readonly List<ClipData> animations;
     
        public float FrameRate => frameRate;
        
        public Vector4[] Buffer => buffer;
        
        public int BonesCount => bonesCount;
        public int AnimationCount => animations.Count;
        public IReadOnlyCollection<ClipData> Animations => animations;
        public ClipData this[int index] => animations[index];
      
        private BakedData(Vector4[] buffer,
                          float frameRate,
                          int bonesCount,
                          List<ClipData> animations)
        {
            this.buffer = buffer;
            this.frameRate = frameRate;
            this.bonesCount = bonesCount;
            this.animations = animations;
        }

        public static Builder Create()
        {
            return new Builder();
        }

        public class Builder
        {
            private Vector4[] buffer;
            private float frameRate = 30;
            private int bonesCount = -1;
            private readonly List<ClipData> animations = new List<ClipData>();

            public Builder SetBuffer(Vector4[] b)
            {
                buffer = b;
                return this;
            }

            public Builder SetFrameRate(float fr)
            {
                frameRate = fr;
                return this;
            }

            public Builder SetBonesCount(int bc)
            {
                bonesCount = bc;
                return this;
            }

            public Builder AddClip(ClipData clipData)
            {
                animations.Add(clipData);
                return this;
            }

            public BakedData Build()
            {
                if (bonesCount == -1) throw new System.NullReferenceException("Bones count shouldn't be -1");
                if (buffer == null) throw new System.NullReferenceException("Texture shouldn't be null");

                return new BakedData(buffer, frameRate, bonesCount, animations);
            }
        }
    }
}