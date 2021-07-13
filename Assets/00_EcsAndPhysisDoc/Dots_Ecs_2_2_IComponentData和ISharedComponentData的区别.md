## IComponentData和ISharedComponentData的区别

### Interface ISharedComponentData

首先这玩意儿是个接口，且继承这个接口的结构的值是被所有entities在同一个chunk里面共享的。

不太清楚。写了个struct继承这玩意儿啥也不用实现。这个接口里也没东西。

但是文档里说**ISharedComponentData**必须实现 and。我也不知道and是个啥。

**ISharedComponentData**允许引用类型的字段。但是开发者计划限制**ISharedComponentData**用于非托管的blittable类型（见下面有详细说明）。

### Interface IComponentData

这玩意儿也是个接口。这里面依然啥也没有。

这个接口的实现必须是个结构体。类不行。文档上这么说的。并且只能包含非托管的 blittable 类型

包括。

- C#-defined [blittable types](https://docs.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types)
- bool
- char
- [BlobAssetReference](https://docs.unity3d.com/Packages/com.unity.entities@0.17/api/Unity.Entities.BlobAssetReference-1.html) (a reference to a Blob data structure)
- (a fixed-sized character buffer)
- [fixed arrays](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/fixed-statement) (in an [unsafe](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/unsafe) context)
- structs containing these unmanaged, blittable fields

----

先暂时不管那么多。。

现在一般就是自己创建的Component都继承自IComponentData。其中系统自带的meshrender是继承自ISharedComponentData的。

并且一个entity和RenderMesh绑定并设置初始mesh和material依然是看不到的。entity中必须添加  **RenderBounds** :的**IComponentData**并且，有的shader也还是看不到的。需要挑选合适的shader。