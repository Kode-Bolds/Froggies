using Kodebolds.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Froggies
{
	public class ProjectileSystem : KodeboldJobSystem
	{
		private BuildPhysicsWorld m_buildPhysicsWorldSystem;
		private StepPhysicsWorld m_stepPhysicsWorld;
		private EndSimulationEntityCommandBufferSystem m_endSimulationECB;

		public override void InitSystem()
		{
			m_buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
			m_stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
			m_endSimulationECB = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		public override void GetSystemDependencies(Dependencies dependencies)
		{
			
		}

		public override void UpdateSystem()
		{
			float deltaTime = Time.DeltaTime;
			Entities.ForEach((ref PhysicsVelocity velocity, in Translation translation, in Projectile projectile, in ProjectileTarget target) =>
			{
				float3 directionToTarget = math.normalize(target.targetPos - translation.Value);

				velocity.Linear = directionToTarget * projectile.projectileSpeed * deltaTime;
			}).ScheduleParallel();

			Dependency = new OnProjectileCollisionJob
			{
				projectileLookup = GetComponentDataFromEntity<Projectile>(true),
				ecb = m_endSimulationECB.CreateCommandBuffer()
			}.Schedule(m_stepPhysicsWorld.Simulation, ref m_buildPhysicsWorldSystem.PhysicsWorld, Dependency);
		}

		[BurstCompile]
		private struct OnProjectileCollisionJob : ICollisionEventsJob
		{
			[ReadOnly] public ComponentDataFromEntity<Projectile> projectileLookup;

			public EntityCommandBuffer ecb;

			public void Execute(CollisionEvent collisionEvent)
			{
				Entity a = collisionEvent.EntityA;
				Entity b = collisionEvent.EntityB;

				if (projectileLookup.HasComponent(a))
					ecb.DestroyEntity(a);

				if (projectileLookup.HasComponent(b))
					ecb.DestroyEntity(b);
			}
		}

		public override void FreeSystem()
		{
			
		}
	}
}