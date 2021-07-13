using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Material = UnityEngine.Material;
using Random = UnityEngine.Random;

public class GameStart : MonoBehaviour
{
    [SerializeField] private Mesh _mesh;
    [SerializeField] private Material _material;

    public float PercentagePerLerp = 0.05f;
    private Translation EndTranslation;
    public int CountToGen = 100;

    // Start is called before the first frame update
    void Start()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;


        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(RenderMesh),
            typeof(Translation),
            typeof(LocalToWorld),
            typeof(RenderBounds),
            typeof(LerpComponent)

        );

        NativeArray<Entity> eArray = new NativeArray<Entity>(CountToGen, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, eArray);

        foreach (var entity in eArray)
        {
            EndTranslation = new Translation { Value = new float3(Random.Range(-8, 8), Random.Range(-5, 5), -5) };
            float3 initPos = new float3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));
            var data = new LerpComponent { LerpT = PercentagePerLerp, endPos = EndTranslation };
            var initTranslation = new Translation { Value = initPos };

            entityManager.SetComponentData(entity, data);
            entityManager.SetComponentData(entity, initTranslation);

            entityManager.SetSharedComponentData(entity, new RenderMesh { mesh = _mesh, material = _material });
        }
    }

}
