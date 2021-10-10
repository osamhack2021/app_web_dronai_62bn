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

        /// <summary>
        /// 베이크 맵을 최신화 합니다
        /// </summary>
        public void UpdateGrid()
        {
            grid.UpdateGrid();
        }

        public void StartFindPath(Vector3 startPos, Vector3 targetPos, bool history)
        {
            StartCoroutine(FindPath(startPos, targetPos, history));
        }
        private IEnumerator FindPath(Vector3 startPos, Vector3 targetPos, bool history)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Vector3[] waypoints = new Vector3[0];
            bool pathSucess = false;

            AstarNode startNode = grid.NodeFromWorldPoint(startPos);
            AstarNode targetNode = grid.NodeFromWorldPoint(targetPos);
            startNode.Parent = startNode;


            if (targetNode.Walkable)
            {
                Heap<AstarNode> openSet = new Heap<AstarNode>(grid.MaxSize);
                HashSet<AstarNode> closedSet = new HashSet<AstarNode>();
                openSet.Add(startNode);

                while (openSet.Count > 0)
                {
                    AstarNode currentNode = openSet.RemoveFirst();
                    // print("world : " + currentNode.WorldPosition);

                    closedSet.Add(currentNode);

                    if (currentNode == targetNode)
                    {
                        sw.Stop();
                        print("[A* Dyanamic] 경로 발견 : " + sw.ElapsedMilliseconds + "ms");
                        pathSucess = true;
                        break;
                    }

                    foreach (AstarNode neighbour in grid.GetNeighbours(currentNode))
                    {
                        if (!neighbour.Walkable || closedSet.Contains(neighbour))
                        {
                            continue;
                        }

                        int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.MovementPenalty;
                        if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                        {
                            neighbour.gCost = newMovementCostToNeighbour;
                            neighbour.hCost = GetDistance(neighbour, targetNode);
                            neighbour.Parent = currentNode;

                            if (!openSet.Contains(neighbour))
                            {
                                openSet.Add(neighbour);
                            }
                            else
                            {
                                openSet.UpdateItem(neighbour);
                            }
                        }
                    }
                }
            }
            yield return null;

            if (pathSucess)
            {
                waypoints = RetracePath(startNode, targetNode, history);
            }
            else
            {
                print("[A* Dyanamic] 경로 탐색 실패, 가능한 경로가 없습니다!");
            }
            requestManager.FinishedProcessingPath(waypoints, pathSucess);
        }

        private Vector3[] RetracePath(AstarNode startNode, AstarNode endNode, bool history = false)
        {
            List<AstarNode> path = new List<AstarNode>();
            AstarNode currentNode = endNode;
            while (currentNode != startNode)
            {
                if(history) currentNode.Walkable = false;
                path.Add(currentNode);
                currentNode = currentNode.Parent;
            }
            List<Vector3> waypoints = SimplifyPath(path);
            waypoints.Reverse();
            return waypoints.ToArray();
        }

        private List<Vector3> SimplifyPath(List<AstarNode> path)
        {
            List<Vector3> waypoints = new List<Vector3>();
            Vector3 directionOld = Vector3.zero;

            waypoints.Add(path[0].WorldPosition);
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 directionNew = new Vector3(path[i - 1].GridX - path[i].GridX, path[i - 1].GridY - path[i].GridY, path[i - 1].GridZ - path[i].GridZ);
                if (directionNew != directionOld)
                {
                    waypoints.Add(path[i].WorldPosition);
                }
                directionOld = directionNew;
            }
            return waypoints;
        }

        public int GetDistance(AstarNode nodeA, AstarNode nodeB)
        {
            int dx = (int)Math.Abs(nodeA.GridX - nodeB.GridX);
            int dy = (int)Math.Abs(nodeA.GridY - nodeB.GridY);
            int dz = (int)Math.Abs(nodeA.GridZ - nodeB.GridZ);

            // make (dx, dy, dz) to (dx > dy > dz)
            if (dx < dy) Swap(ref dx, ref dy);
            if (dx < dz) Swap(ref dx, ref dz);
            if (dy < dz) Swap(ref dy, ref dz);

            // sqrt(3) = 1.7xxx, sqrt(2) = 1.4xxx
            // dz, dy - dz, (dx - dz) - (dy - dz)
            return 17 * dz + 14 * (dy - dz) + 10 * (dx - dy);
        }

        private void Swap(ref int num1, ref int num2)
        {
            int temp = num1;
            num1 = num2;
            num2 = temp;
        }
    }
}