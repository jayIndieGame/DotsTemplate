## Dots_Ecs_4_1_Physis

这部分将会零散的介绍各个API。最后应该会整理一个有逻辑顺序的文档出来。

### BuildPhysicsWorld

```csharp
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[AlwaysUpdateSystem]
public class BuildPhysicsWorld : SystemBase, IPhysicsSystem
```

**BuildPhysicsWorld**是一个建立在Entity world的之上的一个物理世界。

这个世界包含每一个带有**rigid** **body**/**Joint**组件的Entity身上的**rigid** **body**/**Joint**。这个System主要的工作是获取到跟物理相关的各个组件，以及PhysicsWolrd。更新他们的状态，对各种类型的物理物体计数，提供了添加dependency的方法。保证下一帧运行前，上一帧执行完毕。

以下方式可以获取或者创建**BuildPhysicsWorld**

```csharp
BuildPhysicsWorld buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
//获取默认世界的BuildPhysicsWorld
BuildPhysicsWorld buildPhysicsWorld =
            World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
```

公有成员变量介绍（私有的不说了）：

- #### DynamicEntityGroup

  - 必须包含**PhysicsVelocity**，**Translation**，**Rotation**这几个Component但是不能包含**PhysicsExclude**

- #### StaticEntityGroup

  - 必须包含**PhysicsCollider**。并且包含**LocalToWorld**，**Translation**，**Rotation**的其中一个。且不能包含**PhysicsExclude**，**PhysicsVelocity**

- #### **JointEntityGroup**

  - 必须包含**PhysicsConstrainedBodyPair**，**PhysicsJoint**这两个Component。不能包含**PhysicsExclude**。

- #### CollisionWorldProxyGroup

  - 必须含有**CollisionWorldProxy**的Entity的Group

- #### **PhysicsWorld**

  - 放在PhysicsWorld里面详细说。BuildPhysicsWorld就是建立了一个PhysicsWorld的对象。

## StepPhysicsWorld

```csharp
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(ExportPhysicsWorld)), AlwaysUpdateSystem]
public class StepPhysicsWorld : SystemBase, IPhysicsSystem
```

**StepPhysicsWorld**是在模拟物理世界中的时间。与BuildPhysicsWorld在同一个Group内，并且在其之后更新。

这个世界主要是Schedule物理模拟的Jobs。并且合并Dependency。

以下方式可以创建**StepPhysicsWorld**

```csharp
StepPhysicsWorld stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
//获取默认世界的StepPhysicsWorld
StepPhysicsWorld stepPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<StepPhysicsWorld>();
```

公有成员变量介绍：

- #### **Simulation**

  - 是一个ISimulation的实例对象用于声明使用的仿真类型。实现ScheduleStepJobs。

- #### FinalSimulationJobHandle

  - Systems which read the simulation results should depend on this

- #### **SimulationCreator()**

  - 是个**ISimulation**委托。

- [ ] ####  <font color=green>目前属实不理解。等日后理解了再完善，等完善了我就把前面的勾勾打上。</font>

## PhysicsWorld

```csharp
[NoAlias]
public struct PhysicsWorld : ICollidable, IDisposable
```

是刚体和joints的集合。其中包含着CollisionWorld和DynamicsWorld。

PhysicsWorld是**CollisionWorld**和**DynamicsWorld**的并集。三者的公有成员变量是相同的。

以下是PhysicsWorld的获取方式：

```csharp
CollisionWorld collisionWorld = buildPhysicsWorld.PhysicsWorld；
```

## CollisionWorld

```csharp
[NoAlias]
public struct CollisionWorld : ICollidable, IDisposable
```

是一些包裹碰撞体的刚体集合。它允许进行碰撞查询，光线投射、overlap测试。

以下方法课以获取到全局的CollisionWorld。

```csharp
CollisionWorld collisionWorld = buildPhysicsWorld.PhysicsWorld.CollisionWorld;
```

公有成员变量介绍：

- #### NumBodies
  - 静态碰撞体（**NumStaticBodies**）+ 动态碰撞体（**NumDynamicBodies**）

- #### Bodies

  - 碰撞体们的NativeArray（StaticBodies+DynamicBodies）

- #### CollisionTolerance

  - 如果刚体之间的距离小于此距离阈值，则始终会在它们之间创建接触。这是一个只有get方法的属性。不能配置
  - 有趣的是开发者再后面加了个//TODO,看来是希望该项可配置的。

## DynamicsWorld

在物理模拟中使用的运动信息的集合。相比PhysicsWorld，CollisionWorld少个个碰撞能力的接口。这恒河里。

```csharp
[NoAlias]
public struct DynamicsWorld : IDisposable
```

获取运动世界的方法和上面是一致的。通过PhysicsWorld就能拿到。

公有变量也不用再介绍了。就是运动数据和Joint的NativeArray。
