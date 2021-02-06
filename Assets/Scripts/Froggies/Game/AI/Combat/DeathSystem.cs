using Kodebolds.Core;
using Unity.Entities;
using Unity.Transforms;

namespace Froggies
{
	public class DeathSystem : KodeboldJobSystem
	{
		private EndSimulationEntityCommandBufferSystem m_endSimulationECB;

		public override void InitSystem()
		{
			m_endSimulationECB = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		public override void GetSystemDependencies(Dependencies dependencies)
		{
			
		}

		public override void UpdateSystem()
		{
			EntityCommandBuffer.ParallelWriter ecb = m_endSimulationECB.CreateCommandBuffer().AsParallelWriter();

			BufferFromEntity<Child> childLookup = GetBufferFromEntity<Child>();

			Entities.WithReadOnly(childLookup).ForEach((Entity entity, int entityInQueryIndex, in Health health) =>
			{
				if(health.health <= 0)
				{
					ecb.DestroyEntityWithChildren(entityInQueryIndex, entity, childLookup);
				}
			}).ScheduleParallel();

			m_endSimulationECB.AddJobHandleForProducer(Dependency);
		}

		public override void FreeSystem()
		{
			
		}
	}
}