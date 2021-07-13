## SystemBase和ComponentSystem的异同

### SystemBase介绍

**SystemBase**：是一个不安全抽象类，继承自**ComponentSystemBase**，有[**BurstCompile**]特性

目的是在ECS系统中植入了一个System

#### System lifecycle callbacks

他有着自己的生命周期函数：

1. **ComponentSystemBase**.**OnCreate**  -- 系统被创建时
2. **ComponentSystemBase**.**OnStartRunning** -- 第一个OnUpdate之前，或者无论何时系统resumes
3. OnUpdate -- 每一帧
4. **ComponentSystemBase**.**ShouldRunSystem** -- 大概意思就是当OnUpdate执行了，决定System要不要执行的一个检测。
5. **ComponentSystemBase**.**OnStopRunning** -- 在OnDestory之前，无论何时系统停止Update。
6. **ComponentSystemBase**.**OnDestroy** -- 当系统OnDestory

#### System update order

- 所有的system的运行顺序，被ComponentSystemGroup（也是个抽象类，继承自ComponentSystem）决定。
- 把一个system放在group里使用方法UpdateInGroupAttribute
- 使用方法UpdateBeforeAttribute或UpdateAfterAttribute。来具体决定执行顺序

- 默认的，所有system都是可以被发现的，被实例化的，被添加到了Word的SimulationSystemGroup中的。可以使用DisableAutoCreationAttribute来防止一个系统被自动创建。

#### Entity queries

- system查询所有的缓存通过 [Entities.ForEach] 的结构通过方法[ComponentSystemBase.GetEntityQuery]或者[**ComponentSystemBase**.**RequireForUpdate**]

  默认的运行时只被OnUpdate()方法回调。

- 注意一个系统就算没写查询也会在每一帧更行。

#### Entities.ForEach and Job.WithCode constructions

- Entities属性提供了一种方便的机制在entity上迭代。使用[Entities.ForEach]你可以定义你的entity查询，通过具体的lamda表达式让每个entity都跑一边。并且你想另起一个线程schedule或者直接主线程上run都可以。
- [Entities.ForEach]结构可以使用c#compiler extension可以转换成data query的job-based语法这样更有效率。
- Job属性提供了一种和[C# Job]相似的机制。只能使用Schedule来跑[Job.WithCode]这样结构的代码，这样每个lamda表达式就都是一个job

#### System attributes

- **UpdateInGroupAttribute**  -- 把system放在一个ComponentSystemGroup中
- **UpdateBeforeAttribute** -- 在同一个Group中，总是比别的system先更新。
- **UpdateAfterAttribute** -- 后更新。
- **AlwaysUpdateSystemAttribute** -- 每一帧都触发Onupdate更新Attribute
- **DisableAutoCreationAttribute** -- 不要自动生成system
- **AlwaysSynchronizeSystemAttribute** --  force a [sync point] before invoking Onupdate

### SystemBase成员

-  protected JobHandle Dependency *-- 看不懂到时候再补*


暂时不展开了。没有使用过没自己的理解。

---

## ComponentSystem

**ComponentSystem**时一个不安全的抽象部分类，继承ComponetSystemBase。目的也是为了创建一个System。

但是只是创建了一个再主线程上工作的Component System 的子类。ECS时没有用Jobs特别优化的。如果需要使用Job需要使用JobComponentSystem。

与SystemBase的两大区别时：

- systemBase支持Job和Burst。ComponentSystem不支持。
- systemBase可以获取尸体的动态缓冲区。查找实体组件值。ComponentSystem则不行。

相同之处是：

- 都是ComponetSystemBase的子类。同样具有OnUpdate这个抽象方法。可以对实体ForEach来实现某些功能。

---

所以在代码的写法上：

前者需要：

```c#
public class RotationSpeedSystem_ForEach : SystemBase
{
    // OnUpdate runs on the main thread.
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        // Schedule job to rotate around up vector
        Entities
            .WithName("RotationSpeedSystem_ForEach")
            .ForEach((ref Rotation rotation,in RotationSpeed_ForEach rotationSpeed) =>
            {
                //两个四元数相乘
                rotation.Value = math.mul(
                    math.normalize(rotation.Value),
                    //forword、backword是z轴
                    //right、left是x轴
                    //up\down是y轴
                    quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * deltaTime));
                //rotation.Value = rotation.Value + math.forward() * rotationSpeed.RadiansPerSecond * deltaTime;



            })
            .ScheduleParallel();
    }
}
```

```c#
public class LevelUpSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref LevelComponent levelComponent) =>
        {

            levelComponent.level += 1f * Time.DeltaTime;
        });

    }
}
```

可以看到SystemBase和ComponentSystem的主要区别是前者Entity.ForEach必须通过Run(),Schdule()等方法指定在哪个线程上跑，多会儿跑。ComponentSystem则不需要。

并且，SystemBase的OnUpdate的内部是不允许Time.DeltaTime的。应该也是和线程有关。
