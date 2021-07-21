using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dots.SubScene
{
    public class SubSceneLoader : ComponentSystem
    {
        private SceneSystem sceneSystem;

        protected override void OnCreate()
        {
            sceneSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SceneSystem>();
        }

        protected override void OnUpdate()
        {
            Entities.WithAll<PlayerComponent>().ForEach((ref Translation translation,ref PlayerComponent playerComponent) =>
            {
                foreach (var subScene in SubSceneReference.Instance.SubSceneArray)
                {
                    if (math.distance(subScene.transform.position, translation.Value) <
                        playerComponent.LoadSceneDistance)
                        
                    {
                        LoadSceneAsync(subScene);
                    }
                    else
                    {
                        UnLoadScene(subScene);
                    }
                }
            });

            //if (Input.GetKeyDown((KeyCode.Space)))
            //{
            //    LoadSceneAsync(SubSceneReference.Instance.SubSceneArray[0]);
            //}
            //if (Input.GetKeyDown((KeyCode.A)))
            //{
            //    UnLoadScene(SubSceneReference.Instance.SubSceneArray[0]);
            //}
        }

        private void LoadSceneAsync(Unity.Scenes.SubScene subScene)
        {
            sceneSystem.LoadSceneAsync(subScene.SceneGUID);
        }

        private void UnLoadScene(Unity.Scenes.SubScene subScene)
        {
            sceneSystem.UnloadScene(subScene.SceneGUID);

        }

    }

}
