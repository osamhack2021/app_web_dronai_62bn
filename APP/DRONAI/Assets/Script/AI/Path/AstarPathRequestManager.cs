using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;


namespace Dronai.Path
{
    public class AstarPathRequestManager : Singleton<AstarPathRequestManager>
    {

        private Queue<PathResult> results = new Queue<PathResult>();
        private AstarPathFinding pathfinding;

        private void Awake()
        {
            pathfinding = GetComponent<AstarPathFinding>();
        }
        private void Update()
        {
            if (results.Count > 0)
            {
                int itemsInQueue = results.Count;
                lock (results)
                {
                    for (int i = 0; i < itemsInQueue; i++)
                    {
                        PathResult result = results.Dequeue();
                        result.Callback(result.Path, result.Success);
                    }
                }
            }
        }
        public static void RequestUpdateGrid()
        {
            Instance.UpdateGrid();
        }
        public void UpdateGrid()
        {
            pathfinding.UpdateGrid();
        }
        public static void RequestPath(PathRequest request)
        {
            ThreadStart threadStart = delegate
            {
                Instance.pathfinding.FindPath(request, Instance.FinishedProcessingPath);
            };
            threadStart.Invoke();
        }
        public void FinishedProcessingPath(PathResult result)
        {
            lock (results)
            {
                results.Enqueue(result);
            }
        }
    }
    public struct PathResult
    {
        public Vector3[] Path;
        public bool Success;
        public Action<Vector3[], bool> Callback;

        public PathResult(Vector3[] path, bool success, Action<Vector3[], bool> callback)
        {
            Path = path;
            Success = success;
            Callback = callback;
        }
    }
    public struct PathRequest
    {
        public Vector3 PathStart;
        public Vector3 PathEnd;
        public bool History;
        public Action<Vector3[], bool> Callback;


        public PathRequest(Vector3 _start, Vector3 _end, bool history, Action<Vector3[], bool> _callback)
        {
            PathStart = _start;
            PathEnd = _end;
            History = history;
            Callback = _callback;
        }
    }
}