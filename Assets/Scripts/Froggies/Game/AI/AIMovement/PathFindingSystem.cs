using Kodebolds.Core;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Froggies
{
	public class PathFindingSystem : KodeboldJobSystem
	{
		private EndSimulationEntityCommandBufferSystem m_endSimulationECB;
		private MapManager _mMapManager;

		private EntityQuery m_pathFindingQuery;
		private float recalculatePeriod = 1f;

		protected override GameState ActiveGameState => GameState.Updating;

		public override void GetSystemDependencies(Dependencies dependencies)
		{
			_mMapManager = dependencies.GetDependency<MapManager>();
		}

		public override void InitSystem()
		{
			m_pathFindingQuery = GetEntityQuery(
				ComponentType.ReadWrite<PathFinding>(),
				ComponentType.ReadWrite<PathNode>(),
				ComponentType.ReadOnly<CurrentTarget>(),
				ComponentType.ReadOnly<Translation>());

			m_endSimulationECB = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		public override void UpdateSystem()
		{
			//We use IJobChunk so that we can share a grid between all pathfinders in a chunk
			Dependency = new PathFindingJob
			{
				pathFindingComponentHandle = GetComponentTypeHandle<PathFinding>(),
				hasPathComponentHandle = GetComponentTypeHandle<HasPathTag>(),
				pathNodeBufferHandle = GetBufferTypeHandle<PathNode>(),
				currentTargetComponentHandle = GetComponentTypeHandle<CurrentTarget>(true),
				translationComponentHandle = GetComponentTypeHandle<Translation>(true),
				entityType = GetEntityTypeHandle(),
				gridRef = _mMapManager.map,
				ecb = m_endSimulationECB.CreateCommandBuffer().AsParallelWriter()
			}.ScheduleParallel(m_pathFindingQuery, Dependency);

			m_endSimulationECB.AddJobHandleForProducer(Dependency);
		}

		[BurstCompile]
		private unsafe struct PathFindingJob : IJobChunk
		{
			public ComponentTypeHandle<PathFinding> pathFindingComponentHandle;
			public ComponentTypeHandle<HasPathTag> hasPathComponentHandle;
			public BufferTypeHandle<PathNode> pathNodeBufferHandle;
			[ReadOnly] public ComponentTypeHandle<CurrentTarget> currentTargetComponentHandle;
			[ReadOnly] public ComponentTypeHandle<Translation> translationComponentHandle;
			[ReadOnly] public EntityTypeHandle entityType;

			[ReadOnly] public NativeArray2D<MapNode> gridRef;
			public EntityCommandBuffer.ParallelWriter ecb;

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				//Writeable.
				NativeArray<PathFinding> pathfindingArray = chunk.GetNativeArray(pathFindingComponentHandle);
				BufferAccessor<PathNode> pathNodeBufferAccessor = chunk.GetBufferAccessor(pathNodeBufferHandle);

				//Read Only.
				NativeArray<CurrentTarget> currentTargetArray = chunk.GetNativeArray(currentTargetComponentHandle);
				NativeArray<Translation> translationArray = chunk.GetNativeArray(translationComponentHandle);
				NativeArray<Entity> entities = chunk.GetNativeArray(entityType);

				//Create a copy of the grid for this thread and chunk.
				NativeArray2D<MapNode> gridCopy = new NativeArray2D<MapNode>(gridRef.Length0, gridRef.Length1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				UnsafeUtility.MemCpy(gridCopy.GetUnsafePtr(), gridRef.GetUnsafePtrReadOnly(), gridRef.Length * sizeof(MapNode));

				for (int indexInChunk = 0; indexInChunk < chunk.Count; ++indexInChunk)
				{
					//Writable.
					PathFinding pathfinding = pathfindingArray[indexInChunk];
					DynamicBuffer<PathNode> path = pathNodeBufferAccessor[indexInChunk];

					//Read Only.
					Entity entity = entities[indexInChunk];
					CurrentTarget currentTarget = currentTargetArray[indexInChunk];
					Translation translation = translationArray[indexInChunk];

					bool hasPath = chunk.Has(hasPathComponentHandle);

					if (!pathfinding.requestedPath)
					{
						//We only want to remove our has path component if we didn't request a new one, to avoid re-adding later in the job if we find a new path.
						if (pathfinding.completedPath)
						{
							pathfinding.completedPath = false;
							ecb.RemoveComponent<HasPathTag>(chunkIndex, entity);
							pathfindingArray[indexInChunk] = pathfinding;
						}

						continue;
					}

					path.Clear();
					pathfinding.currentIndexOnPath = 0;
					pathfinding.completedPath = false;
					pathfinding.requestedPath = false;

					//Calculate the closest nodes to us and our target position.
					//Don't search for path if we're already at our target node.
					pathfinding.currentNode = MapUtils.FindNearestNode(translation.Value, gridCopy);
					pathfinding.targetNode = MapUtils.FindNearestNode(currentTarget.targetData.targetPos, gridCopy);
					if (pathfinding.targetNode.Equals(pathfinding.currentNode))
					{
						pathfindingArray[indexInChunk] = pathfinding;
						continue;
					}

					CalculateGridH(gridCopy, ref pathfinding);

					bool pathfound = SearchForPath(pathfinding.currentNode, pathfinding.targetNode, gridCopy);

					if (pathfound)
					{
						ConstructPath(gridCopy, ref pathfinding, ref path);

						if (!hasPath)
							ecb.AddComponent<HasPathTag>(chunkIndex, entity);
					}
					else if (hasPath)
					{
						ecb.RemoveComponent<HasPathTag>(chunkIndex, entity);
					}

					pathfindingArray[indexInChunk] = pathfinding;
				}

				gridCopy.Dispose();
			}
		}

		private static unsafe void CalculateGridH(NativeArray2D<MapNode> gridCopy, ref PathFinding kPathfinding)
		{
			//Fill in h values for grid.
			for (int i = 0; i < gridCopy.Length0; ++i)
			{
				for (int j = 0; j < gridCopy.Length1; ++j)
				{
					MapNode node = gridCopy[i, j];
					node.h = math.distancesq(gridCopy[i, j].position, gridCopy[kPathfinding.targetNode.x, kPathfinding.targetNode.y].position);

					node.parent = null;
					node.child = null;
					node.state = NodeState.Untested;

					gridCopy[i, j] = node;
				}
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
			MapNode* currentNodePtr = gridCopy.GetPointerToElement(currentNode);

			NativeList<MapNode> passableNeighbours = GetPassableNeighbours(currentNode, gridCopy, currentNodePtr);

			NodeCompare nodeCompare = new NodeCompare();
			passableNeighbours.Sort(nodeCompare);

			currentNodePtr->state = NodeState.Closed;
			for (int i = 0; i < passableNeighbours.Length; ++i)
			{
				MapNode neighbour = passableNeighbours[i];
				currentNodePtr->child = gridCopy.GetPointerToElement(neighbour.gridPosition);

				//Target has been reached.
				if (neighbour.gridPosition.Equals(targetNode))
				{
					passableNeighbours.Dispose();
					return true;
				}

				//Recursively search deeper.
				if (SearchForPath(neighbour.gridPosition, targetNode, gridCopy))
				{
					passableNeighbours.Dispose();
					return true;
				}
			}

			passableNeighbours.Dispose();
			return false;
		}

		private static unsafe NativeList<MapNode> GetPassableNeighbours(int2 currentNode,
			NativeArray2D<MapNode> gridCopy, MapNode* currentNodePtr)
		{
			NativeList<MapNode> neighbours = new NativeList<MapNode>(8, Allocator.Temp);

			//Find all neighbours on the grid by checking if all surrounding nodes are within grid bounds and are valid nodes to traverse to.
			for (int x = -1; x <= 1; ++x)
			{
				int xIndex = currentNode.x + x;

				if (!gridCopy.CheckXBounds(xIndex))
					continue;

				for (int y = -1; y <= 1; ++y)
				{
					if (x == 0 && y == 0)
						continue;

					int yIndex = currentNode.y + y;

					if (!gridCopy.CheckYBounds(yIndex))
						continue;

					MapNode neighbour = gridCopy[xIndex, yIndex];
					if (neighbour.occupiedBy != OccupiedBy.Nothing || neighbour.state == NodeState.Closed)
						continue;

					TestNeighbour(gridCopy, ref neighbour, currentNodePtr, neighbours);
				}
			}

			return neighbours;
		}

		private unsafe static void TestNeighbour(NativeArray2D<MapNode> gridCopy, ref MapNode kNeighbour, MapNode* currentNodePtr, NativeList<MapNode> neighbours)
		{
			//If node is open, (eg. already tested previously), we compare the g distances to see if traversing the neighbouring node will be more efficient from this node than its parents node.
			if (kNeighbour.state == NodeState.Open)
			{
				float gDistance = math.distancesq(kNeighbour.position, kNeighbour.parent->position);
				float tempGDistance = currentNodePtr->g + gDistance;

				if (tempGDistance < kNeighbour.g)
				{
					MapNode* neighbourPtr = gridCopy.GetPointerToElement(kNeighbour.gridPosition);

					neighbourPtr->SetParent(currentNodePtr);
					neighbours.Add(*neighbourPtr);
				}
			}
			//If we're untested, we don't have a parent already, so set parent and add to list.
			else
			{
				MapNode* neighbourPtr = gridCopy.GetPointerToElement(kNeighbour.gridPosition);

				neighbourPtr->SetParent(currentNodePtr);
				neighbourPtr->state = NodeState.Open;
				neighbours.Add(*neighbourPtr);
			}
		}

		/// <summary>
		/// Iterates through the children of the current node to the target to retrieve the path.
		/// </summary>
		private unsafe static void ConstructPath(NativeArray2D<MapNode> gridCopy, ref PathFinding pathfinding, ref DynamicBuffer<PathNode> path)
		{
			MapNode node = gridCopy[pathfinding.currentNode.x, pathfinding.currentNode.y];
			while (node.child != null)
			{
				path.Add(new PathNode { position = node.child->position, gridPosition = node.child->gridPosition });
				node = *node.child;
			}
		}

		public override void FreeSystem()
		{
		}
	}
}