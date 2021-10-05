using UnityEngine;


namespace Dronai.Path
{
    public class AstarNode : IHeapItem<AstarNode>
    {
        public bool Walkable = default;
        public Vector3 WorldPosition = default;
        public int GridX, GridY, GridZ;
        public AstarNode Parent = default;

        private int heapIndex = default;
        public int HeapIndex
        {
            get
            {
                return heapIndex;
            }
            set
            {
                heapIndex = value;
            }
        }

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
        
        public int CompareTo(AstarNode nodeToCompare)
        {
            int compare = fCost.CompareTo(nodeToCompare.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(nodeToCompare.hCost);
            }
            return -compare;
        }
    }
}
