using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class CharacterMove : MonoBehaviour, IConvertGameObjectToEntity
{
    public float MoveSpeed;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CharacterComponent
        {
            MoveSpeed = MoveSpeed
        });
    }
}
