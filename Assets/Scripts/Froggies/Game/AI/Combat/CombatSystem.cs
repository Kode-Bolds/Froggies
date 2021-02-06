using Kodebolds.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Froggies
{
	public class CombatSystem : KodeboldJobSystem
	{
		private EndSimulationEntityCommandBufferSystem m_endSimulationECB;

		public override void GetSystemDependencies(Dependencies dependencies)
		{

		}

		public override void InitSystem()
		{
			m_endSimulationECB = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		public override void UpdateSystem()
		{
			ComponentDataFromEntity<Translation> translationLookup = GetComponentDataFromEntity<Translation>(true);
			ComponentDataFromEntity<Health> healthLookup = GetComponentDataFromEntity<Health>();

			float deltaTime = Time.DeltaTime;

			MovingToAttack(translationLookup);

			//TODO: Investigate ways to optimise and parallelise attacking code.
			MeleeAttack(healthLookup, deltaTime);
			RangedAttack(deltaTime);
		}

		private void MovingToAttack(ComponentDataFromEntity<Translation> translationLookup)
		{
			Entities
				.WithReadOnly(translationLookup)
				.WithAll<MovingToAttackState>().
				ForEach((ref DynamicBuffer<Command> commandBuffer, ref UnitMove unitMove, ref PhysicsVelocity physicsVelocity, ref CurrentTarget currentTarget,
				in Translation translation, in CombatUnit combatUnit) =>
			{
				Translation targetTranslation = translationLookup[currentTarget.targetData.targetEntity];

				//If our target has moved, update our target pos.
				if (!targetTranslation.Value.Equals(currentTarget.targetData.targetPos))
					currentTarget.targetData.targetPos = targetTranslation.Value;

				//We are in range, execute attack command.
				if (IsInRange(combatUnit.attackRange, translation.Value, currentTarget.targetData.targetPos))
				{
					CommandProcessSystem.ExecuteCommand(ref commandBuffer);

					unitMove.rotating = false;
					physicsVelocity.Linear = 0;
				}
			}).ScheduleParallel();
		}

		private void MeleeAttack(ComponentDataFromEntity<Health> healthLookup, float deltaTime)
		{
			Entities
				.WithAll<AttackingState, MeleeUnit>().ForEach((ref DynamicBuffer<Command> commandBuffer, ref CurrentTarget currentTarget, ref CombatUnit combatUnit, in Translation translation) =>
			{
				//Target no longer exists, therefore is assumed dead.
				if(!HasComponent<Translation>(currentTarget.targetData.targetEntity))
					CommandProcessSystem.CompleteCommand(ref commandBuffer);

				if (!IsInRange(combatUnit.attackRange, translation.Value, currentTarget.targetData.targetPos))
					CommandProcessSystem.RestartCommand(ref commandBuffer);

				combatUnit.attackTimer -= deltaTime;
				if (combatUnit.attackTimer <= 0.0f)
				{
					Health health = healthLookup[currentTarget.targetData.targetEntity];
					health.health -= combatUnit.attackDamage;
					healthLookup[currentTarget.targetData.targetEntity] = health;

					combatUnit.attackTimer = 1.0f / combatUnit.attackSpeed;

					Debug.Log("Melee Attacked " + currentTarget.targetData.targetEntity + " for " + combatUnit.attackDamage + " damage. New attack timer is " + combatUnit.attackTimer
						+ ". New health is " + health.health);
				}
			}).Schedule();
		}

		private void RangedAttack(float deltaTime)
		{
			EntityCommandBuffer ecb = m_endSimulationECB.CreateCommandBuffer();

			Entities.WithAll<AttackingState>().ForEach((ref DynamicBuffer<Command> commandBuffer, ref CurrentTarget currentTarget, ref CombatUnit combatUnit, in Translation translation,
				in RangedUnit rangedUnit) =>
			{
				//Target no longer exists, therefore is assumed dead.
				if (!HasComponent<Translation>(currentTarget.targetData.targetEntity))
					CommandProcessSystem.CompleteCommand(ref commandBuffer);

				if (!IsInRange(combatUnit.attackRange, translation.Value, currentTarget.targetData.targetPos))
					CommandProcessSystem.RestartCommand(ref commandBuffer);

				combatUnit.attackTimer -= deltaTime;
				if (combatUnit.attackTimer <= 0.0f)
				{
					Entity projectile = ecb.Instantiate(rangedUnit.projectile);
					ecb.AddComponent(projectile, new ProjectileTarget { targetPos = currentTarget.targetData.targetPos });
					ecb.SetComponent(projectile, new Translation { Value = new float3 {x = translation.Value.x + 5.0f, y = translation.Value.y + 5.0f, z = translation.Value.z + 5.0f } });

					combatUnit.attackTimer = 1.0f / combatUnit.attackSpeed;
				}
			}).Schedule();

			m_endSimulationECB.AddJobHandleForProducer(Dependency);
		}

		private static bool IsInRange(float attackRange, float3 translation, float3 targetPos)
		{
			float distanceFromTargetSqrd = math.distancesq(translation, targetPos);
			float attackRangeSqrd = attackRange * attackRange;

			if (attackRangeSqrd < distanceFromTargetSqrd)
				return false;

			return true;
		}

		public override void FreeSystem()
		{

		}
	}
}