using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[InternalBufferCapacity(10)]
public struct TestBuffer : IBufferElementData
{
    // Actual value each buffer element will store.
    public float Value;

    // The following implicit conversions are optional, but can be convenient.
    public static implicit operator float(TestBuffer e)
    {
        return e.Value;
    }

    public static implicit operator TestBuffer(float e)
    {
        return new TestBuffer { Value = e };
    }
}
