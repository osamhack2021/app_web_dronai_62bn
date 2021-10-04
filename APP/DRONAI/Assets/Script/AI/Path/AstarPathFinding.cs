using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Dronai.Path
{
    public class AstarPathFinding : MonoBehaviour
    {
        [SerializeField] private AstarGrid grid = default;

        public void FindPath(Vector3 startPos, Vector3 targetPos)
        {
            AstarNode startNode = grid.NodeFromWorldPoint(startPos);
            AstarNode targetNode = grid.NodeFromWorldPoint(targetPos);

            List<AstarNode> openSet = new List<AstarNode>();
            HashSet<AstarNode> closedSet = new HashSet<AstarNode>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                AstarNode currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                {
                    RetracePath(startNode, targetNode);
                    return;
                }

                foreach (AstarNode neighbour in grid.GetNeighbours(currentNode))
                {
                    if (!neighbour.Walkable || closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                    if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;

                        if (!openSet.Contains(neighbour))
                        {
                            openSet.Add(neighbour);
                        }
                    }
                }
            }
        }

        void RetracePath(AstarNode startNode, AstarNode endNode)
        {
            List<AstarNode> path = new List<AstarNode>();
            AstarNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            path.Reverse();

            grid.path = path;
        }

        private void swap(ref int num1, ref int num2)
        {
            int temp = num1;
            num1 = num2;
            num2 = temp;
        }

        public int GetDistance(AstarNode nodeA, AstarNode nodeB)
        {
            int dx = (int)Math.Abs(nodeA.GridX - nodeB.GridX);
            int dy = (int)Math.Abs(nodeA.GridY - nodeB.GridY);
            int dz = (int)Math.Abs(nodeA.GridZ - nodeB.GridZ);

            // make (dx, dy, dz) to (dx > dy > dz)
            if (dx < dy) swap(ref dx, ref dy);
            if (dx < dz) swap(ref dz, ref dz);
            if (dy < dz) swap(ref dy, ref dz);

            // sqrt(3) = 1.7xxx, sqrt(2) = 1.4xxx
            return 17 * dz + 14 * (dy - dz) + 10 * (dx - dy - dz);
        }
    }
}