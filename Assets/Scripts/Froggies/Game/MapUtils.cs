using System.Collections;
using System.Collections.Generic;
using Kodebolds.Core;
using Unity.Mathematics;
using UnityEngine;

namespace Froggies
{
    public static class MapUtils
    {
        public static int2 FindNearestNode(float3 pos, NativeArray2D<MapNode> grid)
        {
            if (!grid.IsCreated)
                Debug.Assert(false);

            //TODO: Can we do a binary search here?
            int2 closestNode = default;
            float closestDistSq = float.MaxValue;

            for (int i = 0; i < grid.Length0; ++i)
            {
                for (int j = 0; j < grid.Length1; ++j)
                {
                    float distanceSq = math.distancesq(grid[i, j].position, pos);
                    if (distanceSq < closestDistSq)
                    {
                        closestNode = new int2(i, j);
                        closestDistSq = distanceSq;
                    }
                }
            }

            return closestNode;
        }
        
        public static int2 FindNearestNode(float3 pos, MapNode[,] grid)
        {
            //TODO: Can we do a binary search here?
            int2 closestNode = default;
            float closestDistSq = float.MaxValue;

            for (int i = 0; i < grid.GetLength(0); ++i)
            {
                for (int j = 0; j < grid.GetLength(1); ++j)
                {
                    float distanceSq = math.distancesq(grid[i, j].position, pos);
                    if (distanceSq < closestDistSq)
                    {
                        closestNode = new int2(i, j);
                        closestDistSq = distanceSq;
                    }
                }
            }

            return closestNode;
        }
    }
}