using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DOTS.SubScene
{
    public class CameraHandler : MonoBehaviour
    {

        private void Update()
        {
            float3 playerEntityPosition = GetPlayerEntityPosition();
            transform.position = new Vector3(playerEntityPosition.x, playerEntityPosition.y, transform.position.z);
        }

        private Entity GetPlayerEntity()
        {
            EntityQuery entityQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(PlayerComponent));

            Entity playerEntity = entityQuery.GetSingletonEntity();

            entityQuery.Dispose();

            return playerEntity;
        }

        private float3 GetPlayerEntityPosition()
        {
            EntityQuery entityQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(PlayerComponent), typeof(Translation));

            World.DefaultGameObjectInjectionWorld.GetExistingSystem<SimulationSystemGroup>().GetSingleton<PlayerComponent>();

            Entity playerEntity = entityQuery.GetSingletonEntity();

            Translation playerTranslation = entityQuery.GetSingleton<Translation>();

            entityQuery.Dispose();

            return playerTranslation.Value;
        }

    }
}
