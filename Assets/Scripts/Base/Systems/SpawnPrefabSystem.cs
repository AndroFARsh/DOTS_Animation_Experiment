using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Demo
{
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
                for (var i = 0; i < spawnPrefab.Count; ++i)
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
}