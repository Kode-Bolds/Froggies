using Kodebolds.Core;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Froggies
{
    public class GridManager : KodeboldBehaviour
    {
        //TODO: Pathfinding - add 3rd dimension?
        public NativeArray2D<MapNode> Grid;
        [SerializeField] public int m_cellSize;
        [SerializeField] public int2 m_gridSize;

        public override void GetBehaviourDependencies(Dependencies dependencies)
        {
        }

        public override void InitBehaviour()
        {
            Grid = new NativeArray2D<MapNode>(m_gridSize.x, m_gridSize.y, Allocator.Persistent);

            for (int x = 0; x < m_gridSize.x; x++)
            {
                for (int z = 0; z < m_gridSize.y; z++)
                {
                    Grid[x, z] = new MapNode
                    {
                        position = (float3)transform.position + new float3(x * m_cellSize, 0, z * m_cellSize),
                        gridPosition = new int2(x, z)
                    };
                }
            }
        }

        public override void UpdateBehaviour()
        {
        }

        public override void FreeBehaviour()
        {
            Grid.Dispose();
        }
    }
}