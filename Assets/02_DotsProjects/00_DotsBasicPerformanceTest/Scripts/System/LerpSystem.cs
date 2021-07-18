using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class LerpSystem :SystemBase
{
    private EntityQuery query;
    protected override void OnCreate()
    {

        query = GetEntityQuery(typeof(Translation), ComponentType.ReadOnly<LerpComponent>());
    }

    protected override void OnUpdate()
    {
        var translationHandle = GetComponentTypeHandle<Translation>();
        var lerpHandle = GetComponentTypeHandle<LerpComponent>(true);

        JobToLerp job = new JobToLerp
        {
            transHandle = translationHandle,
            lerpHandle = lerpHandle,

        };

        Dependency = job.ScheduleParallel(query,1,Dependency);

    }

    [BurstCompile]
    struct JobToLerp:IJobEntityBatch
    {
        public ComponentTypeHandle<Translation> transHandle;

        [ReadOnly] public ComponentTypeHandle<LerpComponent> lerpHandle;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var transArray = batchInChunk.GetNativeArray(transHandle);
            var lerpArray = batchInChunk.GetNativeArray(lerpHandle);


            for (int i = 0; i < batchInChunk.Count; i++)
            {
                var translation = transArray[i];
                var lerp = lerpArray[i];

                Vector3 tranValueVector = new Vector3(translation.Value.x, translation.Value.y, translation.Value.z);
                Vector3 lerpVector3 = new Vector3(lerp.endPos.Value.x, lerp.endPos.Value.y, lerp.endPos.Value.z);
                if (Vector3.Distance(tranValueVector, lerpVector3) > 0.2f)
                {
                    Vector3 lerpPosition = Vector3.Lerp(tranValueVector, lerpVector3, lerp.LerpT);
                    float3 lerpPositionFloat3 = new float3(lerpPosition.x, lerpPosition.y, lerpPosition.z);

                    transArray[i] = new Translation { Value = lerpPositionFloat3 };
                }
            }
        }
    }
}
