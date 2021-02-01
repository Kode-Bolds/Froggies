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
                ComponentType.ReadWrite<PathNode>(),
                ComponentType.ReadOnly<CurrentTarget>());
        }

        public override void UpdateSystem()
        {
            //We use IJobChunk so that we can share a grid between all pathfinders in a chunk
            Dependency = new PathFindingJob
            {
                pathFindingComponentHandle = GetComponentTypeHandle<PathFinding>(),
                pathNodeBufferHandle = GetBufferTypeHandle<PathNode>(),
                currentTargetComponentHandle = GetComponentTypeHandle<CurrentTarget>(true),
                gridRef = m_grid
            }.ScheduleParallel(m_pathFindingQuery, Dependency);
        }

        [BurstCompile]
        private unsafe struct PathFindingJob : IJobChunk
        {
            public ComponentTypeHandle<PathFinding> pathFindingComponentHandle;
            public BufferTypeHandle<PathNode> pathNodeBufferHandle;
            [ReadOnly] public ComponentTypeHandle<CurrentTarget> currentTargetComponentHandle;

            [ReadOnly] public NativeArray2D<MapNode> gridRef;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                NativeArray<PathFinding> pathfindingArray = chunk.GetNativeArray(pathFindingComponentHandle);
                BufferAccessor<PathNode> pathNodeBufferAccessor = chunk.GetBufferAccessor(pathNodeBufferHandle);
                NativeArray<CurrentTarget> currentTargetArray = chunk.GetNativeArray(currentTargetComponentHandle);

                NativeArray2D<MapNode> gridCopy = new NativeArray2D<MapNode>(gridRef.Length0, gridRef.Length1,
                    Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                UnsafeUtility.MemCpy(gridCopy.GetUnsafePtr(), gridRef.GetUnsafePtrReadOnly(),
                    gridRef.Length * sizeof(MapNode));

                for (int i = 0; i < chunk.Count; ++i)
                {
                    PathFinding pathfinding = pathfindingArray[i];
                    CurrentTarget currentTarget = currentTargetArray[i];
                    DynamicBuffer<PathNode> path = pathNodeBufferAccessor[i];

                    if (!pathfinding.requestedPath)
                        continue;
                    
                    path.Clear();
                    pathfinding.currentIndexOnPath = 0;

                    //Calculate closest node to targetpos
                    pathfinding.targetNode = FindNearestNode(currentTarget.targetData.targetPos);
                    if (pathfinding.targetNode.Equals(pathfinding.currentNode))
                    {
                        pathfinding.requestedPath = false;
                        continue;
                    }

                    //Set h values of grid
                    CalculateGridH(gridCopy, ref pathfinding);

                    bool pathfound = SearchForPath(pathfinding.currentNode, pathfinding.targetNode, gridCopy);

                    //Reverse path
                    if (pathfound)
                    {
                        MapNode node = gridCopy[pathfinding.currentNode.x, pathfinding.currentNode.y];
                        while (node.child != null)
                        {
                            path.Add(new PathNode {position = node.position, gridPosition = node.gridPosition});
                            node = *node.child;
                        }  
                        path.Add(new PathNode {position = node.position, gridPosition = node.gridPosition});

                        pathfinding.hasPath = true;
                        pathfinding.requestedPath = false;
                    }

                    pathfindingArray[i] = pathfinding;
                }

                gridCopy.Dispose();
            }
        }

        private struct NodeCompare : IComparer<MapNode>
        {
            public int Compare(MapNode node1, MapNode node2)
            {
                int fCompare = node1.f.CompareTo(node2.f);
                if (fCompare != 0)
                    return fCompare;

                return node1.h.CompareTo(node2.h);
            }
        }

        private static unsafe bool SearchForPath(int2 currentNode, int2 targetNode, NativeArray2D<MapNode> gridCopy)
        {
            //Get pointer into flat array
            MapNode* currentNodePtr = gridCopy.GetPointerToElement(currentNode.x, currentNode.y);

            //Get passable neighbours
            NativeList<MapNode> passableNeighbours = GetPassableNeighbours(currentNode, gridCopy, currentNodePtr);

            NodeCompare nodeCompare = new NodeCompare();
            passableNeighbours.Sort(nodeCompare);

            currentNodePtr->state = NodeState.Closed;
            for (int i = 0; i < passableNeighbours.Length; ++i)
            {
                MapNode neighbour = passableNeighbours[i];
                
                currentNodePtr->child =
                    gridCopy.GetPointerToElement(neighbour.gridPosition.x, neighbour.gridPosition.y);
                
                //WE HAVE FOUND THE TARGET
                if (neighbour.gridPosition.Equals(targetNode))
                {
                    passableNeighbours.Dispose();
                    return true;
                }

                //recurse
                if (SearchForPath(neighbour.gridPosition, targetNode, gridCopy))
                {
                    passableNeighbours.Dispose();
                    return true;
                }
            }

            passableNeighbours.Dispose();
            return false;
        }

        private static unsafe void CalculateGridH(NativeArray2D<MapNode> gridCopy, ref PathFinding kPathfinding)
        {
            //Fill in h values for grid
            for (int i = 0; i < gridCopy.Length0; ++i)
            {
                for (int j = 0; j < gridCopy.Length1; ++j)
                {
                    MapNode node = gridCopy[i, j];
                    node.h = math.distancesq(gridCopy[i, j].position,
                        gridCopy[kPathfinding.targetNode.x, kPathfinding.targetNode.y].position);

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

            for (int i = 0; i < m_grid.Length0; ++i)
            {
                for (int j = 0; j < m_grid.Length1; ++j)
                {
                    float distanceSq = math.distancesq(m_grid[i, j].position, pos);
                    if (distanceSq < closestDistSq)
                    {
                        closestNode = new int2(i, j);
                        closestDistSq = distanceSq;
                    }
                }
            }

            return closestNode;
        }

        private static unsafe NativeList<MapNode> GetPassableNeighbours(int2 currentNode,
            NativeArray2D<MapNode> gridCopy, MapNode* currentNodePtr)
        {
            NativeList<MapNode> neighbours = new NativeList<MapNode>(8, Allocator.Temp);

            bool CheckXBounds(int x)
            {
                return (x < gridCopy.Length0 && x >= 0);
            }

            bool CheckYBounds(int y)
            {
                return (y < gridCopy.Length1 && y >= 0);
            }

            for (int x = -1; x <= 1; ++x)
            {
                int xIndex = currentNode.x + x;

                if (!CheckXBounds(xIndex))
                    continue;

                for (int y = -1; y <= 1; ++y)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int yIndex = currentNode.y + y;

                    if (!CheckYBounds(yIndex))
                        continue;

                    neighbours.Add(gridCopy[xIndex, yIndex]);
                }
            }

            NativeList<MapNode> activeNeighbours = new NativeList<MapNode>(8, Allocator.Temp);

            for (int i = 0; i < neighbours.Length; ++i)
            {
                MapNode neighbour = neighbours[i];
                if (neighbour.occupiedBy != OccupiedBy.Nothing || neighbour.state == NodeState.Closed)
                    continue;

                if (neighbour.state == NodeState.Open)
                {
                    float gDistance = math.distancesq(neighbour.position, neighbour.parent->position);
                    float tempGDistance = currentNodePtr->g + gDistance;

                    if (tempGDistance < neighbour.g)
                    {
                        MapNode* neighbourPtr =
                            gridCopy.GetPointerToElement(neighbour.gridPosition.x, neighbour.gridPosition.y);

                        neighbourPtr->SetParent(currentNodePtr);
                        activeNeighbours.Add(*neighbourPtr);
                    }
                }
                else
                {
                    MapNode* neighbourPtr =
                        gridCopy.GetPointerToElement(neighbour.gridPosition.x, neighbour.gridPosition.y);

                    neighbourPtr->SetParent(currentNodePtr);
                    neighbourPtr->state = NodeState.Open;
                    activeNeighbours.Add(*neighbourPtr);
                }
            }

            neighbours.Dispose();
            return activeNeighbours;
        }

        public override void FreeSystem()
        {
        }
    }
}