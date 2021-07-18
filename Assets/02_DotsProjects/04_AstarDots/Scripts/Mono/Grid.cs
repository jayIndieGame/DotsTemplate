using System;
using System.Collections;
using System.Collections.Generic;
using DOTS.Utils;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class Grid<TGrid>
{

    public event EventHandler<OnGridObjectChangedEventArgs> OnGridObjectChanged;
    public class OnGridObjectChangedEventArgs : EventArgs
    {
        public int x;
        public int y;
    }

    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private TGrid[,] gridArray;
    //private TextMesh[,] debugTextMeshes;


    public Grid(int width, int height,float cellSize,Vector3 originPosition,Func<Grid<TGrid>,int,int,TGrid> createGridObject)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        gridArray = new TGrid[width, height];
        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int y = 0; y < gridArray.GetLength(1); y++)
            {
                gridArray[x, y] = createGridObject(this, x, y);
            }
        }

        #region Debug
        //debugTextMeshes = new TextMesh[width, height];
        //for (int x = 0; x < gridArray.GetLength(0); x++)
        //{
        //    for (int y = 0; y < gridArray.GetLength(1); y++)
        //    {
        //        debugTextMeshes[x, y] = UtilsClass.CreateWorldText(gridArray[x, y].ToString(), null, GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * .5f, 20, Color.white,
        //            TextAnchor.MiddleCenter);
        //        Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, 100f);
        //        Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, 100f);
        //    }

        //}
        //Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
        //Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);

        #endregion



    }

    public int2 GetXYFromWorld(Vector3 worldPosition) => new int2(Mathf.FloorToInt((worldPosition - originPosition).x / cellSize), Mathf.FloorToInt((worldPosition - originPosition).y / cellSize));
    public Vector3 GetWorldPosition(int x,int y) => new Vector3(x, y) * cellSize + originPosition;
    public int GetWidth() => width;
    public int GetHeight() => height;
    public float GetCellSize() => cellSize;
    public TGrid GetGridObject(Vector3 WorldPosition) => GetGridObject(GetXYFromWorld(WorldPosition).x, GetXYFromWorld(WorldPosition).y);
    public void SetGridObject(Vector3 WorldPosition, TGrid value) => SetGridObject(GetXYFromWorld(WorldPosition).x, GetXYFromWorld(WorldPosition).y, value);

    public void SetGridObject(int x, int y, TGrid value)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            gridArray[x, y] = value;
            if (OnGridObjectChanged != null) OnGridObjectChanged(this, new OnGridObjectChangedEventArgs { x = x, y = y });
        }
    }
    public void TriggerGridObjectChanged(int x, int y)
    {
        if (OnGridObjectChanged != null) OnGridObjectChanged(this, new OnGridObjectChangedEventArgs { x = x, y = y });
    }

    public TGrid GetGridObject(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            return gridArray[x, y];
        }
        else
        {
            return default(TGrid);
        }
    }


}
