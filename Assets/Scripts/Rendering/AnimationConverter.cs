using System;
using System.Collections.Generic;
using AnimBakery.Cook;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Hash128 = UnityEngine.Hash128;
using Object = UnityEngine.Object;

namespace Unity.Rendering
{
    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    internal class AnimationConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Animation animation) 
                => AnimUtils.ConvertAnimation(DstEntityManager, this, animation));
        }
    }
    
    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    internal class AnimatorConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Animator animator) 
                => AnimUtils.ConvertAnimation(DstEntityManager, this, animator));
        }
    }

    internal static class AnimUtils
    {
        private static readonly Dictionary<Hash128, Material> sharedMaterial = new Dictionary<Hash128, Material>();
        private static readonly Dictionary<Hash128, Mesh> sharedMesh = new Dictionary<Hash128, Mesh>();
        
        public static void ConvertAnimation(
            EntityManager dstEntityManager,
            GameObjectConversionSystem conversionSystem,
            Component component)
        {
            var bakedData = Bakery.BakeClips(component.gameObject);
            if (bakedData == null || bakedData.AnimationCount <= 0)
                return;

            var offset = 0;
            var animClipArray = new NativeList<AnimationClipInfo>(bakedData.AnimationCount, Allocator.Temp);
            foreach (var anim in bakedData.Animations)
            {
                animClipArray.Add(new AnimationClipInfo
                {
                    Offset = offset,
                    ClipLength = anim.ClipLength,
                    FrameCount = anim.FramesCount,
                    WrapMode = AnimationClipInfo.ToMode(anim.WrapMode),
                });
                offset += anim.FramesCount;
            }

            var animationBakedBuffer = new AnimationBakedBuffer(bakedData.Buffer);

            var renderers = component.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    Debug.LogWarning("Missing renderer");
                    continue;
                }

                var primaryRendererEntity = conversionSystem.GetPrimaryEntity(renderer);

                dstEntityManager.AddComponent<BoneIndexOffset>(primaryRendererEntity);

                dstEntityManager.AddComponent<AnimatedTag>(primaryRendererEntity);
                dstEntityManager.AddComponent<AnimationState>(primaryRendererEntity);
                dstEntityManager.AddComponent<BoneIndexOffset>(primaryRendererEntity);
                dstEntityManager.AddComponentData(primaryRendererEntity, new BonesCount {Value = bakedData.BonesCount});
                dstEntityManager.AddSharedComponentData(primaryRendererEntity, animationBakedBuffer);
                dstEntityManager.AddBuffer<AnimationClipInfo>(primaryRendererEntity).CopyFrom(animClipArray);

                foreach (var rendererEntity in conversionSystem.GetEntities(renderer))
                {
                    if (dstEntityManager.HasComponent<RenderMesh>(rendererEntity))
                    {
                        var renderMesh = dstEntityManager.GetSharedComponentData<RenderMesh>(rendererEntity);
                        var renderMeshKey = ToHash128(ref renderMesh);
                        if (!sharedMesh.ContainsKey(renderMeshKey))
                        {
                            sharedMesh.Add(renderMeshKey, BakeryUtils.CopyAndFillBonesUV(renderMesh.mesh));
                        }
                        if (!sharedMaterial.ContainsKey(renderMeshKey))
                        {
                            var material = Object.Instantiate(renderMesh.material);
                            material.SetBuffer("_AnimationsBuffer", animationBakedBuffer.ToBuffer());
                            sharedMaterial.Add(renderMeshKey, material);
                        }

                        renderMesh.mesh = sharedMesh[renderMeshKey];
                        renderMesh.material = sharedMaterial[renderMeshKey];
                        
                        dstEntityManager.SetSharedComponentData(rendererEntity, renderMesh);
                    }
                }
            }

            animClipArray.Dispose();
        }

        public static Hash128 ToHash128(ref RenderMesh renderMesh)
        {
            if (renderMesh.mesh != null && renderMesh.material != null)
            {
                
                return UnityEngine.Hash128.Compute(
                    $"{renderMesh.mesh.GetInstanceID()}{renderMesh.material.GetInstanceID()}{renderMesh.layer}{renderMesh.castShadows}{renderMesh.receiveShadows}{renderMesh.subMesh}");
            }

            throw new ArgumentException("mesh and material couldn't be null");
        }
    }
}