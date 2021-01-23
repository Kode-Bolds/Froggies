using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    public class PathFindingSystem : KodeboldJobSystem
    {
        private GridManager m_gridManager;
        private static NativeArray2D<MapNode> m_grid;

        private float recalculatePeriod = 1f;

        private EntityQuery m_pathFindingQuery;

        public override void GetSystemDependencies(Dependencies dependencies)
        {
            m_gridManager = dependencies.GetDependency<GridManager>();
        }

        public override void InitSystem()
        {
            m_grid = m_gridManager.Grid;
            m_pathFindingQuery = GetEntityQuery(ComponentType.ReadWrite<PathFinding>(),
                ComponentType.ReadOnly<CurrentTarget>(), ComponentType.ReadOnly<Translation>());
        }

        public override void UpdateSystem()
        {
            //We use IJobChunk so that we can share a grid between all pathfinders in a chunk
            Dependency = new PathFindingJob
            {
                pathFindingComponentHandle = GetComponentTypeHandle<PathFinding>(),
                currentTargetComponentHandle = GetComponentTypeHandle<CurrentTarget>(true),
                gridRef = m_grid
            }.ScheduleParallel(m_pathFindingQuery, Dependency);
        }

        [BurstCompile]
        private unsafe struct PathFindingJob : IJobChunk
        {
            public ComponentTypeHandle<PathFinding> pathFindingComponentHandle;
            [ReadOnly] public ComponentTypeHandle<CurrentTarget> currentTargetComponentHandle;

            [ReadOnly] public NativeArray2D<MapNode> gridRef;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                NativeArray<PathFinding> pathfindingArray = chunk.GetNativeArray(pathFindingComponentHandle);
                NativeArray<CurrentTarget> currentTargetArray = chunk.GetNativeArray(currentTargetComponentHandle);

                NativeArray2D<MapNode> gridCopy = new NativeArray2D<MapNode>(gridRef.Columns, gridRef.Rows,
                    Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                UnsafeUtility.MemCpy(gridCopy.GetUnsafePtr(), gridRef.GetUnsafePtr(),
                    gridRef.Length * sizeof(MapNode));

                for (int i = 0; i < chunk.Count; ++i)
                {
                    PathFinding pathfinding = pathfindingArray[0];
                    CurrentTarget currentTarget = currentTargetArray[0];

                    if (!pathfinding.requestedPath)
                        return;

                    //Calculate closest node to targetpos
                    pathfinding.targetNode = FindNearestNode(currentTarget.targetData.targetPos);

                    //Set h values of grid
                    CalculateGridH(gridCopy, ref pathfinding);

                    bool pathfound = SearchForPath(ref pathfinding, gridCopy);

                    //Reverse path
                    if (pathfound)
                    {
                        MapNode node = gridCopy[pathfinding.currentNode.x, pathfinding.currentNode.y];
                        while (node.child!= null)
                        {
                            pathfinding.path.Add(new PathNode{ position = node.position});
                            node = *node.child;
                        }

                        pathfinding.currentIndexOnPath = 0;
                        pathfinding.hasPath = true;
                        pathfinding.requestedPath = false;
                    }
                
                    pathfindingArray[0] = pathfinding;
                }
            }
        }

        private struct FCompare : IComparer<MapNode>
        {
            public int Compare(MapNode node1, MapNode node2)
            {
                return node1.f.CompareTo(node2.f);
            }
        }

        private static unsafe bool SearchForPath(ref PathFinding kPathfinding, NativeArray2D<MapNode> gridCopy)
        {
            //Get pointer into flat array
            MapNode* currentNode = (MapNode*) gridCopy.GetUnsafePtr();
            currentNode += kPathfinding.currentNode.x + kPathfinding.currentNode.y * gridCopy.Columns;
        
            //Get passable neighbours
            NativeArray<MapNode> passableNeighbours = GetPassableNeighbours(ref kPathfinding, gridCopy, currentNode);
        
            FCompare fCompare = new FCompare();
            passableNeighbours.Sort(fCompare);

            currentNode->state = NodeState.Closed;
            for (int i = 0; i < passableNeighbours.Length; ++i)
            {
                MapNode neighbour = passableNeighbours[i];
                //WE HAVE FOUND THE TARGET
                if (neighbour.gridPosition.Equals(kPathfinding.targetNode))
                {
                    return true;
                }
            
                //recurse
                if (SearchForPath(ref kPathfinding, gridCopy))
                {
                    return true;
                }
            }
        
            return false;
        }

        private static unsafe void CalculateGridH(NativeArray2D<MapNode> gridCopy, ref PathFinding kPathfinding)
        {
            //Fill in h values for grid
            for (int i = 0; i < gridCopy.Columns; ++i)
            {
                for (int j = 0; j < gridCopy.Rows; ++j)
                {
                    MapNode node = gridCopy[i, j];
                    if (i != kPathfinding.currentNode.x && j != kPathfinding.currentNode.y)
                    {
                        node.h = math.distancesq(gridCopy[i, j].position,
                            gridCopy[kPathfinding.targetNode.x, kPathfinding.targetNode.y].position);
                    }

                    node.parent = null;
                    node.child = null;
                    node.state = NodeState.Untested;

                    gridCopy[i, j] = node;
                }
            }
        }

        public static int2 FindNearestNode(float3 pos)
        {
            if (!m_grid.IsCreated)
                Debug.Assert(false);

            //Can we do a binary search here?
            int2 closestNode = default;
            float closestDistSq = float.MaxValue;

            for (int i = 0; i < m_grid.Columns; ++i)
            {
                for (int j = 0; j < m_grid.Rows; ++j)
                {
                    if (math.distancesq(m_grid[i, j].position, pos) < closestDistSq)
                    {
                        closestNode = new int2(i, j);
                    }
                }
            }

            return closestNode;
        }

        private static unsafe NativeArray<MapNode> GetPassableNeighbours(ref PathFinding kPathfinding,
            NativeArray2D<MapNode> gridCopy, MapNode* currentNode)
        {
            NativeArray<MapNode> neighbours = new NativeArray<MapNode>(8, Allocator.TempJob)
            {
                [0] = gridCopy[kPathfinding.currentNode.x - 1, kPathfinding.currentNode.y - 1],
                [1] = gridCopy[kPathfinding.currentNode.x - 1, kPathfinding.currentNode.y],
                [2] = gridCopy[kPathfinding.currentNode.x - 1, kPathfinding.currentNode.y + 1],
                [3] = gridCopy[kPathfinding.currentNode.x, kPathfinding.currentNode.y - 1],
                [4] = gridCopy[kPathfinding.currentNode.x, kPathfinding.currentNode.y + 1],
                [5] = gridCopy[kPathfinding.currentNode.x + 1, kPathfinding.currentNode.y - 1],
                [6] = gridCopy[kPathfinding.currentNode.x + 1, kPathfinding.currentNode.y],
                [7] = gridCopy[kPathfinding.currentNode.x + 1, kPathfinding.currentNode.y + 1]
            };

            NativeList<MapNode> activeNeighbours = new NativeList<MapNode>(8, Allocator.TempJob);

            for (int i = 0; i < neighbours.Length; ++i)
            {
                MapNode neighbour = neighbours[i];
                if (neighbour.occupiedBy != OccupiedBy.Nothing || neighbour.state == NodeState.Closed)
                    continue;

                float gDistance = math.distancesq(neighbour.position, neighbour.parent->position);
                float tempGDistance = currentNode->g + gDistance;

                if ((neighbour.state == NodeState.Open && tempGDistance < neighbour.g)
                    || neighbour.state != NodeState.Open)
                { 
                    MapNode* realNeighbour = (MapNode*) gridCopy.GetUnsafePtr(); 
                    realNeighbour += neighbour.gridPosition.x + neighbour.gridPosition.y * gridCopy.Columns;
                    currentNode->child = realNeighbour;
                    neighbour.parent = currentNode;
                    neighbour.parent->g += gDistance;
                    activeNeighbours.Add(neighbour);
                }
            }

            return activeNeighbours;
        }

        public override void FreeSystem()
        {
        }
    }
}