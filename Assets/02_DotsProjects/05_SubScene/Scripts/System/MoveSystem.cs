using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DOTS.SubScene
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class PlayerMovementSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            if (SceneManager.GetActiveScene().name != "SubScene_MainScene") return;

            Entities.WithAll<PlayerComponent>().ForEach((ref Translation translation) => {
                float moveX = 0f;
                float moveY = 0f;
                if (Input.GetKey(KeyCode.RightArrow)) moveX = +1f;
                if (Input.GetKey(KeyCode.LeftArrow)) moveX = -1f;
                if (Input.GetKey(KeyCode.UpArrow)) moveY = +1f;
                if (Input.GetKey(KeyCode.DownArrow)) moveY = -1f;

                float moveSpeed = 5f;
                translation.Value += new float3(moveX, moveY, 0f) * Time.DeltaTime * moveSpeed;
            });
        }

    }
}
