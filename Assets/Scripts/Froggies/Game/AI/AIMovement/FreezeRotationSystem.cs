using Kodebolds.Core;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;

namespace Froggies
{
	public class FreezeRotationSystem : KodeboldJobSystem
	{
		private EndInitializationEntityCommandBufferSystem m_entityCommandBuffer;

		public override void GetSystemDependencies(Dependencies dependencies)
		{

		}

		public override void InitSystem()
		{
			m_entityCommandBuffer = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
		}

		public override void UpdateSystem()
		{
			EntityCommandBuffer.ParallelWriter ecb = m_entityCommandBuffer.CreateCommandBuffer().AsParallelWriter();

			JobHandle freezeRotationJobHandle = Entities.ForEach((Entity entity, int entityInQueryIndex, ref PhysicsMass physicsMass, in FreezeRotation freezeRotation) =>
			{
				physicsMass.InverseInertia[0] = freezeRotation.x ? 0 : physicsMass.InverseInertia[0];
				physicsMass.InverseInertia[1] = freezeRotation.y ? 0 : physicsMass.InverseInertia[1];
				physicsMass.InverseInertia[2] = freezeRotation.z ? 0 : physicsMass.InverseInertia[2];

				ecb.RemoveComponent<FreezeRotation>(entityInQueryIndex, entity);
			}).Schedule(Dependency);

			m_entityCommandBuffer.AddJobHandleForProducer(freezeRotationJobHandle);
			Dependency = freezeRotationJobHandle;
		}

		public override void FreeSystem()
		{

		}
	}
}
