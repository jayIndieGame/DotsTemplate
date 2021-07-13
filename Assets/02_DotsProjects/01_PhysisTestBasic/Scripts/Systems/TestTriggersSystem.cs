using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public class TestTriggersSystem : SystemBase
{
    [BurstCompile]
    private struct TriggerJob:ITriggerEventsJob
    {
        public ComponentDataFromEntity<PhysicsVelocity> physisVelocityEntities;

        public void Execute(TriggerEvent triggerEvent)
        {
            if (physisVelocityEntities.HasComponent(triggerEvent.EntityA))
            {
                PhysicsVelocity physicsVelocity = physisVelocityEntities[triggerEvent.EntityA];
                physicsVelocity.Linear.y = 5f;
                physisVelocityEntities[triggerEvent.EntityA] = physicsVelocity;
            }

            if (physisVelocityEntities.HasComponent(triggerEvent.EntityB))
            {
                PhysicsVelocity physicsVelocity = physisVelocityEntities[triggerEvent.EntityB];
                physicsVelocity.Linear.y = 5f;
                physisVelocityEntities[triggerEvent.EntityB] = physicsVelocity;
            }
        }
    }

    private BuildPhysicsWorld buildPhysicsWorld;
    private StepPhysicsWorld stepPhysicsWorld;

    protected override void OnCreate()
    {
        buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
    }

    protected override void OnUpdate()
    {
        TriggerJob triggerJob = new TriggerJob
        {
            physisVelocityEntities = GetComponentDataFromEntity<PhysicsVelocity>()
        };
        Dependency = triggerJob.Schedule(stepPhysicsWorld.Simulation, ref buildPhysicsWorld.PhysicsWorld,Dependency);
    }
}
