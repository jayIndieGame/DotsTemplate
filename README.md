# DotsTemplate

> 该项目使用2020.3LTS
>
> 重要的包：
>
> ECS：Version 0.17.0-preview.42 - May 28, 2021
>
> Jobs：Version 0.8.0-preview.23 - January 22, 2021
>
> Havok Physics for Unity：Version 0.6.0-preview.3 - January 22, 2021

## 项目概述

鉴于网上Unity Dots的教程更新过慢，内容已经过时太久了。而官方的API则是天天都在变。

所以我决定随着官方API的更新，把Unity Dots的功能点和知识点重新梳理一遍。

该项目内部含多个Samples可以比较全面的了解Dots API。并且内容会逐步更新。

## 目录介绍

以下是该项目的目录结构。

<details>
<summary>Assets</summary>
    <p>00_EcsAndPhyDoc</p>
    <p>01_TraditionalProjects</p>
    <p>02_DotsProjects</p>
    <p>99_UtilClass</p>
</details>


- #### **00_EcsAndPhyDoc**

  是**ECS**和**Unity.Physis**、**Havok.Physics**;的相关知识点,后续还会接着补充**Job**和**Brust**的相关内容。内容基本来源于官方手册，一下是使用的手册：

  -  [ECS阅读手册](https://docs.unity3d.com/Packages/com.unity.entities@0.17/manual/ecs_core.html)
  -  [Unity.Physis 文档](https://docs.unity3d.com/Packages/com.unity.physics@0.6/manual/index.html)
  -  [Havok.Physis 文档](https://docs.unity3d.com/Packages/com.havok.physics@0.6/manual/index.html)
  -  [Job System 文档](https://docs.unity3d.com/Manual/JobSystem.html)
  -  [Brust 文档](https://docs.unity3d.com/Packages/com.unity.burst@1.5/manual/docs/QuickStart.html)

  今后的**TODO** **List**：

  - [x] ECS 相关文档整理
  - [ ] Unity.Physis、Havok.Physis文档整理
  - [ ] JobSystem、Brust文档整理

- #### **01_TraditionalProjects**与02_DotsProjects

  - 这两个文件夹内的内容基本是一一对应的。TraditionalProjects代表着面对对象，基本靠Mono实现的一些功能。DotsProjects代表着面对数据，基本靠Ecs+Job System+Brust实现的功能。
  - 02_DotsProjects不会对每一个Api都做一个案例。而是围绕着一个功能取实现。其中牵扯的一些Api会标注出来。
  - 两者的00项目是对Dots和传统面对对象编程的性能做了个初步的比对。后续的项目则是在实现某些具体的功能。
  
- #### 99_UtilClass工具类


  - 写代码时会把一些公有的方法提取出来放在这个文件夹下面。



## 急待解决的问题

- <font color=Brown>DOTS ECS中只要继承了SystemBase无论你在哪个场景下都会执行这个System。有没有一个办法能按照场景执行对应的System。</font>

- <font color= Gold>Physics中Layer BelongsTo、Collide With在代码中是如何实现区分的。</font>
- <font color= DarkGreen>DOTS如何Debug。出了Assertion。怎么打断断点？</font>

## 项目目标

1. 第一阶段以上内容整理完毕即可。
2. 第二阶段会实现DOTS+URP和DOTS+HDRP。

