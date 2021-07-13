using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct PlayerComponent : IComponentData
{
    //public NativeArray<Entity> EnemyEntities;
    public float Angle;
    public float Speed;
    public float Radius;
}
