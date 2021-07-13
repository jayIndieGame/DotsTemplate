using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

public class RaycastTest : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            UnityEngine.Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            float rayDistance = 100f;
            Debug.Log(RayCast(ray.origin,ray.direction*rayDistance));
        }
    }

    private Entity RayCast(float3 fromPosition, float3 endPosition)
    {
        BuildPhysicsWorld buildPhysicsWorld =
            World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
        CollisionWorld collisionWorld = buildPhysicsWorld.PhysicsWorld.CollisionWorld;

        RaycastInput raycastInput = new RaycastInput()
        {
            Start = fromPosition,
            End = endPosition,
            Filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u,
                GroupIndex = 0,
            }
        };

        RaycastHit raycastHit = new RaycastHit();
        if(collisionWorld.CastRay(raycastInput,out raycastHit))
        {
            //这俩都行的
            Entity entity = buildPhysicsWorld.PhysicsWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
            Entity entityCollisionWorld = collisionWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
            return entityCollisionWorld;
        }
        else
        {
            return Entity.Null;;
        }
    }
}
