using Unity.Mathematics;

namespace Froggies
{
    [System.Serializable]
    public unsafe struct MapNode
    {
        public MapNode* parent;
        public MapNode* child;
        public float3 position;
        public int2 gridPosition;
        public OccupiedBy occupiedBy;
        public NodeState state;
        public float g; //Total length of path from start node to this node.
        public float h; //Distance from node to target node.
        public float f; //Estimated total distance traveled if taking this node in the path.

        public void SetParent(MapNode* newParent)
        {
            if (newParent == null)
                return;

            parent = newParent;
            g = newParent->g + math.distancesq(position, newParent->position);
            f = g + h;
        }
    }

    public enum NodeState
    {
        Untested, //Not yet tested for current path.
        Closed, //Tested and eliminated from consideration, or already added to path.
        Open //Tested but still open for consideration for path.
    }

    public enum OccupiedBy
    {
        Nothing = 0,
        Unit = 1,
        Building = 2,
        Environment = 3
    }

}