using System;
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

        protected override GameState ActiveGameState => GameState.Updating;

#if UNITY_EDITOR
		private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i <= m_gridSize.x; i++)
            {
                Vector3 start = transform.position + new Vector3(m_cellSize * i, 0, 0);
                Vector3 end = start + new Vector3(0, 0, m_gridSize.y * m_cellSize);

                Gizmos.DrawLine(start, end);
            }

            for (int i = 0; i <= m_gridSize.y; i++)
            {
                Vector3 start = transform.position + new Vector3(0, 0, m_cellSize * i);
                Vector3 end = start + new Vector3(m_gridSize.x * m_cellSize, 0, 0);

                Gizmos.DrawLine(start, end);
            }
        }
#endif

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