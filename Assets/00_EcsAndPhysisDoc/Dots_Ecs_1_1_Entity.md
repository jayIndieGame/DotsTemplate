## Entity相关手册

从基本了解过ECS、IJob和Burst之后。会按照官网文档的API顺序过一遍。

![image-20210706094029449](C:\Users\lovel\AppData\Roaming\Typora\typora-user-images\image-20210706094029449.png)

## Entities

### Creating entities

1. 在场景中添加GameObject，并添加脚本Convert to Entity
2. 对于冬天添加的Entities，可以在systems中创建一个创建entities的job。
3. 也可以直接使用**EntityMananger.CreateEntity**这个方法。这个方法效率是最高的。

#### Creating entities with an EntityManager

使用**EntityMananger.CreateEntity**有一下几种方式可以创建单个Enity：

- 使用ComponnetType的NativeArray来创建。

```csharp
Entity createEntity = entityManager.CreateEntity(typeof(Translation), ComponentType.ReadOnly<Rotation>(), typeof(DragComponent));
```

- 使用**EntityArchetype**创建**Entity**

```csharp
EntityArchetype entityArchetype = entityManager.CreateArchetype(
        typeof(LevelComponent),
        typeof(RenderMesh),
        typeof(Translation),
        typeof(LocalToWorld),
        typeof(RenderBounds),
        typeof(DragComponent),
        typeof(PhysicsCollider),
        typeof(PhysicsMass),
        typeof(PhysicsVelocity),
        typeof(PhysicsGravityFactor)
    );
Entity createEntityByEntityArchetype = entityManager.CreateEntity(entityArchetype);
```

- 对于一个已经存在的entity，复制他和他里面的数据。使用**Instantiate**

```csharp
NativeArray<Entity> cloneArray = new NativeArray<Entity>(2,Allocator.Temp);
entityManager.Instantiate(createEntityByComponentType, cloneArray);
cloneArray.Dispose();
```

这里就是先创建了一个空的Entity的Array。然后我们把createEntityByComponentType这个entity赋给cloneArray。最后Dispose是使用完了以后记得丢弃掉。

- 创建一个Enitiy但是其中并不包含Component，然后再加进来。

```csharp
Entity emptyEntity = entityManager.CreateEntity();
entityManager.AddComponent<Translation>(emptyEntity);
```

总之使用EntityManager的全局实例来搞就行。

### Accessing entity data

遍历全局的Entities是一种非常常见的操作。有以下几种方式

- [SystemBase.Entities.ForEach](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/entities_job_foreach.html) -- 特点就是简单高效好用。

  注意点：

  - 继承于SystemBase的ForEach是可以用Job来跑的。
  - 继承于ComponentSystem的ForEach只能在主线程上跑。
  - 需要使用的值类型变量要在Lamda外部，OnUpdate内部声明，如果声明在类里就变成引用变量了，会报错。如果必须要用的引用变量的值，用ref传入。如果是只读引用变量用in修饰。

