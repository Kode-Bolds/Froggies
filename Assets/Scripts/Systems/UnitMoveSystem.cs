using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;

public class UnitMoveSystem : KodeboldJobSystem
{
	private EndSimulationEntityCommandBufferSystem m_endSimECBSystem;

	public override void GetSystemDependencies(Dependencies dependencies)
	{

	}

	public override void InitSystem()
	{
		m_endSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
	}

	public override void UpdateSystem()
	{
		ComponentDataFromEntity<TargetableByAI> targetableByAILookup = GetComponentDataFromEntity<TargetableByAI>(true);

		float deltaTime = Time.DeltaTime;

		Dependency = Entities.WithReadOnly(targetableByAILookup).ForEach((ref PhysicsVelocity velocity, in LocalToWorld transform, in UnitMove unitMove, in CurrentTarget currentTarget) =>
		{
			if (!targetableByAILookup.Exists(currentTarget.targetData.targetEntity))
				return;

			velocity.Linear = math.normalize(currentTarget.targetData.targetPos - transform.Position) * unitMove.moveSpeed * deltaTime;
			velocity.Linear.y = 0;
		}).ScheduleParallel(Dependency);

		EntityCommandBuffer.Concurrent ecb = m_endSimECBSystem.CreateCommandBuffer().ToConcurrent();

		Dependency = Entities
			.WithAll<MovingToPositionState>()
			.ForEach((Entity entity, int entityInQueryIndex, in Translation translation, in CurrentTarget currentTarget) =>
		{
			float distance = math.distance(translation.Value, currentTarget.targetData.targetPos);

			if (distance < 0.1f)
			{
				StateTransitionSystem.RequestStateChange(AIState.Idle, ecb, entityInQueryIndex, entity);

				Debug.Log("Reached target position, requesting switch to Idle state");
			}
		}).ScheduleParallel(Dependency);

		m_endSimECBSystem.AddJobHandleForProducer(Dependency);
	}

	public override void FreeSystem()
	{

	}
}
