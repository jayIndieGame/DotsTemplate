## ECS（二）_World介紹

<font size = 6>**World**</font> 组织各个实体去不同群体。一个世界包含着一个**EntityManager**和一系列的**Systems**。一个游戏内你想创建多少的个**World**都可以。但是Systems只能操作和自己同一个World的Entities。

默认的Unity会创建一个默认的World。当你的程序启动，或者进入PlayMode。Unity都会实例化所有的Systems并且把他们加到这个默认的World里面。通过**World.DefaultGameObjectInjectionWorld**可以访问默认World。

系统还为Editor建立了EditorWorld。反正我没有过。以后碰到再补充。

### Managing systems

World提供了创建、获取、删除System的方法。

```csharp
    World world = World.DefaultGameObjectInjectionWorld;
    LerpSystem lerp = world.AddSystem<LerpSystem>(new LerpSystem());
    world.CreateSystem<LerpSystem>();
    world.DestroySystem(lerp);
```

但实际上，System默认已经被加入到defaultWorld中了。

可以使用**GetOrCreateSystem**获取这个系统的实例。

```csharp
LerpSystem getLerp = world.GetOrCreateSystem<LerpSystem>();
```

### Time

Time相关的属性也被World管理着。Unity会为每个世界自动创建一个TimeData的Entity。这个Time会自动被**UpdateWorldTimeSystem**更新。

![image-20210707104424216](C:\Users\lovel\AppData\Roaming\Typora\typora-user-images\image-20210707104424216.png)

对于Time可以设定世界时间：World.SetTime。也可以使用PushTime暂时的改变时间。然后PopTimeReturn前一个时间。

### Custom Initialization

可以通过[ICustomBootstrap](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.ICustomBootstrap.html)接口实现。我这就不看了。后续遇到再看。