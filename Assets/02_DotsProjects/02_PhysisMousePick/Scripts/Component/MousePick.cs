using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.MousePickBehaviour
{
    public struct MousePick : IComponentData
    {
        public int IgnoreTriggers;

    }

}
