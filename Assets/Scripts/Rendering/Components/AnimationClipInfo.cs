using System;
using Unity.Entities;
using UnityEngine;

namespace Unity.Rendering
{
    public struct AnimationClipInfo : IBufferElementData
    {
        public enum Mode
        {
            /** When time reaches the end of the animation clip, time will continue at the beginning. **/
            Loop = 0,

            /** When time reaches the end of the animation clip, the clip will automatically stop playing and time will be reset to beginning of the clip. **/
            OnceStartForever = 1,

            /** Plays back the animation. When it reaches the end, it will keep playing the last frame and never stop playing. **/
            OnceEndForever = 2,

            /** When time reaches the end of the animation clip, time will ping pong back between beginning and end. **/
            PingPong = 4,
        }

        public Mode WrapMode;
        public float ClipLength;
        public int Offset;
        public int FrameCount;

        public static Mode ToMode(WrapMode mode)
        {
            switch (mode)
            {
                case UnityEngine.WrapMode.Once:
                    return Mode.OnceStartForever;
                case UnityEngine.WrapMode.ClampForever:
                    return Mode.OnceEndForever;
                case UnityEngine.WrapMode.PingPong:
                    return Mode.OnceEndForever;
                case UnityEngine.WrapMode.Default:
                case UnityEngine.WrapMode.Loop:
                    return Mode.Loop;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}