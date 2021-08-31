# IJobEntityBatch与Burst

### Convert与ConvertToEntity

- ### <font size =5>**Convert**</font><font size =3>    方法来自于**IConvertGameObjectToEntity**接口</font>

```csharp
public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
```

这个方法声明的功能相当于是把一个继承了**IComponentData**接口的**struct**赋给**EntityManager**中所有的**Entity**。其中**GameObjectConversionSystem**是把**GameObjects**转化为**Entities**的一个**System**。

例子：

```csharp
public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
{
    var data = new RotationSpeed_IJobEntityBatch { RadiansPerSecond = math.radians(DegreesPerSecond) };
    
    dstManager.AddComponentData(entity, data);
}
```

其中**RotationSpeed_IJobEntityBatch**是一个**IComponentData**

可以通过**EntityManager**的实例调用**AddComponentData**方法来增加Entity具备的ComponentData。**AddComponentData**是具有泛型的，泛型约束为**Struct**，**IComponentData**

- #### <font size =5>**ConvertToEntity**</font><font size =3>   方法来自于**Unity.Entities**下的一个类</font>

源码如下

```c#
[DisallowMultipleComponent]
[AddComponentMenu("DOTS/Convert To Entity")]
public class ConvertToEntity : MonoBehaviour
{
    public enum Mode
    {
        ConvertAndDestroy,
        ConvertAndInjectGameObject
    }

    public Mode ConversionMode;

    void Awake()
    {
        if (World.DefaultGameObjectInjectionWorld != null)
        {
            var system = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ConvertToEntitySystem>();
            system.AddToBeConverted(World.DefaultGameObjectInjectionWorld, this);
        }
        else
        {
            UnityEngine.Debug.LogWarning($"{nameof(ConvertToEntity)} failed because there is no {nameof(World.DefaultGameObjectInjectionWorld)}", this);
        }
    }
}
```

可见这个脚本是MonoBehaviour的。可以挂载到GameObject上。该物体运行起来后会直接变成Entity。并且物体上原有的一些IComponentData组件也能直接继承到Entity上。

##### <font color= red>IComponnet直接挂载到物体上的方法：</font>

- **[GenerateAuthoringComponent]**特性

源码：

```csharp
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
  public class GenerateAuthoringComponentAttribute : Attribute
  {
  }
```

添加了该特性的IComponentData的Struct可以直接挂到GameObject上不用MonoBehaviour。Unity 会自动创建一个包含着该Struct的公共变量的 MonoBehaviour 类并提供了一个转化方法能把所有变量转化为运行时的ComponentData。



##### 总之，你可以选择不用GenerateAuthoringComponent特性，自己写个类继承Mono，和IConvertGameObjectToEntity接口实现Convert方法来自己添加Component至Entity。

##### 也可以就把IComponentData的Struct的挂载到GameObject上通过ConvertToEntity实现Conent的挂载

-----

### IJob 、IJobParallelFor、IJobChunck、IJobentityBathch

IJob是Job的基本的接口，这些接口也都是去继承struct的。就是声明一个需要在线程上跑的方法。这些Job接口都有一个Execute的方法，这些job实例化后，可以通过ScheduleParaller或者Schedule等方法在不同的线程里跑里面的execute。

你会想struct是怎么执行一个不属于他的schedule方法呢，其实是通过泛型拓展方法实现的。

```csharp
namespace Unity.Jobs
{
  [JobProducerType(typeof (IJobExtensions.JobStruct<>))]
  public interface IJob
  {
    /// <summary>
    ///   <para>Implement this method to perform work on a worker thread.</para>
    /// </summary>
    void Execute();
  }
}
```

对于一个数组中的数据需要复杂计算。可以使用**IJobParallelFor**

```csharp
namespace Unity.Jobs
{
  [JobProducerType(typeof (IJobParallelForExtensions.ParallelForJobStruct<>))]
  public interface IJobParallelFor
  {
    /// <summary>
    ///   <para>Implement this method to perform work against a specific iteration index.</para>
    /// </summary>
    /// <param name="index">The index of the Parallel for loop at which to perform work.</param>
    void Execute(int index);
  }
}

```

index可以传数组长度等能表明循环次数的即可。

### **IJobEntityBatch**

```csharp
namespace Unity.Entities
{
  [JobProducerType(typeof (JobEntityBatchExtensions.JobEntityBatchProducer<>))]
  public interface IJobEntityBatch
  {
    void Execute(ArchetypeChunk batchInChunk, int batchIndex);
  }
}
```

**IJobEntityBatch**是一种**IJob**类型，它在一组**ArchetypeChunk**实例上进行迭代，其中每个实例表示一个块中连续的一批实体。第一个参数

前面接触了**EntityArchetype**。EntityArchetype是构建包含一些列**ComponentType**的Entities的一个结构体。

**ArchetypeChunk**是包含共享同一原型的实体的组件的非托管内存块。也就是说，entities是存在chunck里面的。

具体存进去的操作方式：

```csharp
public ComponentTypeHandle<Rotation> RotationTypeHandle;
[ReadOnly] public ComponentTypeHandle<RotationSpeed_IJobEntityBatch> RotationSpeedTypeHandle;
```

先定义了几个**ComponentTypeHandle**里面的泛型是IComponentData

```csharp
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
```

### IJobChunck

这个是已经弃用了的。用**IJobEntityBatch**就好了。与Enities.ForEach相同的是：

- 他们都可以操作每一个Entitie中的Component
- 他们都可以利用Job系统，执行Schedule，或者Schedule。Enities.ForEach注意如果要使用JobSystem的话 需要让结构体继承SystemBase而不是ComponentSystem。因为ComponentSystem就规定是主线程上跑的。

**IJobEntityBatch**更好的是他操作的Entities更精细一些。可以通过**EntityQuery**把对应的Entities查出来。然后通过ComponentTypeHandle<T>把泛型类型T的Chunk中T类型的作为NativeArray传出来，精细操作。这样更节省性能。并且可以设置一个batch把chunk拆成几份。一般来说chunk不拆分是挺好的。但是可以根据实际项目调整，

-----

### BurstCompile

BurstCompile直接可以对类、属性、结构体、方法添加。总之就是让速度加快。

但是如果其中含有非托管类型的数据就会报错。比如方法内部含有Camera.main。这样就会报错。

和IComponentData的Struct一样。其中不能包含非托管类型的数据。

