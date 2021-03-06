using Kodebolds.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Froggies
{
	[AlwaysUpdateSystem]
	public class SpawningQueueSystem : KodeboldJobSystem
	{
		private InputManager m_inputManager;
		private RaycastSystem m_raycastSystem;
		private GridManager m_gridManager;

		public NativeQueue<SpawnCommand> spawnQueue;
		public JobHandle spawnQueueDependencies;

		protected override GameState ActiveGameState => GameState.Updating;

		public override void GetSystemDependencies(Dependencies dependencies)
		{
			m_inputManager = dependencies.GetDependency<InputManager>();
			m_raycastSystem = dependencies.GetDependency<RaycastSystem>();
			m_gridManager = dependencies.GetDependency<GridManager>();
		}

		public override void InitSystem()
		{
			spawnQueue = new NativeQueue<SpawnCommand>(Allocator.Persistent);
		}

		public override void UpdateSystem()
		{
			Dependency = JobHandle.CombineDependencies(Dependency, m_raycastSystem.RaycastSystemDependency);

			if (m_inputManager.InputData.inputActions.spawn)
			{
				NativeArray<RaycastResult> raycastResult = m_raycastSystem.RaycastResult;
				NativeQueue<SpawnCommand> spawnQueueLocal = spawnQueue;
				NativeArray2D<MapNode> grid = m_gridManager.Grid;

				Dependency = Entities.WithReadOnly(grid).WithReadOnly(raycastResult).ForEach((ref RuntimePrefabData runtimePrefabData) =>
				{
					if (raycastResult[0].raycastTargetType == RaycastTargetType.Ground)
					{
						Rotation rotation = GetComponent<Rotation>(runtimePrefabData.aiDrone);
						Translation translation = new Translation { Value = raycastResult[0].hitPosition + new float3(0, 1, 0) };
						LocalToWorld localToWorld = new LocalToWorld { Value = new float4x4(rotation.Value, translation.Value) };
						PathFinding pathFinding = new PathFinding { currentNode = PathFindingSystem.FindNearestNode(translation.Value, grid) };

						SpawnCommands.SpawnHarvester(spawnQueueLocal, runtimePrefabData.aiDrone, translation, localToWorld, pathFinding);
					}
				}).Schedule(Dependency);

				spawnQueueDependencies = Dependency;
			}
		}

		public override void FreeSystem()
		{
			spawnQueue.Dispose();
		}
	}
}