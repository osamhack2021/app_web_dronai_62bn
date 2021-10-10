using System;
using System.Collections.Generic;
using UnityEngine;


namespace Dronai.Path
{
    public class AstarPathRequestManager : Singleton<AstarPathRequestManager>
    {

        private Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
        private PathRequest currentPathRequest;

        private AstarPathFinding pathfinding;

        private bool isProcessingPath;

        private void Awake()
        {
            pathfinding = GetComponent<AstarPathFinding>();
        }

        public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, bool history, Action<Vector3[], bool> callback)
        {
            PathRequest newRequest = new PathRequest(pathStart, pathEnd, history, callback);
            Instance.pathRequestQueue.Enqueue(newRequest);
            Instance.TryProcessNext();
        }

        public static void RequestUpdateGrid()
        {
            Instance.UpdateGrid();
        }

        public void UpdateGrid()
        {
            pathfinding.UpdateGrid();
        }

        void TryProcessNext()
        {
            if (!isProcessingPath && pathRequestQueue.Count > 0)
            {
                currentPathRequest = pathRequestQueue.Dequeue();
                isProcessingPath = true;
                if (!currentPathRequest.History) UpdateGrid();
                pathfinding.StartFindPath(currentPathRequest.PathStart, currentPathRequest.PathEnd, currentPathRequest.History);
            }
        }

        public void FinishedProcessingPath(Vector3[] path, bool success)
        {
            currentPathRequest.Callback(path, success);
            isProcessingPath = false;
            TryProcessNext();
        }

        struct PathRequest
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
}