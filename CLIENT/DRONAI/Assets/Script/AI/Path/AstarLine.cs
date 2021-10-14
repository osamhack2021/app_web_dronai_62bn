using UnityEngine;


namespace Dronai.Path
{
    public struct AstarLine
    {
        private const float verticalLineGradient = 1e5f;

        private float gradient;
        private float z_intercept;
        private Vector3 pointOnLine_1;
        private Vector3 pointOnLine_2;
        private float gradientPerpendicular;
        private bool approachSide;

        public AstarLine(Vector3 pointOnLine, Vector3 pointPerpendicularToLine)
        {
            float dx = pointOnLine.x - pointPerpendicularToLine.x;
            float dz = pointOnLine.z - pointPerpendicularToLine.z;

            if (dx == 0)
            {
                gradientPerpendicular = verticalLineGradient;
            }
            else
            {
                gradientPerpendicular = dz / dx;
            }

            if (gradientPerpendicular == 0)
            {
                gradient = verticalLineGradient;
            }
            else
            {
                gradient = -1 / gradientPerpendicular;
            }


            z_intercept = pointOnLine.z - gradient * pointOnLine.x;
            pointOnLine_1 = pointOnLine;
            pointOnLine_2 = pointOnLine + new Vector3(1, gradient, 0);

            approachSide = false;
            approachSide = GetSide(pointPerpendicularToLine);
        }

        private bool GetSide(Vector3 p)
        {
            return (p.x - pointOnLine_1.x) * (pointOnLine_2.z - pointOnLine_1.z) > (p.z - pointOnLine_1.z) * (pointOnLine_2.x - pointOnLine_1.x);
        }

        public bool HasCrossedLine(Vector3 p)
        {
            return GetSide(p) != approachSide;
        }
        public void DrawWithGizmos(float length)
        {
            Vector3 lineDir = new Vector3(1, 0, gradient).normalized;
            Vector3 lineCentre = new Vector3(pointOnLine_1.x, pointOnLine_1.y, pointOnLine_1.z);
            Gizmos.DrawLine(lineCentre - lineDir * length / 2f, lineCentre + lineDir * length / 2f);
        }
    }
}