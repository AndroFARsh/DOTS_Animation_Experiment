using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Demo
{

    [GenerateAuthoringComponent]
    public struct SpawnPrefab : IComponentData
    {
        public Entity Prefab;
        public int Count;
    }
}

// namespace Authoring
// {
//     public class SpawnPrefab: MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
//     {
//         public GameObject Prefab;
//         public int Count;
//         
//         public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
//         {
//             referencedPrefabs.Add(Prefab);
//         }
//
//         public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
//         {
//             dstManager.AddComponentData(entity, new global::SpawnPrefab
//             {
//                 Prefab = conversionSystem.GetPrimaryEntity(Prefab),
//                 Count = Count
//             });
//         }
//     }    
// }
