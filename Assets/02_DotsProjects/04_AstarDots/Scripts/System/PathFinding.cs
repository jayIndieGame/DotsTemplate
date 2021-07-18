using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.PlayerLoop;


//TODO 把ComponentSystem改成SytemBase
public class PathFinding : ComponentSystem
{


    private const int Move_Diagonal_Cost = 14;
    private const int Move_Straight_Cost = 10;

    protected override void OnUpdate()
    {
        int gridWidth = PathFindingGrid.Instance.pathFindingGrid.GetWidth();
        int gridHeight = PathFindingGrid.Instance.pathFindingGrid.GetHeight();

        int2 gridSize = new int2(gridWidth, gridHeight);

        List<FindPathJob> findPathJobList = new List<FindPathJob>();
        NativeList<JobHandle> jobHandleList = new NativeList<JobHandle>(Allocator.Temp);

        NativeArray<PathNode> pathNodeArray = GetPathNodeArray();

        Entities.ForEach((Entity entity,ref PathFindingParams pathFindingParams) =>
        {
            NativeArray<PathNode> tmpPathNodeArray = new NativeArray<PathNode>(pathNodeArray, Allocator.TempJob);

            FindPathJob findPathJob = new FindPathJob
            {
                gridSize = gridSize,
                pathNodeArray = tmpPathNodeArray,
                startPosition = pathFindingParams.startPosition,
                endPosition = pathFindingParams.endPosition,
                entity = entity
            };
            findPathJobList.Add(findPathJob);
            jobHandleList.Add(findPathJob.Schedule());

            PostUpdateCommands.RemoveComponent<PathFindingParams>(entity);
        });

        JobHandle.CompleteAll(jobHandleList);

        foreach (FindPathJob findPathJob in findPathJobList)
        {
            new SetBufferPathJob
            {
                entity = findPathJob.entity,
                gridSize = findPathJob.gridSize,
                pathNodeArray = findPathJob.pathNodeArray,
                pathfindingParamsComponentDataFromEntity = GetComponentDataFromEntity<PathFindingParams>(),
                pathFollowComponentDataFromEntity = GetComponentDataFromEntity<PathFollow>(),
                pathPositionBufferFromEntity = GetBufferFromEntity<PathPositions>(),
            }.Run();
        }

        pathNodeArray.Dispose();
    }
    [BurstCompile]
    private struct SetBufferPathJob : IJob
    {

        public int2 gridSize;

        [DeallocateOnJobCompletion]
        public NativeArray<PathNode> pathNodeArray;

        public Entity entity;

        public ComponentDataFromEntity<PathFindingParams> pathfindingParamsComponentDataFromEntity;
        public ComponentDataFromEntity<PathFollow> pathFollowComponentDataFromEntity;
        public BufferFromEntity<PathPositions> pathPositionBufferFromEntity;

        public void Execute()
        {
            DynamicBuffer<PathPositions> pathPositionBuffer = pathPositionBufferFromEntity[entity];
            pathPositionBuffer.Clear();

            PathFindingParams pathfindingParams = pathfindingParamsComponentDataFromEntity[entity];
            int endNodeIndex = CalculateIndex(pathfindingParams.endPosition.x, pathfindingParams.endPosition.y, gridSize.x);
            PathNode endNode = pathNodeArray[endNodeIndex];
            if (endNode.cameFromNodeIndex == -1)
            {
                // Didn't find a path!
                //Debug.Log("Didn't find a path!");
                pathFollowComponentDataFromEntity[entity] = new PathFollow { pathIndex = -1 };
            }
            else
            {
                // Found a path
                CalculatePath(pathNodeArray, endNode, pathPositionBuffer);

                pathFollowComponentDataFromEntity[entity] = new PathFollow { pathIndex = pathPositionBuffer.Length - 1 };
            }

        }
    }

    [BurstCompatible]
    private struct FindPathJob : IJob
    {
        public int2 gridSize;

        public NativeArray<PathNode> pathNodeArray;

        public int2 startPosition;
        public int2 endPosition;

        public Entity entity;
        //public ComponentDataFromEntity<PathFollow> PathFollowComponentDataFromEntity;
        //public DynamicBuffer<PathPositions> pathPositionsBuffer;

        public void Execute()
        {
            for (int i = 0; i < pathNodeArray.Length; i++)
            {
                PathNode pathNode = pathNodeArray[i];
                pathNode.hcost = CalculateDistanceCost(new int2(pathNode.X, pathNode.Y), endPosition);
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[i] = pathNode;
            }

            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(8, Allocator.Temp);
            neighbourOffsetArray[0] = new int2(-1, 0); // Left
            neighbourOffsetArray[1] = new int2(+1, 0); // Right
            neighbourOffsetArray[2] = new int2(0, +1); // Up
            neighbourOffsetArray[3] = new int2(0, -1); // Down
            neighbourOffsetArray[4] = new int2(-1, -1); // Left Down
            neighbourOffsetArray[5] = new int2(-1, +1); // Left Up
            neighbourOffsetArray[6] = new int2(+1, -1); // Right Down
            neighbourOffsetArray[7] = new int2(+1, +1); // Right Up

            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);

            PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, gridSize.x)];
            startNode.gcost = 0;
            pathNodeArray[startNode.Index] = startNode;

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.Index);

            while (openList.Length > 0)
            {
                int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
                PathNode currentNode = pathNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex)
                {
                    // Reached our destination!
                    break;
                }

                // Remove current node from Open List
                for (int i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == currentNodeIndex)
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closedList.Add(currentNodeIndex);

                for (int i = 0; i < neighbourOffsetArray.Length; i++)
                {
                    int2 neighbourOffset = neighbourOffsetArray[i];
                    int2 neighbourPosition = new int2(currentNode.X + neighbourOffset.x, currentNode.Y + neighbourOffset.y);

                    if (!IsPositionInGrid(neighbourPosition, gridSize))
                    {
                        // Neighbour not valid position
                        continue;
                    }

                    int neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, gridSize.x);

                    if (closedList.Contains(neighbourNodeIndex))
                    {
                        // Already searched this node
                        continue;
                    }

                    PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
                    if (!neighbourNode.isWalkable)
                    {
                        // Not walkable
                        continue;
                    }

                    int2 currentNodePosition = new int2(currentNode.X, currentNode.Y);

                    int tentativeGCost = currentNode.gcost + CalculateDistanceCost(currentNodePosition, neighbourPosition);
                    if (tentativeGCost < neighbourNode.gcost)
                    {
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.gcost = tentativeGCost;
                        pathNodeArray[neighbourNodeIndex] = neighbourNode;

                        if (!openList.Contains(neighbourNode.Index))
                        {
                            openList.Add(neighbourNode.Index);
                        }
                    }

                }
            }


            neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
        }

        

    }
    private static void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode, DynamicBuffer<PathPositions> buffer)
    {
        if (endNode.cameFromNodeIndex == -1)
        {
            //找不到路
        }
        else
        {
            buffer.Add(new PathPositions { position = new int2(endNode.X, endNode.Y) });

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1)
            {
                PathNode camePathNode = pathNodeArray[currentNode.cameFromNodeIndex];
                buffer.Add(new PathPositions { position = new int2(camePathNode.X, camePathNode.Y) });
                currentNode = camePathNode;
            }

        }
    }
    private static NativeList<int2> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode)
    {
        if (endNode.cameFromNodeIndex == -1)
        {
            // Couldn't find a path!
            return new NativeList<int2>(Allocator.Temp);
        }
        else
        {
            // Found a path
            NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
            path.Add(new int2(endNode.X, endNode.Y));

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1)
            {
                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                path.Add(new int2(cameFromNode.X, cameFromNode.Y));
                currentNode = cameFromNode;
            }

            return path;
        }
    }
    private static NativeArray<PathNode> GetPathNodeArray()
    {
        Grid<GridNode> grid = PathFindingGrid.Instance.pathFindingGrid;

        int2 gridSize = new int2(grid.GetWidth(), grid.GetHeight());
        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(gridSize.x * gridSize.y, Allocator.TempJob);

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                PathNode pathNode = new PathNode();
                pathNode.X = x;
                pathNode.Y = y;
                pathNode.Index = CalculateIndex(x, y, gridSize.x);

                pathNode.gcost = int.MaxValue;

                pathNode.isWalkable = grid.GetGridObject(x, y).IsWalkable();
                pathNode.cameFromNodeIndex = -1;

                pathNodeArray[pathNode.Index] = pathNode;
            }
        }

        return pathNodeArray;
    }

    private static bool IsPositionInGrid(int2 gridPosition, int2 grid) => gridPosition.x >= 0 && gridPosition.x < grid.x && gridPosition.y >= 0 && gridPosition.y < grid.y;

    private static int CalculateDistanceCost(int2 position, int2 endPosition)
    {
        int xDistance = Mathf.Abs(position.x - endPosition.x);
        int yDistance = Mathf.Abs(position.y - endPosition.y);
        int remainning = Mathf.Abs(xDistance - yDistance);
        return Move_Diagonal_Cost * Mathf.Min(xDistance, yDistance) + remainning * Move_Straight_Cost;
    }

    private static int CalculateIndex(int x, int y, int column) => x + y * column;

    private static int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
    {
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Length; i++)
        {
            PathNode testPathNode = pathNodeArray[openList[i]];

            if (testPathNode.fcost < lowestCostPathNode.fcost)
            {
                lowestCostPathNode = testPathNode;
            }
        }

        return lowestCostPathNode.Index;
    }


    private struct PathNode
    {
        public int X;
        public int Y;

        public int Index;

        public int gcost;
        public int hcost;
        public int fcost => gcost + hcost;

        public bool isWalkable;

        public int cameFromNodeIndex;

        public void SetWalkableFalse()
        {
            isWalkable = false;
        }
    }

}
