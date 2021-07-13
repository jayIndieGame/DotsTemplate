## Dots_Ecs_2_1_Component

<font size = 6 color=red>**Components**</font> 是构成游戏的数据。entities是对components集合的分类，目录。system是定义了行为。

看下图：EntityManager讲**Components**组织成**archetypes**。并且每个Entity都存在一个Chunk（一块儿内存区域）里面。而结构一样的Chunk则被一个同一的Archetype管理。也就是说在给定的Chunk下，实体都有同样的**Components**。ps：我总感觉图不太对，应该是一个Archetype对应多个chunk，一个chunk里面有多个Entities。

--- 知乎大佬解释

**在Unity ECS中,并不只是对Entity进行分组,而是连着Entity对应的Component一起进行分组 - 称作Archetype.每一种Component都存放在连续内存里,而这些Component对应的内存又被分割成固定长度被打包在一块固定大小的内存里 - 称作Chunk,满足一类Archetype的多个Chunk又被一个LinkedList连接起来存于满足条件的Archetype.**

<img src="https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/images/ArchetypeChunkDiagram.png" alt="img" style="zoom:80%;" />

![img](https://pic4.zhimg.com/80/v2-8df7b611b6c09a9b13d5a2669cef4b93_720w.jpg)

Components必须是一个结构体，并且实现了以下其中一个接口

- <font color=green>IComponentData</font> -- 一般情况就用这个

  - IComponentData只能包含entity的示例数据。不包含方法。所有数据传的都是本身而不是应用。所以修改数据必须按照以下规律

    ```c#
    var transform = group.transform[index]; // Read
    
    transform.heading = playerInput.move; // Modify
    transform.position += deltaTime * playerInput.move * settings.playerMoveSpeed;
    
    group.transform[index] = transform; // Write
    ```

    可以看到以上代码都是直接值处理。引用类型逐渐被舍弃，采用值类型来代替。

  - 托管的IComponentData。IComponent也能修饰Class。还得实现IEquatable<T>和重写Object.GetHashCode()除此之外。你在设置值的时候还得在主线程上处理。。但是建议把这种写法ban了。因为会产生以下四点问题

    - 不能使用Brust Compile
    - 不同在Job中使用
    - 不能使用[Chunk memory](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.ArchetypeChunk.html)
    - 会产生GC

  - 非托管的数据类型

    - C#定义的blittable类型。就是Int，uint，float等
    - 布尔值
    - char
    - [BlobAssetReference](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.BlobAssetReference-1.html) (a reference to a Blob data structure)
    - (a fixed-sized character buffer)
    - [fixed arrays](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/fixed-statement) (in an [unsafe](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/unsafe) context)
    - 仅包含以上内容的结构体

  - 一般而言，使用多个较小的Componnet比使用交大数量少的Component更有效率。

- <font color=green>IBufferElementData</font> -- 跟动态buffers相关的实体

  - 这个接口是创建了一结构，这个结构可以被存储于[DynamicBuffer](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.DynamicBuffer-1.html)

    - DynamicBuffer是一个类似于数组的结构，存的是一个entity。DynamicBuffer可以存大量的元素，并且自动调整大小。

    ```c#
    public struct TestBuffer : IBufferElementData
    {
        public int Value;
    }
    DynamicBuffer<TestBuffer> entityBuffers = new DynamicBuffer<TestBuffer>();
    ```

    - 要让一个DynamicBuffer和entity产生联系的话，直接给一个entity添加IBufferElementData就行。而不是给他添加DynamicBuffer。
    - DynamicBuffer的使用：

    ```csharp
    [InternalBufferCapacity(8)]
    public struct FloatBufferElement : IBufferElementData
    {
        // Actual value each buffer element will store.
        public float Value;
    
        // The following implicit conversions are optional, but can be convenient.
        public static implicit operator float(FloatBufferElement e)
        {
            return e.Value;
        }
    
        public static implicit operator FloatBufferElement(float e)
        {
            return new FloatBufferElement {Value = e};
        }
    }
    
    public class DynamicBufferExample : ComponentSystem
    {
        protected override void OnUpdate()
        {
            float sum = 0;
    
            Entities.ForEach((DynamicBuffer<FloatBufferElement> buffer) =>
            {
                for(int i = 0; i < buffer.Length; i++)
                {
                    sum += buffer[i].Value;
                }
            });
    
            Debug.Log("Sum of all buffers: " + sum);
        }
    }
    ```

    可以看到DynamicBuffer是自动创建然后就能用的，所以在EntityQuery上去查DynamicBuffer也是通过IBufferElementData去查。[InternalBufferCapacity(8)]表示chunk内的DynamicBuffer中的元素最多有8个超出8个以后整个dynamicBuffer就会被移到heap上。

  - IBufferElementData与IComponentData有相同的限制。

  - <font color=gray>Buffer到底是啥呢。其实就是一个缓冲区，一个entity进来，把数据留下，但是不够啊，所以就要第二个entity。所以dynamicBuffer就是来处理多个entities的数据混合计算的。不对就当我瞎说的。</font>

- <font color=green>ISharedComponentData</font> -- 组件类型的接口，其值由同一Chunk中的所有实体共享。也就是说拥有相同值的ISharedComponentData其实对应的是同一个Entity。也就是说在同一个[EntityArchetype](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.EntityArchetype.html)中的ISharedComponentData的value修改的时候，其他的Entities也都修改了

- <font color=green>Hybrid Components</font> -- Hybrid Components提供了一种便利的方式可以让ECS和传统项目结合起来。比如光线、反射探针、后处理等等都可以使用HyBrid Components。Hybrid Components主要限制：

  - 不会调用事件函数，仅仅是提供值的。
  - 完全没有性能提升
  - 未来版本中肯定不用了
  - LiveLink是不支持的
  - HyBrid Components只能在混合时创建。

- <font color=green>ISystemStateComponentData</font> -- ISystemStateComponentData 中声明的值和IComponentData是一样的。

  - 继承该接口的Entity是可以存储System state components data的
  - 通用组件和系统状态组件之间的功能差异在于系统状态组件的存在会延迟实体销毁，直到系统明确删除该组件。这种延迟允许系统清除它已创建并与实体关联的任何状态或持久资源。

- <font color=green>ISystemStateSharedComponentData</font> -- 和ISharedComponentData反正就是同结构的Entity是共享该数据的。

- [Blob assets](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.BlobBuilder.html) -- 虽然技术上不是“组件”，但可以使用blob来存储数据。Blob可以由一个或多个使用BlobAssetReference的组件引用，并且是不可变的。您可以使用blob资产在资产之间共享数据，并在C# jobs中访问这些数据。

  

