using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using Hash128 = Unity.Entities.Hash128;

namespace Unity.Rendering
{
    public class CopyFrameOffsetProperty : JobComponentSystem
    {
        private unsafe class PropertyData : IDisposable
        {
            private static readonly int NameId = Shader.PropertyToID("_FrameOffset");
            private const int SizeOfValue = sizeof(float);
            
            private int size;
            private int capacity;
            private float defaultValue;
            
            private NativeArray<float> values;
            private ComputeBuffer computeBuffer;
            private JobHandle prevJobHandle;
            
            internal PropertyData(int newCapacity, float newDefaultValue = 0)
            {
                size = 0;
                capacity = newCapacity;
                defaultValue = newDefaultValue;
                prevJobHandle = default;
                
                values = new NativeArray<float>(capacity, Allocator.Persistent);
                computeBuffer = new ComputeBuffer(capacity, sizeof(float));
            }

            internal void SetData(Material material)
            {
                prevJobHandle.Complete();
                
                computeBuffer.SetData(values, 0, 0, size);
                material.SetBuffer(NameId, computeBuffer);
            }

            internal JobHandle Schedule(EntityQuery entityQuery, ComponentSystemBase system, JobHandle jobHandle)
            {
                size = entityQuery.CalculateEntityCount();
                if (capacity < size)
                {
                    capacity = 2 * size;
                    values.Dispose(jobHandle);
                    values = new NativeArray<float>(capacity, Allocator.Persistent);
                    computeBuffer.Dispose();
                    computeBuffer = new ComputeBuffer(capacity, sizeof(float));
                }
                
                return prevJobHandle = new FillArrayJob
                {
                    sizeOfValue = SizeOfValue,
                    dynamicType = system.GetArchetypeChunkComponentTypeDynamic(ComponentType.ReadOnly<BoneIndexOffset>()),
                            
                    defaultValue = UnsafeUtility.AddressOf(ref defaultValue),
                    dst = values.GetUnsafePtr(),
                }.Schedule(entityQuery, jobHandle);
            }
            
            public void Dispose()
            {
                values.Dispose();
                computeBuffer.Dispose();
            }
        }

        private Dictionary<Hash128, PropertyData> properties;
        private EntityQuery entityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            properties = new Dictionary<Hash128, PropertyData>(128);
            entityQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<RenderMesh>(),
                    ComponentType.ReadOnly<BoneIndexOffset>(),
                },
            });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var prop in properties.Values)
            {
                prop.Dispose();
            }
            properties.Clear();
        }

        protected override JobHandle OnUpdate(JobHandle jobHandle)
        {
            if (entityQuery.IsEmptyIgnoreFilter) return jobHandle;

            var chunkCount = entityQuery.CalculateChunkCount();
            using (var meshRenderIndexes = new NativeArray<int>(chunkCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory))
            using (var sortedSharedIndexes = new NativeArraySharedValues<int>(meshRenderIndexes, Allocator.TempJob))
            {
                Profiler.BeginSample("Sort Shared RenderMesh");
                {
                    jobHandle = new FillSharedIndexInChunkJob<RenderMesh>
                    {
                        sharedType = GetArchetypeChunkSharedComponentType<RenderMesh>(),
                        sharedIndexesInChunk = meshRenderIndexes
                    }.Schedule(entityQuery, jobHandle);

                    jobHandle = sortedSharedIndexes.Schedule(jobHandle);
                    jobHandle.Complete();
                }
                Profiler.EndSample();

                Profiler.BeginSample("Prepare & Set FameOffset");
                {
                    var sharedRenderCount = sortedSharedIndexes.SharedValueCount;
                    for (var i = 0; i < sharedRenderCount; ++i)
                    {
                        var sharedValueIndices = sortedSharedIndexes.GetSharedValueIndicesBySharedIndex(i);
                        if (sharedValueIndices.Length == 0) continue;

                        var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(meshRenderIndexes[sharedValueIndices[0]]);
                        var renderMeshKey = AnimUtils.ToHash128(ref renderMesh);
                        if (!properties.ContainsKey(renderMeshKey))
                            properties.Add(renderMeshKey, new PropertyData(1024));

                        var arrayData = properties[renderMeshKey];
                        
                        Profiler.BeginSample("Set FameOffset");
                        arrayData.SetData(renderMesh.material);
                        Profiler.EndSample();

                        Profiler.BeginSample("Prepare FameOffset");
                        entityQuery.SetSharedComponentFilter(renderMesh);
                        jobHandle = arrayData.Schedule(entityQuery, this, jobHandle);
                        entityQuery.ResetFilter();
                        Profiler.EndSample();
                    }
                }
                Profiler.EndSample();
            }

            return jobHandle;
        }

        #region Jobs

        [BurstCompile]
        private struct FillSharedIndexInChunkJob<T> : IJobChunk
            where T : struct, ISharedComponentData
        {
            [ReadOnly] public ArchetypeChunkSharedComponentType<T> sharedType;
            public NativeArray<int> sharedIndexesInChunk;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
                => sharedIndexesInChunk[chunkIndex] = chunk.GetSharedComponentIndex(sharedType);
        }

        [BurstCompile]
        internal unsafe struct FillArrayJob : IJobChunk
        {
            [ReadOnly] public int sizeOfValue;
            [ReadOnly] public ArchetypeChunkComponentTypeDynamic dynamicType;

            [NativeDisableUnsafePtrRestriction] [ReadOnly] public void* defaultValue;
            [NativeDisableUnsafePtrRestriction] public void* dst;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var dstPtr = (void*) ((IntPtr) dst + sizeOfValue * firstEntityIndex);
                var src = chunk.GetDynamicComponentDataArrayReinterpret<byte>(dynamicType, sizeOfValue);
                if (src.Length == 0)
                {
                    UnsafeUtility.MemCpyReplicate(dstPtr, defaultValue, sizeOfValue, chunk.Count);
                }
                else
                {
                    var srcPtr = src.GetUnsafeReadOnlyPtr();
                    UnsafeUtility.MemCpy(dstPtr, srcPtr, src.Length);
                }
            }
        }
        
        #endregion
    }
}