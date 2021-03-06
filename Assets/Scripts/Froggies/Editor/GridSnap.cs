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
            
            //Calculate position of bottom left corner based on size
            float3 halfSize = new float3((float)size.x / 2, 0,  (float)size.y / 2) * MapAuthoringEditor.map.cellSize;

            float3 bottomLeft = (float3) transform.position - halfSize;

            //Find nearest node to corner position and snap the left corner to it
            int2 bottomLeftSnappedNode = MapUtils.FindNearestNode(bottomLeft, MapAuthoringEditor.map);
            MapNode bottomLeftSnappedPos = MapAuthoringEditor.map.grid[bottomLeftSnappedNode.y * MapAuthoringEditor.map.gridSize.x + bottomLeftSnappedNode.x];
            //snap to position, remember to add back half our size to ensure the bottom left snaps to the given point instead of the center
            //Also add half a cell to reallign 
            float3 halfCell = new float3((float)MapAuthoringEditor.map.cellSize / 2, 0, (float)MapAuthoringEditor.map.cellSize / 2);
            transform.position = bottomLeftSnappedPos.position + halfSize - halfCell + new float3(0.1f, 0, 0.1f);

            //unoccupy previously occupied squares
            for (int i = 0; i < occupied.Count; ++i)
            {
                int2 index = occupied[i];
                MapAuthoringEditor.map.grid[index.y * MapAuthoringEditor.map.gridSize.x + index.x].occupiedBy = OccupiedBy.Nothing;
            }
            
            //occupy gridSquares
            if (TryGetComponent<ObstacleAuthoringComponent>(out _))
            {
                for (int i = 0; i < size.x; ++i)
                {
                    for (int j = 0; j < size.y; ++j)
                    {
                        int2 occupiedNode = bottomLeftSnappedNode + new int2(i, j);
                        MapAuthoringEditor.map.grid[occupiedNode.y * MapAuthoringEditor.map.gridSize.x + occupiedNode.x].occupiedBy = OccupiedBy.Environment;
                        occupied.Add(occupiedNode);
                    }
                }
            }
        }
    }
}