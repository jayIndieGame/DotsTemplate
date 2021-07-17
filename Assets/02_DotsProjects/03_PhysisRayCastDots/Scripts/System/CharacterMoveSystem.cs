using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.MousePickBehaviour;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Unity.Physics.Math;
using RaycastHit = Unity.Physics.RaycastHit;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class CharacterMoveSystem : SystemBase
{
    private EntityQuery query;
    private const float k_MaxDistance = 100f;
    public JobHandle? MoveJobHandle;
    BuildPhysicsWorld m_BuildPhysicsWorldSystem;


    public CharacterMoveSystem()
    {

    }
    protected override void OnCreate()
    {
        query = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(CharacterComponent),
                typeof(Translation),
                typeof(Rotation)
            }
        });

        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }

    protected override void OnDestroy()
    {

    }

    [BurstCompile]
    struct MoveJob: IJobEntityBatch
    {

        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public int NumDynamicBodies;

        public ComponentTypeHandle<Translation> transHandle;
        [ReadOnly] public ComponentTypeHandle<CharacterComponent> CharacterComponentTypeHandle;

        public RaycastInput RayInput;
        private RaycastInput rayUnderFoot;
        private RaycastHit raycastHit;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            MousePickCollector mousePickCollector = new MousePickCollector(1.0f, CollisionWorld.Bodies, NumDynamicBodies);
            mousePickCollector.IgnoreTriggers = true;

            CollisionWorld.CastRay(RayInput, ref mousePickCollector);
            if (mousePickCollector.MaxFraction < 1f)
            {
                float3 MovePoint = mousePickCollector.Hit.Position;

                var transArray = batchInChunk.GetNativeArray(transHandle);
                var characterArray = batchInChunk.GetNativeArray(CharacterComponentTypeHandle);

                for (int i = 0; i < batchInChunk.Count; i++)
                {
                    var translation = transArray[i];
                    var character = characterArray[i];

                    rayUnderFoot = new RaycastInput
                    {
                        Start = translation.Value,
                        End = new float3(0, -100f, 0),
                        Filter = new CollisionFilter
                        {
                            BelongsTo = ~0u,
                            CollidesWith = ~0u,
                            GroupIndex = 0,//TODO
                        }
                    };
                    CollisionWorld.CastRay(rayUnderFoot, out raycastHit);


                    //TODO

                }

            }
        }

    }

    protected override void OnUpdate()
    {
        if (query.CalculateEntityCount() == 0)
        {
            return;
        }
        var handle = JobHandle.CombineDependencies(Dependency, m_BuildPhysicsWorldSystem.GetOutputDependency());

        var translationHandle = GetComponentTypeHandle<Translation>();
        var characterComponentTypeHandle = GetComponentTypeHandle<CharacterComponent>(true);

        if (Input.GetMouseButtonDown(0) && (Camera.main != null))
        {
            Vector2 mousePosition = Input.mousePosition;
            //获取鼠标射线的方式还没变
            UnityEngine.Ray unityRay = Camera.main.ScreenPointToRay(mousePosition);

            handle = new MoveJob
            {
                transHandle = translationHandle,
                CharacterComponentTypeHandle = characterComponentTypeHandle,
                CollisionWorld = m_BuildPhysicsWorldSystem.PhysicsWorld.CollisionWorld,
                NumDynamicBodies = m_BuildPhysicsWorldSystem.PhysicsWorld.NumDynamicBodies,
                RayInput = new RaycastInput
                {
                    Start = unityRay.origin,
                    End = unityRay.origin + unityRay.direction * k_MaxDistance,
                    Filter = CollisionFilter.Default,//TODO
                },

            }.Schedule(query, handle);

            MoveJobHandle = handle;

            handle.Complete();
        }

    }

}
