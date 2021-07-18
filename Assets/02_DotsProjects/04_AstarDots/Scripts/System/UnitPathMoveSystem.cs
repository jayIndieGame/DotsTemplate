using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//TODO 1、System脚本是作用于全局的。想办法让他只对当前场景生效
//TODO 2、ComponentSystem 改成SystemBase
public class UnitPathMoveSystem : ComponentSystem
{

    protected override void OnUpdate()
    {
        if(GameObject.FindObjectOfType<PathFindingGrid>() == null)return;
        ;
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0;
            float cellSize = PathFindingGrid.Instance.pathFindingGrid.GetCellSize();

            int2 XY = PathFindingGrid.Instance.pathFindingGrid.GetXYFromWorld(mousePosition + new Vector3(1, 1) * cellSize * +.5f);

            ValidateGridPosition(ref XY.x, ref XY.y);
            //CMDebug.TextPopupMouse(x + ", " + y);

            Entities.ForEach((Entity entity, DynamicBuffer<PathPositions> pathPositionBuffer, ref Translation translation) => {
                //Debug.Log("Add Component!");
                int2 startXY = PathFindingGrid.Instance.pathFindingGrid.GetXYFromWorld(translation.Value + new float3(1, 1, 0) * cellSize * +.5f);

                ValidateGridPosition(ref startXY.x, ref startXY.y);

                EntityManager.AddComponentData(entity, new PathFindingParams
                {
                    startPosition = new int2(startXY.x, startXY.y),
                    endPosition = new int2(XY.x, XY.y)
                });
            });

            #region test

            //Entities.ForEach((Entity entity,ref Translation translation) =>
            //{
            //    EntityManager.AddComponentData(entity, new PathFindingParams
            //    {
            //        startPosition = new int2(0, 0),
            //        endPosition = new int2(4, 0)
            //    });
            //});

            #endregion
        }
    }
    private void ValidateGridPosition(ref int x, ref int y)
    {
        x = math.clamp(x, 0, PathFindingGrid.Instance.pathFindingGrid.GetWidth() - 1);
        y = math.clamp(y, 0, PathFindingGrid.Instance.pathFindingGrid.GetHeight() - 1);
    }

}
