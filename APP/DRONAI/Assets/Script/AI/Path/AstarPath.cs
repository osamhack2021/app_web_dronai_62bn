using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dronai.Path
{
    public class AstarPath
    {
        public readonly Vector3[] LookPoints;
        public readonly AstarLine[] TurnBoundaries;
        public readonly int finishLineIndex;

        public AstarPath(Vector3[] waypoints, Vector3 startPos, float turnDst)
        {
            LookPoints = waypoints;
            TurnBoundaries = new AstarLine[LookPoints.Length];
            finishLineIndex = TurnBoundaries.Length - 1;

            Vector3 previousPoint = startPos;
            for (int i = 0; i < LookPoints.Length; i++)
            {
                Vector3 currentPoint = LookPoints[i];
                Vector3 dirToCurrentPoint = (currentPoint - previousPoint).normalized;
                Vector3 turnBoundaryPoint = (i == finishLineIndex) ? currentPoint : currentPoint - dirToCurrentPoint * turnDst;
                TurnBoundaries[i] = new AstarLine(turnBoundaryPoint, previousPoint - dirToCurrentPoint * turnDst);
                previousPoint = turnBoundaryPoint;
            }
        }

        public void DrawWithGizmos()
        {

            Gizmos.color = Color.black;
            foreach (Vector3 p in LookPoints)
            {
                Gizmos.DrawSphere(p, 0.2f);
            }

            Gizmos.color = Color.white;
            foreach (AstarLine l in TurnBoundaries)
            {
                l.DrawWithGizmos(2);
            }
        }
    }
}