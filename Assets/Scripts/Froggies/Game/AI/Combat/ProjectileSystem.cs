using Kodebolds.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Froggies
{
	public class ProjectileSystem : KodeboldJobSystem
	{
		private BuildPhysicsWorld m_buildPhysicsWorldSystem;
		private StepPhysicsWorld m_stepPhysicsWorld;
		private EndSimulationEntityCommandBufferSystem m_endSimulationECB;

		protected override GameState ActiveGameState => GameState.Updating;

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
			EntityCommandBuffer.ParallelWriter ecb = m_endSimulationECB.CreateCommandBuffer().AsParallelWriter();
			Entities.ForEach((Entity entity, int entityInQueryIndex, ref PhysicsVelocity velocity, ref Projectile projectile, in Translation translation) =>
			{
				float distanceSqrd = math.distancesq(translation.Value, projectile.targetPos);

				if (HasComponent<Translation>(projectile.targetEntity))
					projectile.targetPos = GetComponent<Translation>(projectile.targetEntity).Value + GetComponent<Attackable>(projectile.targetEntity).centreOffset;
				else if(distanceSqrd <= 1.0f)
					ecb.DestroyEntity(entityInQueryIndex, entity);

				float3 directionToTarget = math.normalize(projectile.targetPos - translation.Value);
				velocity.Linear = directionToTarget * projectile.projectileSpeed;
			}).ScheduleParallel();

			Dependency = new OnProjectileCollisionJob
			{
				projectileLookup = GetComponentDataFromEntity<Projectile>(true),
				healthLookup = GetComponentDataFromEntity<Health>(),
				ecb = m_endSimulationECB.CreateCommandBuffer()
			}.Schedule(m_stepPhysicsWorld.Simulation, ref m_buildPhysicsWorldSystem.PhysicsWorld, Dependency);
		}

		[BurstCompile]
		private struct OnProjectileCollisionJob : ICollisionEventsJob
		{
			[ReadOnly] public ComponentDataFromEntity<Projectile> projectileLookup;
			public ComponentDataFromEntity<Health> healthLookup;

			public EntityCommandBuffer ecb;

			public void Execute(CollisionEvent collisionEvent)
			{
				Entity a = collisionEvent.EntityA;
				Entity b = collisionEvent.EntityB;

				Entity hitEntity;
				Projectile projectile;

				if (projectileLookup.TryGetComponentDataFromEntity(a, out projectile))
				{
					hitEntity = b;
					ecb.DestroyEntity(a);
				}
				else if (projectileLookup.TryGetComponentDataFromEntity(b, out projectile))
				{
					hitEntity = a;
					ecb.DestroyEntity(b);
				}
				else
					return; //Neither entity was a projectile.

				if (healthLookup.TryGetComponentDataFromEntity(hitEntity, out Health health))
				{
					health.health -= projectile.damage;
					healthLookup[hitEntity] = health;

					Debug.Log("Range Attacked " + hitEntity + " for " + projectile.damage + " damage. " + ". New health is " + health.health);
				}
			}
		}

		public override void FreeSystem()
		{

		}
	}
}