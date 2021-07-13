using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
//�����ű������͹ٷ���UnityPhysicsSamples�е�MousePickһ�¡����MousePick�Ǹ�����ķ����ű���
namespace Unity.Physics.MousePickBehaviour
{
    public class MousePickBehaviour : MonoBehaviour, IConvertGameObjectToEntity
    {
        //����Triggers�ļ��
        public bool IgnoreTriggers = true;
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            //ͨ��EntityManager��entity���MousePick��IComponentData�����Ҹ�ֵ��
            dstManager.AddComponentData(entity, new MousePick()
            {
                IgnoreTriggers = IgnoreTriggers ? 1 : 0,
            });
        }
    }
    //���ÿ�ε�����Ὣ�������������
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
            //������������ǶԵģ�������ԵĻ��������Asserttion is Failed���log��AssertionҲ��һ��Debug�ķ�ʽ��
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



