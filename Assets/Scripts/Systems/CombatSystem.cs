
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class CombatSystem : KodeboldJobSystem
{
	public override void GetSystemDependencies(Dependencies dependencies)
	{

	}

	public override void InitSystem()
	{

	}

	public override void UpdateSystem()
	{
		ComponentDataFromEntity<Translation> translationLookup = GetComponentDataFromEntity<Translation>();
		ComponentDataFromEntity<Health> healthLookup = GetComponentDataFromEntity<Health>();

		Entities.WithAll<MovingToAttackState>().ForEach((ref DynamicBuffer<Command> commandBuffer, ref UnitMove unitMove, ref PhysicsVelocity physicsVelocity, ref CurrentTarget currentTarget,
			in Translation translation, in CombatUnit combatUnit) =>
		{
			Translation targetTranslation = translationLookup[currentTarget.targetData.targetEntity];

			//If our target has moved, update our target pos.
			if (!targetTranslation.Value.Equals(currentTarget.targetData.targetPos))
			{
				currentTarget.targetData.targetPos = targetTranslation.Value;
			}

			float distance = math.distance(translation.Value, currentTarget.targetData.targetPos);

			//We are in range, execute attack command.
			if (distance < combatUnit.attackRange)
			{
				CommandProcessSystem.ExecuteCommand(ref commandBuffer);

				unitMove.rotating = false;
				physicsVelocity.Linear = 0;
			}
		}).ScheduleParallel();


	}

	public override void FreeSystem()
	{

	}
}
