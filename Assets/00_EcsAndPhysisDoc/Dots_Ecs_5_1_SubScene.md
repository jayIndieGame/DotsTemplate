## Dots_Ecs_5_1_SubScene

这部分其实没啥内容。因为SubScene是一个引用类型。ForEach的Lamda表达式和JobSystem中都放不进去。所以一切做起来都没有那么愉快。

SubScene会把里面所有的东西都变成Entity。所以。我们一般情况下可以先做好各个SubScene，最后再建立一个MainScene然后把各个Scene组合起来，使用代码控制Scene的Load和UnLoad就可以了。

SubScene允许在一个Scene中放很多很多的东西。然后动态的Stream Loading Subscene 从而拥有很高的性能。省去了原来使用SceneManager.LoadSceneAsync了。并且原来的方法还需要设置DontDestoryOnLoad。现在也完全不需要。只是如果有Component不需要销毁的话。也需要特定去保留对应的Entity。

先说下如何获取SubScene。每一个SubScene都有一个唯一的Hash128类型的数据存他。所以通过这个数据就能获取到SubScene。

通过`subScene.SceneGUID`可以获取到子场景的Hash128。

#### SceneSystem在DefaultWorld中也是存在的。直接就能拿到：

```csharp
SceneSystem sceneSystem= World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<SceneSystem>();
```

#### 加载SubScene的方法：

```csharp
sceneSystem.LoadSceneAsync(subScene.SceneGUID);
```

#### 卸载SubScene的方法：

```csharp
sceneSystem.UnloadScene(subScene.SceneGUID);
```

#### 通过SubScene获取Entity的方法：

```csharp
sceneSystem.GetSceneEntity(subScene.SceneGUID)
```

#### 当然也能通过Entity直接加载或者写在SubScene：

```
sceneSystem.LoadSceneAsync(sceneSystem.GetSceneEntity(subScene.SceneGUID));
```

