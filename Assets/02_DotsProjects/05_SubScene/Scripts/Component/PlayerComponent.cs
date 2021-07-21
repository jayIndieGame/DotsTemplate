using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Dots.SubScene
{
    [GenerateAuthoringComponent]
    public struct PlayerComponent : IComponentData
    {
        public float LoadSceneDistance;
    }

}
