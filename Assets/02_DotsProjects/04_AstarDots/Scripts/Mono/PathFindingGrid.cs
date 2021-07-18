using System.Collections;
using System.Collections.Generic;
using DOTS.Utils;
using UnityEngine;

public class PathFindingGrid : MonoBehaviour
{
    public static PathFindingGrid Instance { get; private set; }

    [SerializeField] private PathFindingVisual pathfindingVisual;
    public Grid<GridNode> pathFindingGrid;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        pathFindingGrid = new Grid<GridNode>(30, 15, 1f, Vector3.zero, (Grid<GridNode> grid, int x, int y) => new GridNode(grid, x, y));

        pathFindingGrid.GetGridObject(2, 0).SetIsWalkable(false);

        pathfindingVisual.SetGrid(pathFindingGrid);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePosition = GetMouseWorldPosition() + (new Vector3(+1, +1) * pathFindingGrid.GetCellSize() * .5f);
            GridNode gridNode = pathFindingGrid.GetGridObject(mousePosition);
            if (gridNode != null)
            {
                gridNode.SetIsWalkable(!gridNode.IsWalkable());
            }
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0f;
        return worldPosition;
    }
}
