using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Dronai.Path
{
    public class AstarPathRequestManager : Singleton<AstarPathRequestManager>
    {
        struct PathRequest
        {
            public Vector3 PathStart;
            public Vector3 PathEnd;
            public Action<Vector3[], bool> Callback;

            public PathRequest(Vector3 start, Vector3 end, Action<Vector3[], bool> callback)
            {
                PathStart = start;
                PathEnd = end;
                Callback = callback;
            }
        }

        private Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
        private PathRequest currentPathRequest = default;

        private AstarPathFinding pathFinding;
        
        bool isProcessingPath;

        private void Awake() {
            pathFinding = GetComponent<AstarPathFinding>();
        }

        public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback)
        {
            PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback);
            Instance.pathRequestQueue.Enqueue(newRequest);
            Instance.TryProcessNext();
        }

        private void TryProcessNext()
        {
            if(!isProcessingPath && pathRequestQueue.Count > 0)
            {
                currentPathRequest = pathRequestQueue.Dequeue();
                isProcessingPath = true;
                pathFinding.StartFindPath(currentPathRequest.PathStart, currentPathRequest.PathEnd);
            }
        }

        public void FinishedProcessingPath(Vector3[] path, bool success)
        {
            currentPathRequest.Callback(path, success);
            isProcessingPath = false;
            TryProcessNext();
        }
    }

}