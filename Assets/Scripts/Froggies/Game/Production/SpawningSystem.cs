using Kodebolds.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Froggies
{
	public class SpawningSystem : KodeboldJobSystem
	{
		private EndInitializationEntityCommandBufferSystem m_entityCommandBuffer;

		private SpawningQueueSystem m_spawningQueueSystem;

		public override void GetSystemDependencies(Dependencies dependencies)
		{
			m_spawningQueueSystem = dependencies.GetDependency<SpawningQueueSystem>();
		}

		public override void InitSystem()
		{
			m_entityCommandBuffer = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
		}

		public unsafe override void UpdateSystem()
		{
			Dependency = JobHandle.CombineDependencies(Dependency, m_spawningQueueSystem.spawnQueueDependencies);

			EntityCommandBuffer ecb = m_entityCommandBuffer.CreateCommandBuffer();
			NativeQueue<SpawnCommand> spawnQueue = m_spawningQueueSystem.spawnQueue;

			Dependency = Job.WithCode(() =>
			{
				while (spawnQueue.TryDequeue(out SpawnCommand spawnCommand))
				{
					Entity entity;
					switch (spawnCommand.spawnCommandType)
					{
						case SpawnCommandType.Harvester:
							HarvesterSpawnData harvesterSpawnData = *spawnCommand.CommandData<HarvesterSpawnData>();

							entity = ecb.Instantiate(spawnCommand.entity);
							ecb.SetComponent(entity, harvesterSpawnData.translation);
							ecb.SetComponent(entity, harvesterSpawnData.localToWorld);
							ecb.SetComponent(entity, harvesterSpawnData.pathFinding);
							break;
						case SpawnCommandType.Projectile:
							ProjectileSpawnData projectileSpawnData = *spawnCommand.CommandData<ProjectileSpawnData>();

							entity = ecb.Instantiate(spawnCommand.entity);
							ecb.SetComponent(entity, projectileSpawnData.translation);
							ecb.SetComponent(entity, projectileSpawnData.projectile);
							break;
						default:
							Debug.LogError("Invalid spawn command type!");
							break;
					}
				}
			}).Schedule(Dependency);

			m_entityCommandBuffer.AddJobHandleForProducer(Dependency);
		}

		public override void FreeSystem()
		{

		}
	}
}