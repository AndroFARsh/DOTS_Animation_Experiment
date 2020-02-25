using UnityEngine;

namespace AnimBakery.Cook.Model
{
    public struct ClipData
    {
        private string name;
        private float clipLength;
        private WrapMode wrapMode;
        private int framesCount;
        private int start;

        public static ClipData Create(
            string name,
            float length,
            WrapMode wrapMode,
            int start,
            int frameCount)
        {
            return new ClipData
            {
                name            = name,
                clipLength      = length,
                wrapMode        = wrapMode,
                start           = start,
                framesCount     = frameCount
            };
        }
            
        public string Name => name;
        public float ClipLength => clipLength;
        public WrapMode WrapMode => wrapMode;
        public int Start => start;
        public int FramesCount => framesCount;
    }
}