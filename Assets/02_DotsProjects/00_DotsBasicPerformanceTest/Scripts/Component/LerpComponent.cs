using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public struct LerpComponent : IComponentData
{
    public float LerpT;
    public Translation endPos;
}
