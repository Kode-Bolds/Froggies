using Kodebolds.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Froggies
{
	[UpdateAfter(typeof(UnitMoveSystem))]
	public class CombatSystem : KodeboldJobSystem
	{
		private SpawningQueueSystem m_spawningQueueSystem;

		protected override GameState ActiveGameState => GameState.Updating;

		public override void GetSystemDependencies(Dependencies dependencies)
		{
			m_spawningQueueSystem = dependencies.GetDependency<SpawningQueueSystem>();
		}

		public override void InitSystem()
		{
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

		private unsafe void RangedAttack(float deltaTime)
		{
			NativeQueue<SpawnCommand> spawnQueueLocal = m_spawningQueueSystem.spawnQueue;

			Dependency = Entities.WithAll<AttackingState>().ForEach((ref DynamicBuffer<Command> commandBuffer, ref CurrentTarget currentTarget, ref CombatUnit combatUnit, in Translation translation,
				in RangedUnit rangedUnit, in LocalToWorld localToWorld) =>
			{
				TargetData currentTargetData = currentTarget.targetData;

				//Target no longer exists, therefore is assumed dead.
				if (!HasComponent<Translation>(currentTargetData.targetEntity))
					CommandProcessSystem.CompleteCommand(ref commandBuffer);

				if (!IsInRange(combatUnit.attackRange, translation.Value, currentTargetData.targetPos))
					CommandProcessSystem.RestartCommand(ref commandBuffer);

				combatUnit.attackTimer -= deltaTime;
				if (combatUnit.attackTimer <= 0.0f)
				{
					float3 posToTarget = currentTarget.targetData.targetPos - translation.Value;
					float forwardDotTargetDir = math.dot(localToWorld.Forward, math.normalize(posToTarget));

					//Don't shoot if we're not facing target.
					if (forwardDotTargetDir <= UnitMoveSystem.RotationAngleThresholdDot)
						return;

					Projectile projectile = GetComponent<Projectile>(rangedUnit.projectile);
					projectile.damage = combatUnit.attackDamage;
					projectile.damageType = combatUnit.damageType;
					projectile.targetPos = currentTargetData.targetPos + GetComponent<Attackable>(currentTargetData.targetEntity).centreOffset;
					projectile.targetEntity = currentTargetData.targetEntity;

					Translation projectileTranslation = new Translation { Value = translation.Value + math.rotate(localToWorld.Rotation, rangedUnit.projectileSpawnOffset) };

					SpawnCommands.SpawnProjectile(spawnQueueLocal, rangedUnit.projectile, projectileTranslation, projectile);

					Debug.Log("Firing projectile at " + projectile.targetEntity);

					combatUnit.attackTimer = 1.0f / combatUnit.attackSpeed;
				}
			}).Schedule(JobHandle.CombineDependencies(Dependency, m_spawningQueueSystem.spawnQueueDependencies));
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