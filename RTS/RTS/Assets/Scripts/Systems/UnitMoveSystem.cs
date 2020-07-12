using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;

public class UnitMoveSystem : KodeboldJobSystem
{
	public override void GetSystemDependencies(Dependencies dependencies)
	{

	}

	public override void InitSystem()
	{
		
	}

	public override void UpdateSystem()
	{
		ComponentDataFromEntity<TargetableByAI> targetableByAILookup = GetComponentDataFromEntity<TargetableByAI>(true);

		Entities.WithReadOnly(targetableByAILookup).ForEach((ref PhysicsVelocity velocity, in LocalToWorld transform, in UnitMove unitMove, in CurrentTarget currentTarget) =>
		{
			if (!targetableByAILookup.Exists(currentTarget.targetData.targetEntity))
				return;

			velocity.Linear = math.normalize(currentTarget.targetData.targetPos - transform.Position) * unitMove.moveSpeed;
			velocity.Linear.y = 0;
		}).ScheduleParallel();
	}

	public override void FreeSystem()
	{

	}
}
