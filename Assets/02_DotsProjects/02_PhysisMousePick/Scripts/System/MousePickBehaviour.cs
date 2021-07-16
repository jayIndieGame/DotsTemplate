using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Unity.Physics.Math;

//整个脚本基本和官方的UnityPhysicsSamples中的MousePick一致。这个MousePick是个不错的范例脚本。
//直接就照着抄了。一些注释会写的随意一点
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
    //很标准的一个ICollector<RaycastHit> 没有添加任何额外的功能。。为啥不做成一个公共的API啊
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

    //这个System在SimulationSystemGroup中更新
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class MousePickSystem : SystemBase
    {
        //射线最远100米写死了
        const float k_MaxDistance = 100.0f;

        //一个System里面就是得有一个EntityQuery来获取各个entities
        EntityQuery m_MouseGroup;
        BuildPhysicsWorld m_BuildPhysicsWorldSystem;

        public NativeArray<SpringData> SpringDatas;
        public JobHandle? PickJobHandle;
        
        //点击物体需要给物体添加一个Spring。所以弹簧都不配在physics里面写个标准的么。
        public struct SpringData
        {
            public Entity Entity;
            public int Dragging; // bool isn't blittable
            public float3 PointOnBody;
            public float MouseDepth;
        }

        //按下鼠标左键时需要执行的Job。
        //所以这个Job一定要写在这个System的OnUpdate的Input.GetMouseButtonDown里面。
        [BurstCompile]
        struct Pick : IJob
        {
            //把需要用到的变量定义出来
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public int NumDynamicBodies;
            public NativeArray<SpringData> SpringData;
            public RaycastInput RayInput;
            public float Near;
            public float3 Forward;
            [ReadOnly] public bool IgnoreTriggers;

            public void Execute()
            {
                //MousePickCollector 这玩意儿就类似于 RaycastInfo。但是竟然要自己写么。都没现成的？？？？
                //public bool CastRay<T>(RaycastInput input, ref T collector) where T : struct, ICollector<RaycastHit>
                //限制了结构体和继承了ICollector<RaycastHit>就行。
                var mousePickCollector = new MousePickCollector(1.0f, CollisionWorld.Bodies, NumDynamicBodies);
                mousePickCollector.IgnoreTriggers = IgnoreTriggers;

                CollisionWorld.CastRay(RayInput, ref mousePickCollector);
                //加这个if干啥.里面不是有断言么
                if (mousePickCollector.MaxFraction < 1.0f)
                {

                    float fraction = mousePickCollector.Hit.Fraction;

                    //Bodies是一个存储所有刚体的NativeArray。直接拿出来就行
                    RigidBody hitBody = CollisionWorld.Bodies[mousePickCollector.Hit.RigidBodyIndex];
                    //目的是获取到物体身上被射线点中的点。
                    //hitBody.WorldFromBody是物体的世界坐标。现在需要一个把物体坐标转换为世界坐标的旋转矩阵。
                    //物体世界坐标 = （0，0，0）左乘 世界的旋转矩阵 。
                    //世界的旋转矩阵 = 物体的世界坐标的逆。又因为是旋转矩阵。所以逆就等于转置。所以旋转矩阵 = 当前物体的世界坐标的转置。
                    Math.MTransform bodyFromWorld = Inverse(new Math.MTransform(hitBody.WorldFromBody));
                    //mousePickCollector.Hit.Position 这个是被射中的物体的坐标系。
                    //左乘一个世界旋转矩阵就能得到世界坐标了。
                    float3 pointOnBody = Mul(bodyFromWorld, mousePickCollector.Hit.Position);

                    //以上步骤不打个包写个API出来么？直接写在射线里pointOnBody = mousePickCollector.Hit.Position.ToWorld不好么，不过这个Position是个float3..确实不能在float3里加


                    //建立一个弹簧。。本质上是像有限元的思路
                    SpringData[0] = new SpringData
                    {
                        Entity = hitBody.Entity,
                        Dragging = 1,
                        PointOnBody = pointOnBody,
                        //其实就是被点击物体的z值。
                        MouseDepth = Near + math.dot(math.normalize(RayInput.End - RayInput.Start), Forward) * fraction * k_MaxDistance,
                    };
                }
                else
                {

                    SpringData[0] = new SpringData
                    {
                        Dragging = 0
                    };
                }
            }
        }

        public MousePickSystem()
        {
            //不销毁
            SpringDatas = new NativeArray<SpringData>(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            SpringDatas[0] = new SpringData();
        }

        protected override void OnCreate()
        {
            m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_MouseGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(MousePick) }
            });
        }

        protected override void OnDestroy()
        {
            SpringDatas.Dispose();
        }

        protected override void OnUpdate()
        {
            if (m_MouseGroup.CalculateEntityCount() == 0)
            {
                return;
            }
            //Dependcy需要设置
            var handle = JobHandle.CombineDependencies(Dependency, m_BuildPhysicsWorldSystem.GetOutputDependency());

            if (Input.GetMouseButtonDown(0) && (Camera.main != null))
            {
                Vector2 mousePosition = Input.mousePosition;
                //获取鼠标射线的方式还没变
                UnityEngine.Ray unityRay = Camera.main.ScreenPointToRay(mousePosition);

                var mice = m_MouseGroup.ToComponentDataArray<MousePick>(Allocator.TempJob);
                var IgnoreTriggers = mice[0].IgnoreTriggers != 0;
                mice.Dispose();

                // Schedule picking job, after the collision world has been built
                handle = new Pick
                {
                    CollisionWorld = m_BuildPhysicsWorldSystem.PhysicsWorld.CollisionWorld,
                    NumDynamicBodies = m_BuildPhysicsWorldSystem.PhysicsWorld.NumDynamicBodies,
                    SpringData = SpringDatas,
                    RayInput = new RaycastInput
                    {
                        Start = unityRay.origin,
                        End = unityRay.origin + unityRay.direction * k_MaxDistance,
                        Filter = CollisionFilter.Default,
                    },
                    Near = Camera.main.nearClipPlane,
                    Forward = Camera.main.transform.forward,
                    IgnoreTriggers = IgnoreTriggers,
                }.Schedule(handle);

                PickJobHandle = handle;

                handle.Complete(); // TODO.ma figure out how to do this properly...we need a way to make physics sync wait for
                // any user jobs that touch the component data, maybe a JobHandle LastUserJob or something that the user has to set
            }

            if (Input.GetMouseButtonUp(0))
            {
                SpringDatas[0] = new SpringData();
            }
            //Dependency补充一下
            Dependency = handle;
        }
    }

    // Applies any mouse spring as a change in velocity on the entity's motion component
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    public class MouseSpringSystem : SystemBase
    {
        EntityQuery m_MouseGroup;
        MousePickSystem m_PickSystem;

        protected override void OnCreate()
        {
            m_PickSystem = World.GetOrCreateSystem<MousePickSystem>();
            m_MouseGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(MousePick) }
            });
        }

        protected override void OnUpdate()
        {
            if (m_MouseGroup.CalculateEntityCount() == 0)
            {
                return;
            }

            ComponentDataFromEntity<Translation> Positions = GetComponentDataFromEntity<Translation>(true);
            ComponentDataFromEntity<Rotation> Rotations = GetComponentDataFromEntity<Rotation>(true);
            ComponentDataFromEntity<PhysicsVelocity> Velocities = GetComponentDataFromEntity<PhysicsVelocity>();
            ComponentDataFromEntity<PhysicsMass> Masses = GetComponentDataFromEntity<PhysicsMass>(true);

            // If there's a pick job, wait for it to finish
            if (m_PickSystem.PickJobHandle != null)
            {
                JobHandle.CombineDependencies(Dependency, m_PickSystem.PickJobHandle.Value).Complete();
            }

            // If there's a picked entity, drag it
            MousePickSystem.SpringData springData = m_PickSystem.SpringDatas[0];
            if (springData.Dragging != 0)
            {
                Entity entity = m_PickSystem.SpringDatas[0].Entity;
                if (!EntityManager.HasComponent<PhysicsMass>(entity))
                {
                    return;
                }

                PhysicsMass massComponent = Masses[entity];
                PhysicsVelocity velocityComponent = Velocities[entity];

                //相当于Mass等于无穷大时的直接退出,massComponent也是离谱为啥不存mass存个InverseMass
                if (massComponent.InverseMass == 0)
                {
                    return;
                }
                //这个只是获取每帧物体的旋转和位置
                var worldFromBody = new MTransform(Rotations[entity].Value, Positions[entity].Value);

                // Body to motion transform
                // 获取质心坐标。InertiaOrientation不懂，是PhysicsMass里独有的一个四元数
                // 大概的意思是从物体的世界坐标变成了运动的世界坐标
                var bodyFromMotion = new MTransform(Masses[entity].InertiaOrientation, Masses[entity].CenterOfMass);
                MTransform worldFromMotion = Mul(worldFromBody, bodyFromMotion);

                // Damp the current velocity
                const float gain = 0.95f;
                velocityComponent.Linear *= gain;
                velocityComponent.Angular *= gain;

                // Get the body and mouse points in world spaces
                float3 pointBodyWs = Mul(worldFromBody, springData.PointOnBody);
                // 弹簧的世界坐标
                float3 pointSpringWs = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, springData.MouseDepth));

                // Calculate the required change in velocity
                // 从物体坐标系又变成了运动坐标系？
                float3 pointBodyLs = Mul(Inverse(bodyFromMotion), springData.PointOnBody);
                float3 deltaVelocity;
                {
                    //弹簧和作用点的距离
                    float3 pointDiff = pointBodyWs - pointSpringWs;
                    float3 relativeVelocityInWorld = velocityComponent.Linear + math.mul(worldFromMotion.Rotation, math.cross(velocityComponent.Angular, pointBodyLs));

                    //劲度系数
                    const float elasticity = 0.1f;
                    //怎么定义这么多阻力
                    const float damping = 0.5f;
                    deltaVelocity = -pointDiff * (elasticity / Time.DeltaTime) - damping * relativeVelocityInWorld;
                }

                // Build effective mass matrix in world space
                // TODO how are bodies with inf inertia and finite mass represented 都无穷大的惯性和质量了，就不存在相对应的物理效果了啊
                // TODO the aggressive damping is hiding something wrong in this code if dragging non-uniform shapes 阻力很大的时候，不均匀的形状会发生错误。。emm
                float3x3 effectiveMassMatrix;
                {
                    float3 arm = pointBodyWs - worldFromMotion.Translation;
                    var skew = new float3x3(
                        new float3(0.0f, arm.z, -arm.y),
                        new float3(-arm.z, 0.0f, arm.x),
                        new float3(arm.y, -arm.x, 0.0f)
                    );

                    // world space inertia = worldFromMotion * inertiaInMotionSpace * motionFromWorld
                    var invInertiaWs = new float3x3(
                        massComponent.InverseInertia.x * worldFromMotion.Rotation.c0,
                        massComponent.InverseInertia.y * worldFromMotion.Rotation.c1,
                        massComponent.InverseInertia.z * worldFromMotion.Rotation.c2
                    );
                    invInertiaWs = math.mul(invInertiaWs, math.transpose(worldFromMotion.Rotation));

                    float3x3 invEffMassMatrix = math.mul(math.mul(skew, invInertiaWs), skew);
                    invEffMassMatrix.c0 = new float3(massComponent.InverseMass, 0.0f, 0.0f) - invEffMassMatrix.c0;
                    invEffMassMatrix.c1 = new float3(0.0f, massComponent.InverseMass, 0.0f) - invEffMassMatrix.c1;
                    invEffMassMatrix.c2 = new float3(0.0f, 0.0f, massComponent.InverseMass) - invEffMassMatrix.c2;

                    effectiveMassMatrix = math.inverse(invEffMassMatrix);
                }

                // Calculate impulse to cause the desired change in velocity
                float3 impulse = math.mul(effectiveMassMatrix, deltaVelocity);

                // Clip the impulse
                const float maxAcceleration = 250.0f;

                float maxImpulse = math.rcp(massComponent.InverseMass) * Time.DeltaTime * maxAcceleration;
                impulse *= math.min(1.0f, math.sqrt((maxImpulse * maxImpulse) / math.lengthsq(impulse)));

                // Apply the impulse
                {
                    velocityComponent.Linear += impulse * massComponent.InverseMass;

                    float3 impulseLs = math.mul(math.transpose(worldFromMotion.Rotation), impulse);
                    float3 angularImpulseLs = math.cross(pointBodyLs, impulseLs);
                    velocityComponent.Angular += angularImpulseLs * massComponent.InverseInertia;
                }

                // Write back velocity
                Velocities[entity] = velocityComponent;
            }
        }
    }
}



