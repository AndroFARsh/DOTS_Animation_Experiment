using System.Collections.Generic;
using AnimBakery.Cook.Model;
using UnityEngine;

namespace AnimBakery.Cook
{
   public static class Bakery
    {
        private const int MATRIX_ROWS_COUNT = 3;
        
        /// <summary>
        /// Bake all animation clips to texture in format:
        /// [clip0[frame0[bone0[row0, row1, row0]..boneN[row0, row1, row0]]..frameM[bone0[row0, row1, row0]..boneN[row0, row1, row0]]]..clipK[..
        /// </summary>
        /// <returns>BakedData - baked animation matrix to texture</returns>
        public static BakedData BakeClips(GameObject prototype, float frameRate = 30f)
        {
            var go = Object.Instantiate(prototype);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.SetActive(true);
            
            var skinnedMeshRenderer = go.GetComponentInChildren<SkinnedMeshRenderer>();

            var sampledBoneMatrices = SampleAnimationClips(go, 
                                                           skinnedMeshRenderer,
                                                           frameRate, 
                                                           out var animationClips,
                                                           out var numberOfKeyFrames, 
                                                           out var numberOfBones);
            var buffer = new Vector4[numberOfBones * numberOfKeyFrames * MATRIX_ROWS_COUNT];
            
            var bakedDataBuilder = BakedData.Create()
                .SetFrameRate(frameRate)
                .SetBonesCount(numberOfBones);
            
            var clipOffset = 0;
            for (var clipIndex = 0; clipIndex < sampledBoneMatrices.Count; clipIndex++)
            {
                var framesCount = sampledBoneMatrices[clipIndex].GetLength(0);
                for (var keyframeIndex = 0; keyframeIndex < framesCount; keyframeIndex++)
                {
                    var frameOffset = keyframeIndex * numberOfBones * MATRIX_ROWS_COUNT;
                    for (var boneIndex = 0; boneIndex < numberOfBones; boneIndex++)    
                    {
                        var index = clipOffset + frameOffset + boneIndex * MATRIX_ROWS_COUNT;
                        var matrix = sampledBoneMatrices[clipIndex][keyframeIndex, boneIndex];

                        if (buffer[index + 0] != Vector4.zero) Debug.LogError($"Index {index + 0} not empty");
                        if (buffer[index + 1] != Vector4.zero) Debug.LogError($"Index {index + 1} not empty");
                        if (buffer[index + 2] != Vector4.zero) Debug.LogError($"Index {index + 2} not empty");
                        
                        buffer[index + 0] = matrix.GetRow(0);
                        buffer[index + 1] = matrix.GetRow(1);
                        buffer[index + 2] = matrix.GetRow(2);
                    }
                }

                var clip = animationClips[clipIndex];
                var start = clipOffset;

                var clipData = ClipData.Create(clip.name,
                                               clip.length, 
                                               clip.wrapMode,
                                               start,
                                               framesCount);
                
                bakedDataBuilder.AddClip(clipData);

                clipOffset += framesCount * numberOfBones * MATRIX_ROWS_COUNT;
            }
            bakedDataBuilder.SetBuffer(buffer);
            
            clipOffset = 0;
            for (var clipIndex = 0; clipIndex < sampledBoneMatrices.Count; clipIndex++)
            {
                var framesCount = sampledBoneMatrices[clipIndex].GetLength(0);
                for (var keyframeIndex = 0; keyframeIndex < framesCount; keyframeIndex++)
                {
                    var frameOffset = keyframeIndex * numberOfBones * MATRIX_ROWS_COUNT;
                    for (var boneIndex = 0; boneIndex < numberOfBones; boneIndex++)    
                    {   
                        var index = clipOffset + frameOffset + boneIndex * MATRIX_ROWS_COUNT;
                        var matrix = sampledBoneMatrices[clipIndex][keyframeIndex, boneIndex];
                        
                        var data0 = buffer[index];
                        var row0 = (Color)matrix.GetRow(0);
                        index++;

                        var data1 = buffer[index];
                        var row1 = (Color)matrix.GetRow(1);
                        index++;

                        var data2 = buffer[index];
                        var row2 = (Color)matrix.GetRow(2);

                        if (!Verify(row0, data0, clipIndex, keyframeIndex, boneIndex)) break;
                        if (!Verify(row1, data1, clipIndex, keyframeIndex, boneIndex)) break;
                        if (!Verify(row2, data2, clipIndex, keyframeIndex, boneIndex)) break;
                    }
                }

                clipOffset += numberOfBones * framesCount * MATRIX_ROWS_COUNT;
            }

            var data = bakedDataBuilder.Build();
            
            go.SetActive(false);
            Object.Destroy(go);
            return data;
        }

        private static List<Matrix4x4[,]> SampleAnimationClips(GameObject go,
            SkinnedMeshRenderer skinnedMeshRenderer,
            float frameRate, 
            out List<AnimationClip> animationClips,
            out int numberOfKeyFrames,
            out int numberOfBones)
        {
            animationClips = GetAllAnimationClips(go);
            numberOfBones = 0;
            
            numberOfKeyFrames = 0;
            var sampledBoneMatrices = new List<Matrix4x4[,]>();
            foreach (var animationClip in animationClips)
            {
                var sampledMatrix = SampleAnimationClip(go, 
                    animationClip,
                    frameRate,
                    skinnedMeshRenderer.bones,
                    skinnedMeshRenderer.sharedMesh);
                sampledBoneMatrices.Add(sampledMatrix);

                numberOfKeyFrames += sampledMatrix.GetLength(0);
                numberOfBones = sampledMatrix.GetLength(1);
            }

            return sampledBoneMatrices;
        }
        
        private static List<AnimationClip> GetAllAnimationClips(GameObject go)
        {
            var animator = go.GetComponent<Animator>();
            if (animator != null)
                return BakeryUtils.GetAllAnimationClips(animator);
            
            var animation = go.GetComponent<Animation>();
            if (animation != null)
                return BakeryUtils.GetAllAnimationClips(animation);
            
            return new List<AnimationClip>();
        }
        
        private static bool Verify(Vector4 row, Vector4 data,  
            int clipIndex, int keyframeIndex, int boneIndex)
        {
            if (row != data)
            {
                Debug.LogError("Error at (" + clipIndex + ", " + keyframeIndex + ", " + boneIndex + ")" +
                               " expected " + BakeryUtils.Format(row) +
                               " in data array " + BakeryUtils.Format(data));
                return false;
            }
            return true;
        }
        
        private static Matrix4x4[,] SampleAnimationClip(
            GameObject go,
            AnimationClip clip,
            float frameRate,
            IReadOnlyList<Transform> bones,
            Mesh mesh)
        {
            var boneCount = mesh.bindposes.Length;
            var numFrames = Mathf.CeilToInt(frameRate * clip.length);
            var boneMatrices = new Matrix4x4[numFrames, boneCount];
            
            for (var frameIndex = 0; frameIndex < numFrames; frameIndex++)
            {
                var time = (clip.length * frameIndex) / numFrames;
                clip.SampleAnimation(go, time);               
                
                for (var boneIndex = 0; boneIndex < boneCount; boneIndex++)
                {
                    // Put it into model space for better compression.
                    if (boneIndex < (bones?.Count ?? 0))
                        boneMatrices[frameIndex, boneIndex] = bones[boneIndex].localToWorldMatrix * mesh.bindposes[boneIndex];
                    else 
                        boneMatrices[frameIndex, boneIndex] = mesh.bindposes[boneIndex];
                }
            }
            
            return boneMatrices;
        }
    }
}