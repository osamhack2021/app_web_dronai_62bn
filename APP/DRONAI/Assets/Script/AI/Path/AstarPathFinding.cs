using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Dronai.Path
{
    public class AstarPathFinding : MonoBehaviour
    {
        private AstarPathRequestManager requestManager;
        [SerializeField] private AstarGrid grid = default;

        private void Awake()
        {
            if (requestManager == null) requestManager = GetComponent<AstarPathRequestManager>();
            if (grid == null) grid = GetComponent<AstarGrid>();
        }

        public void StartFindPath(Vector3 startPos, Vector3 targetPos)
        {
            StartCoroutine(FindPath(startPos, targetPos));
        }
        private IEnumerator FindPath(Vector3 startPos, Vector3 targetPos)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Vector3[] waypoints = new Vector3[0];
            bool pathSucess = false;

            AstarNode startNode = grid.NodeFromWorldPoint(startPos);
            AstarNode targetNode = grid.NodeFromWorldPoint(targetPos);

            if (startNode.Walkable && targetNode.Walkable)
            {
                Heap<AstarNode> openSet = new Heap<AstarNode>(grid.MaxSize);
                HashSet<AstarNode> closedSet = new HashSet<AstarNode>();
                openSet.Add(startNode);

                while (openSet.Count > 0)
                {
                    AstarNode currentNode = openSet.RemoveFirst();
                    closedSet.Add(currentNode);

                    if (currentNode == targetNode)
                    {
                        sw.Stop();
                        print("Path found: " + sw.ElapsedMilliseconds + "ms");
                        pathSucess = true;
                        break;
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
                            neighbour.Parent = currentNode;

                            if (!openSet.Contains(neighbour))
                            {
                                openSet.Add(neighbour);
                            }
                        }
                    }
                }
            }
            yield return null;

            if (pathSucess)
            {
                waypoints = RetracePath(startNode, targetNode);
            }
            requestManager.FinishedProcessingPath(waypoints, pathSucess);
        }


        private Vector3[] RetracePath(AstarNode startNode, AstarNode endNode)
        {
            List<AstarNode> path = new List<AstarNode>();
            AstarNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }
            Vector3[] waypoints = SimplifyPath(path);
            Array.Reverse(waypoints);
            return waypoints;

        }

        private Vector3[] SimplifyPath(List<AstarNode> path)
        {
            List<Vector3> waypoints = new List<Vector3>();
            Vector3 directionOld = Vector3.zero;

            for (int i = 1; i < path.Count; i++)
            {
                Vector3 directionNew =
                new Vector3(
                    path[i - 1].GridX - path[i].GridX,
                    path[i - 1].GridY - path[i].GridY,
                    path[i - 1].GridZ - path[i].GridZ
                );

                if(directionNew != directionOld)
                {
                    waypoints.Add(path[i].WorldPosition);
                }
                directionOld = directionNew;
            }
            return waypoints.ToArray();
        }

        public int GetDistance(AstarNode nodeA, AstarNode nodeB)
        {
            int dx = (int)Math.Abs(nodeA.GridX - nodeB.GridX);
            int dy = (int)Math.Abs(nodeA.GridY - nodeB.GridY);
            int dz = (int)Math.Abs(nodeA.GridZ - nodeB.GridZ);

            // make (dx, dy, dz) to (dx > dy > dz)
            if (dx < dy) Swap(ref dx, ref dy);
            if (dx < dz) Swap(ref dz, ref dz);
            if (dy < dz) Swap(ref dy, ref dz);

            // sqrt(3) = 1.7xxx, sqrt(2) = 1.4xxx
            return 17 * dz + 14 * (dy - dz) + 10 * (dx - dy - dz);
        }

        private void Swap(ref int num1, ref int num2)
        {
            int temp = num1;
            num1 = num2;
            num2 = temp;
        }
    }
}