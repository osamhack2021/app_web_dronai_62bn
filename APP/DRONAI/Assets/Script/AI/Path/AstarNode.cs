using UnityEngine;


namespace Dronai.Path
{
    public class AstarNode : IHeapItem<AstarNode>
    {
        public bool Walkable = default;
        public Vector3 WorldPosition = default;
        public int GridX, GridY, GridZ;
        public int MovementPenalty = default;

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
        public AstarNode Parent = default;


        public AstarNode(bool walkable, Vector3 worldPosition, int gridX, int gridY, int gridZ, int penalty)
        {
            Walkable = walkable;
            WorldPosition = worldPosition;

            GridX = gridX;
            GridY = gridY;
            GridZ = gridZ;

            MovementPenalty = penalty;
        }

        public void UpdateNode(bool walkable, Vector3 worldPosition, int gridX, int gridY, int gridZ, int penalty)
        {
            Walkable = walkable;
            WorldPosition = worldPosition;

            GridX = gridX;
            GridY = gridY;
            GridZ = gridZ;

            MovementPenalty = penalty;
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