- [IJobEntityBatch](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/ecs_ijobentitybatch.html) -- 特点是对Enities的操作更精细。节省性能。可以支持更复杂的情况

  注意点：

  - **IJobEntityBatch**的第一个参数是**ArchetypeChunk**意指包含着合格Entities的内存块。

    我们可以通过**batchInChunk.GetNativeArray(ComponentTypeHandle)**的方式获取指定类型的NativeArray

  1. 当需要按照索引处理Entities，使用IJobEntityBatchWithIndex。否则**IJobEntityBatch**效率更高。

  2. **IJobEntityBatch**处理作业的顺序：

     1. 使用EntityQuery查询数据。

        ```csharp
        //方法一
        query = GetEntityQuery(typeof(Rotation),ComponentType.ReadOnly<RotationSpeed_IJobEntityBatch>(),ComponentType.Exclude<Translation>());
        //方法二
        var description = new EntityQueryDesc()
        {
        	All = new ComponentType[]
        	{ComponentType.ReadWrite<Rotation>(),
        	ComponentType.ReadOnly<RotationSpeed_IJobEntityBatch>()},
            None =  new ComponentType[]
            {ComponentType.ReadOnly<Translation>()}
        };
        query = this.GetEntityQuery(description);
        ```

        注：typeof可以获取ComponentType,默认是ReadWrite的。但是用Component.ReadOnly就可以获得只读的查询类型。

        方法二中，All，None，Any一开始都是Empty的。

        - Any，有一个或多个声明的Component就能放到这个集合里
        - None，是只要包含这个Component就被这个集合排除。
        - All，声明的Component必须全都有才能放进这个集合。

     2. 使用**IJobEntityBatch**或**IJobEntityBatchWithIndex**定义job结构。

     3. 声明作业访问的数据指定该数据是读的还是写的。

     4. 编写作业结构的**Excute**函数。获取作业读取或写入的组件的 NativeArray 实例，然后迭代当前批处理以执行所需的工作。

        ```csharp
        [BurstCompile]
        struct RotationSpeedJob : IJobEntityBatch
        {
            public float DeltaTime;
            public ComponentTypeHandle<Rotation> RotationTypeHandle;
            [ReadOnly] public ComponentTypeHandle<RotationSpeed_IJobEntityBatch> RotationSpeedTypeHandle;
            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                var chunkRotations = batchInChunk.GetNativeArray(RotationTypeHandle);
                var chunkRotationSpeeds = batchInChunk.GetNativeArray(RotationSpeedTypeHandle);
                for (var i = 0; i < batchInChunk.Count; i++)
                {
                    var rotation = chunkRotations[i];
                    var rotationSpeed = chunkRotationSpeeds[i];
        
                    // Rotate something about its up vector at the speed given by RotationSpeed_IJobChunk.
                    chunkRotations[i] = new Rotation
                    {
                        Value = math.mul(math.normalize(rotation.Value),
                            quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * DeltaTime))
                    };
                }
            }
        }
        ```

        注：

        - **[BurstCompile]**可以加速这部分的代码速度。该方法内不准使用引用变量。
        - [ComponentTypeHandle](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.ComponentTypeHandle-1.html) -- 允许我在 Execute 函数访问存储在当前块中的实体组件和缓冲区。

     5. 在系统 OnUpdate 函数中[调度作业](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/ecs_ijobentitybatch.html#schedule-the-job)，将标识要处理的实体的实体查询传递给调度函数（ScheduleParallel、Schedule）。

        ```csharp
        //方法1
        var rotationType = GetComponentTypeHandle<Rotation>();
        var rotationSpeedType = GetComponentTypeHandle<RotationSpeed_IJobEntityBatch>(true);
        var job = new RotationSpeedJob()
            {
                RotationTypeHandle = rotationType,
                RotationSpeedTypeHandle = rotationSpeedType,
                DeltaTime = Time.DeltaTime
        	};
        //方法二
        var job = new RotationSpeedJob();
        job.RotationSpeedTypeHandle = GetComponentTypeHandle<RotationSpeed_IJobEntityBatch>(false);
        job.RotationTypeHandle = GetComponentTypeHandle<Rotation>();
        job.DeltaTime = Time.DeltaTime;
        
        Dependency = job.ScheduleParallel(m_Query, 1, Dependency);
        Dependency.Complete();
        ```

- [Manual iteration](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/manual_iteration.html) -- 手动方法。如果前面的方法还不够的话 `IJobParallelFor` 可以去遍历一个包含着ENtyties或者Chunk的 `NativeArray` 

  - 这个可以具体看我写的《2021-7-5 IJobEntityBatch与Burst 》
  - 注以下的接口就别用了，新代码不支持了。
    - IJobChunk
    - IJobForEach
    - IJobForEachWithEntity
    - ComponentSystem
    - JobComponentSystem

  

