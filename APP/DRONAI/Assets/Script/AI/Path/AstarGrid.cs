using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dronai.Path
{
    public class AstarGrid : MonoBehaviour
    {

        [SerializeField] private LayerMask unwalkableMask = default;
        [SerializeField] private Vector3 gridWorldSize = default;
        [SerializeField] private float nodeRadius = default;

        private AstarNode[,,] grid = default;
        private float nodeDiameter = default;
        private int gridSizeX, gridSizeY, gridSizeZ;
        public List<AstarNode> path;



        private void Start()
        {
            nodeDiameter = nodeRadius * 2f;
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
            gridSizeZ = Mathf.RoundToInt(gridWorldSize.z / nodeDiameter);

            CreateGrid();
        }

        private void CreateGrid()
        {
            grid = new AstarNode[gridSizeX, gridSizeY, gridSizeZ];
            Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y / 2 - Vector3.forward * gridWorldSize.z / 2;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius) + Vector3.forward * (z * nodeDiameter + nodeRadius);
                        bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));
                        grid[x, y, z] = new AstarNode(walkable, worldPoint, x, y, z);
                    }
                }
            }
        }

        public List<AstarNode> GetNeighbours(AstarNode node)
        {
            List<AstarNode> neighbours = new List<AstarNode>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0)
                        {
                            continue;
                        }

                        int checkX = node.GridX + x;
                        int checkY = node.GridY + y;
                        int checkZ = node.GridZ + z;

                        if (checkX >= 0 && checkX < gridSizeX &&
                            checkY >= 0 && checkY < gridSizeY &&
                            checkZ >= 0 && checkZ < gridSizeZ)
                        {
                            neighbours.Add(grid[checkX, checkY, checkZ]);
                        }
                    }
                }
            }

            return neighbours;
        }

        public AstarNode NodeFromWorldPoint(Vector3 worldPosition)
        {
            float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
            float percentY = (worldPosition.y + gridWorldSize.y / 2) / gridWorldSize.y;
            float percentZ = (worldPosition.z + gridWorldSize.z / 2) / gridWorldSize.z;
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);
            percentZ = Mathf.Clamp01(percentZ);

            int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
            int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
            int z = Mathf.RoundToInt((gridSizeZ - 1) * percentZ);

            return grid[x, y, z];
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, gridWorldSize.z));

            if (grid != null)
            {
                foreach (AstarNode n in grid)
                {
                    Gizmos.color = (n.Walkable) ? Color.white : Color.red;
                    if (path != null)
                    {
                        if (path.Contains(n))
                        {
                            
                        }
                    }

                    Gizmos.DrawCube(n.WorldPosition, Vector3.one * (nodeDiameter - .2f));
                }
            }
        }
    }
}

