using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct SpawnPrefab: IComponentData
{
    public Entity Prefab;
    public int Count;
}

namespace Authoring
{
    public class SPrefab: MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public GameObject Prefab;
        public int Count;
        
        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(Prefab);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new SpawnPrefab
            {
                Prefab = conversionSystem.GetPrimaryEntity(Prefab) ,
                Count = Count
            });
        }
    }    
}

public class SpawnPrefabSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Entity entity, ref SpawnPrefab spawnPrefab, ref Translation translation) =>
            {
                PostUpdateCommands.RemoveComponent<SpawnPrefab>(entity);

                var centerPoint = translation.Value;
                var line = (int) math.sqrt(spawnPrefab.Count);
                var halfLine = line >> 1;
                for (var i=0; i<spawnPrefab.Count; ++i)
                {
                    var itemEntity = PostUpdateCommands.Instantiate(spawnPrefab.Prefab);
                    PostUpdateCommands.SetComponent(itemEntity, new Translation
                    {
                        Value = new float3
                        {
                            x = centerPoint.x + (i / line) * 1.0f - halfLine,
                            y = centerPoint.y,
                            z = centerPoint.z + (i % line) * 1.0f - halfLine,
                        }
                    });
                }
            });
    }
}