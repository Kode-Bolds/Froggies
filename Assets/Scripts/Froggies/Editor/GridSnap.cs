using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Froggies;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Froggies.EditorScripts
{
    public class GridSnap : MonoBehaviour
    {
        public int2 size;
        private List<int2> occupied = new List<int2>();

        public void SnapToGrid()
        {
            if (MapAuthoringEditor.map == null)
                return;
            
            //Calculate position of top left corner based on size
            float3 offset = new float3((float)size.x / 2, 0,  (float)size.y / 2) * MapAuthoringEditor.map.cellSize;

            float3 pos = (float3) transform.position + offset;

            //Find nearest node to corner position and snap the left corner to it
            int2 gridPos = MapUtils.FindNearestNode(pos, MapAuthoringEditor.map.grid);
            MapNode posNode = MapAuthoringEditor.map.grid[gridPos.x, gridPos.y];
            transform.position = posNode.position - offset;

            //unoccupy previously occupied squares
            for (int i = 0; i < occupied.Count; ++i)
            {
                int2 index = occupied[i];
                MapAuthoringEditor.map.grid[index.x, index.y].occupiedBy = OccupiedBy.Nothing;
            }
            
            //occupy gridSquares
            if (TryGetComponent<ObstacleAuthoringComponent>(out _))
            {
                for (int i = 0; i < size.x; ++i)
                {
                    for (int j = 0; j < size.y; ++j)
                    {
                        MapAuthoringEditor.map.grid[gridPos.x, gridPos.y].occupiedBy = OccupiedBy.Environment;
                    }
                }
                occupied.Add(gridPos);
            }
        }
    }
}