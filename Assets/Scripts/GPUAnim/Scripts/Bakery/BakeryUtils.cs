using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimBakery.Cook
{
    public static class  BakeryUtils
    {
        public static int NextPowerOfTwo(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v;
        }

        private static Mesh CopyMesh(Mesh originalMesh)
        {
            if (originalMesh == null) return null;
            
            return new Mesh
            {
                vertices = originalMesh.vertices,
                triangles = originalMesh.triangles,
                normals = originalMesh.normals,
                uv = originalMesh.uv,
                tangents = originalMesh.tangents,
                name = originalMesh.name
            };
        }

        public static Mesh CopyAndFillBonesUV(Mesh originalMesh)
        {
            var newMesh = CopyMesh(originalMesh);
            
            var boneWeights= originalMesh.boneWeights;
            var vertexCount   = originalMesh.vertexCount;
            
            var uv           = new List<Vector4>(vertexCount);
            for (var i = 0; i < vertexCount; i++)
            {
                var bw = boneWeights[i];
                var bonesWeightsSorted = new List<Tuple<int, float>>(4)
                {
                    Tuple.Create(bw.boneIndex0, bw.weight0),
                    Tuple.Create(bw.boneIndex1, bw.weight1),
                    Tuple.Create(bw.boneIndex2, bw.weight2),
                    Tuple.Create(bw.boneIndex3, bw.weight3)
                };
                bonesWeightsSorted.Sort((b1, b2) => b1.Item2 < b2.Item2 ? 1 : -1);

                uv.Add(new Vector4
                {
                    x = bonesWeightsSorted[0].Item1,
                    y = bonesWeightsSorted[0].Item2,
                    z = bonesWeightsSorted[1].Item1,
                    w = bonesWeightsSorted[1].Item2
                });
            }

            newMesh.SetUVs(2, uv);
            return newMesh;
        }


        public static void FillMeshBonesUV(Mesh originalMesh)
        {
            var boneWeights= originalMesh.boneWeights;
            var vertexCount   = originalMesh.vertexCount;
            
            var uv1           = new List<Vector4>(vertexCount);
            // var uv2           = new List<Vector4>(vertexCount);
            for (var i = 0; i < vertexCount; i++)
            {
                var bw = boneWeights[i];
                var bonesWeightsSorted = new List<Tuple<int, float>>(4)
                {
                    Tuple.Create(bw.boneIndex0, bw.weight0),
                    Tuple.Create(bw.boneIndex1, bw.weight1),
                    Tuple.Create(bw.boneIndex2, bw.weight2),
                    Tuple.Create(bw.boneIndex3, bw.weight3)
                };
                bonesWeightsSorted.Sort((b1, b2) => b1.Item2 < b2.Item2 ? 1 : -1);

                uv1.Add(new Vector4
                {
                    x = bonesWeightsSorted[0].Item1,
                    y = bonesWeightsSorted[0].Item2,
                    z = bonesWeightsSorted[1].Item1,
                    w = bonesWeightsSorted[1].Item2
                });

                // uv2.Add(new Vector4
                // {
                //     x = bonesWeightsSorted[2].Item1,
                //     y = bonesWeightsSorted[2].Item2,
                //     z = bonesWeightsSorted[3].Item1,
                //     w = bonesWeightsSorted[3].Item2
                // });
            }

            originalMesh.SetUVs(2, uv1);
            //originalMesh.SetUVs(2, uv2);
        }

        public static List<AnimationClip> GetAllAnimationClips(Animation animation)
        {
            if (animation == null) return new List<AnimationClip>();

            var animationClips = new List<AnimationClip>(animation.GetClipCount());
            foreach (var obj in animation)
            {
                var state = obj as AnimationState;
                if (state != null)
                    animationClips.Add(state.clip);
            }
            
            animationClips.Sort((c1, c2) 
                => string.CompareOrdinal(c1.name, c2.name));
            
            return animationClips;
        }
        
        public static List<AnimationClip> GetAllAnimationClips(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return new List<AnimationClip>();
            
            var controller = animator.runtimeAnimatorController;
            var animationClips = new List<AnimationClip>(controller.animationClips);
            animationClips.Sort((c1, c2) => string.CompareOrdinal(c1.name, c2.name));
            return animationClips;
        }

        public static string Format(Vector2Int v)
        {
            return "(" + v.x + ", " + v.y + ")";
        }

        public static string Format(Color v)
        {
            return "(" + v.r + ", " + v.g + ", " + v.b + ", " + v.a + ")";
        }

        public static Vector2Int To2D(int index, int width)
        {
            var y = index / width;
            var x = index % width;
            return new Vector2Int(x, y);
        }
    }
}