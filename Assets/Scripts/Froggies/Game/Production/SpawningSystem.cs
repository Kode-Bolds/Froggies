using Kodebolds.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Froggies
{
	public class SpawningSystem : KodeboldJobSystem
	{
		private EndInitializationEntityCommandBufferSystem m_entityCommandBuffer;

		private SpawningQueueSystem m_spawningQueueSystem;
		private MapManager _mMapManager;

		public override void GetSystemDependencies(Dependencies dependencies)
		{
			m_spawningQueueSystem = dependencies.GetDependency<SpawningQueueSystem>();
			_mMapManager = dependencies.GetDependency<MapManager>();
		}

		public override void InitSystem()
		{
			m_entityCommandBuffer = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
		}

		public override void UpdateSystem()
		{
			Dependency = JobHandle.CombineDependencies(Dependency, m_spawningQueueSystem.spawnQueueDependencies);

			EntityCommandBuffer ecb = m_entityCommandBuffer.CreateCommandBuffer();
			NativeQueue<Translation> spawnQueue = m_spawningQueueSystem.spawnQueue;
			NativeArray2D<MapNode> grid = _mMapManager.map;

			Dependency = Entities.ForEach((ref RuntimePrefabData runtimePrefabData) =>
			{
				Translation translation;
				while (spawnQueue.TryDequeue(out translation))
				{
					Rotation rotation = GetComponent<Rotation>(runtimePrefabData.aiDrone);

					Entity e = ecb.Instantiate(runtimePrefabData.aiDrone);

					ecb.SetComponent(e, translation);
					ecb.SetComponent(e, new LocalToWorld { Value = new float4x4(rotation.Value, translation.Value) });
					ecb.SetComponent(e, new PathFinding { currentNode = MapUtils.FindNearestNode(translation.Value, grid) });
				}
			}).Schedule(Dependency);

			m_entityCommandBuffer.AddJobHandleForProducer(Dependency);
		}

		public override void FreeSystem()
		{

		}
	}
}