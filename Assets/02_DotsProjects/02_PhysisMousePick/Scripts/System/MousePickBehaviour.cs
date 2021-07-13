using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
//整个脚本基本和官方的UnityPhysicsSamples中的MousePick一致。这个MousePick是个不错的范例脚本。
namespace Unity.Physics.MousePickBehaviour
{
    public class MousePickBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        //忽略Triggers的检测
        public bool IgnoreTriggers = true;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            //通过EntityManager给entity添加MousePick的IComponentData。并且赋值。
            dstManager.AddComponentData(entity, new MousePick()
            {
                IgnoreTriggers = IgnoreTriggers ? 1 : 0,
            });
        }
    }
    //鼠标每次点击都会将点击储存起来。
    [BurstCompile]
    public struct MousePickCollector : ICollector<RaycastHit>
    {
        public bool IgnoreTriggers;
        public NativeArray<RigidBody> Bodies;
        public int NumDynamicBodies;


        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; private set; }
        public int NumHits { get; private set; }

        private RaycastHit m_ClosestHit;
        public RaycastHit Hit => m_ClosestHit;

        public bool AddHit(RaycastHit hit)
        {
            //断言这个条件是对的，如果不对的话，会输出Asserttion is Failed这个log。Assertion也是一种Debug的方式。
            Assert.IsTrue(hit.Fraction < MaxFraction);

            var isAcceptable = (hit.RigidBodyIndex >= 0) && (hit.RigidBodyIndex < NumDynamicBodies);
            if (IgnoreTriggers)
            {
                isAcceptable = isAcceptable && hit.Material.CollisionResponse != CollisionResponsePolicy.RaiseTriggerEvents;
            }

            if (!isAcceptable)
            {
                return false;
            }

            MaxFraction = hit.Fraction;
            m_ClosestHit = hit;
            NumHits = 1;
            return true;
        }

        public MousePickCollector(float maxFraction, NativeArray<RigidBody> rigidBodies, int numDynamicBodies)
        {
            m_ClosestHit = default(RaycastHit);
            MaxFraction = maxFraction;
            NumHits = 0;
            IgnoreTriggers = true;
            Bodies = rigidBodies;
            NumDynamicBodies = numDynamicBodies;
        }
    }
}



