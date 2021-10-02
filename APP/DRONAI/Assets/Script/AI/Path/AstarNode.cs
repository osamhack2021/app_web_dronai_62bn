using UnityEngine;


namespace Dronai.Path
{
    public class AstarNode
    {
        public bool Walkable = default;
        public Vector3 WorldPosition = default;
        public int GridX, GridY, GridZ;
        public AstarNode parent;

        public int gCost;
        public int hCost;
        public int fCost
        {
            get
            {
                return gCost + hCost;
            }
        }

        public AstarNode(bool walkable, Vector3 worldPosition, int gridX, int gridY, int gridZ)
        {
            Walkable = walkable;
            WorldPosition = worldPosition;

            GridX = gridX;
            GridY = gridY;
            GridZ = gridZ;
        }
    }
}
