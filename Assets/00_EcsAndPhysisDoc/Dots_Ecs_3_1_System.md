## Dots_Ecs_3_1_System

<font size = 6 color=red>**System**</font> 是提供了component data从一个状态到另一个状态的逻辑。

![img](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/images/BasicSystem.png)

系统会自动发现项目中的system并且实例化他们。所有的Systems都会被默认添加到默认的World中。详情见1-2.

System的更新循环是被他的Parent也就是ComponentSystemGroup控制的。

### System的类型

- SystemBase -- 基本的System的类型。
  - 是个继承了ComponentSystemBase的抽象类
  - System Lifecycle callbacks -- System有着自己的生命周期函数
    - OnCreate() -- OnCreate 在第一次调用[OnStartRunning()](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.ComponentSystemBase.OnStartRunning.html#Unity_Entities_ComponentSystemBase_OnStartRunning)和 OnUpdate 之前调用。
    
    - OnStartRunning() -- 在第一次调用 OnUpdate 之前以及系统在停止或禁用后恢复更新时调用。
    
    - OnUpdate -- 当该系统的任何[EntityQueries](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.EntityQuery.html)匹配现有实体、系统具有 [AlwaysUpdateSystem] 属性时，系统会在主线程上每帧调用一次 。
    
    - OnStopRunning() -- 因为没有实体与EntityQuery对应，或者因为将System的Enable属性调整成false时此系统停止运行。该方法调用。
    
    - OnDestroy -- 在系统被销毁时，该方法调用。
  
- EntityCommandBufferSystem -- 提供了[EntityCommandBuffer](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.EntityCommandBuffer.html)实例给其他的系统。每个默认系统组在其子系统列表的开头和结尾处维护一个实体命令缓冲区系统。这允许您对结构更改进行分组，以便它们在帧中产生较少的[同步点](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/sync_points.html)。
- ComponentSystemGroup -- 为其他系统提供嵌套组织和更新顺序。Unity ECS 默认创建多个组件系统组。
- GameObjectConversionSystem -- 将GameObject转换成entity-based。GameObjectConversionSystem 这个系统是在Unity Editor中运行的。

<!--Import-->

**ComponentSystem**和JobComponentSystem和IJobForEach被移除了。但是还没正式的移出DOTS的API。使用SystemBase和Entities.ForEach代替。



### Dependency

是一个**JobHandle**类型的数据**JobHandle**是含有 一个JobGroup和Complete来判断Job是否执行完毕的。也是代表了ECS系统的相关依赖

所以**Dependency**主要是用来防止多个System读写数据时，读发生在了写之前。所以Job的Schedule方法保证了，这个当前Job运行前一定会把他的Dependency的Job运行完。

系统[Dependency](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.SystemBase.Dependency.html#Unity_Entities_SystemBase_Dependency)属性不会跟踪作业可能对通过[NativeArrays](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html)或其他类似容器传递的数据的依赖关系。如果您在一个作业中编写 NativeArray，并在另一个作业中读取该数组，则必须手动添加第一个作业的 JobHandle 作为第二个作业的依赖项（通常使用[JobHandle.CombineDependencies](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.CombineDependencies.html)）。

