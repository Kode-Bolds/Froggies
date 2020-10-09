using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

public class UnitMoveSystem : KodeboldJobSystem
{
	private EndSimulationEntityCommandBufferSystem m_endSimECBSystem;
	private DebugDrawer m_debugDrawer;

	public override void GetSystemDependencies(Dependencies dependencies)
	{
		m_debugDrawer = dependencies.GetDependency<DebugDrawer>();
	}

	public override void InitSystem()
	{
		m_endSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
	}

	public override void UpdateSystem()
	{
		ComponentDataFromEntity<TargetableByAI> targetableByAILookup = GetComponentDataFromEntity<TargetableByAI>(true);

		float deltaTime = Time.DeltaTime;

#if UNITY_EDITOR
		Dependency = JobHandle.CombineDependencies(Dependency, m_debugDrawer.debugDrawDependencies);

		NativeQueue<DebugDrawCommand>.ParallelWriter debugDrawCommandQueue = m_debugDrawer.DebugDrawCommandQueue.AsParallelWriter();
#endif
		Dependency = Entities
			.WithReadOnly(targetableByAILookup)
			.ForEach((ref PhysicsVelocity velocity, in LocalToWorld transform, in UnitMove unitMove, in CurrentTarget currentTarget) =>
			{
				if (!targetableByAILookup.Exists(currentTarget.targetData.targetEntity))
					return;
#if UNITY_EDITOR
				debugDrawCommandQueue.Enqueue(new DebugDrawCommand
				{
					debugDrawCommandType = DebugDrawCommandType.Line,
					debugDrawLineData = new DebugDrawLineData
					{
						colour = Color.green,
						start = transform.Position,
						end = currentTarget.targetData.targetPos
					}
				});
#endif
				velocity.Linear = math.normalize(currentTarget.targetData.targetPos - transform.Position) * unitMove.moveSpeed * deltaTime;
				velocity.Linear.y = 0;
			}).ScheduleParallel(Dependency);

#if UNITY_EDITOR
		m_debugDrawer.debugDrawDependencies = Dependency;
#endif

		EntityCommandBuffer.Concurrent ecb = m_endSimECBSystem.CreateCommandBuffer().ToConcurrent();

		Dependency = Entities
			.WithAll<MovingToPositionState>()
			.ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<Command> commandBuffer, in Translation translation, in CurrentTarget currentTarget) =>
		{
			float distance = math.distance(translation.Value, currentTarget.targetData.targetPos);

			if (distance < 0.1f)
			{
				commandBuffer.RemoveAt(0);
				Debug.Log("Reached target position, move to next command");
			}
		}).ScheduleParallel(Dependency);

		m_endSimECBSystem.AddJobHandleForProducer(Dependency);
	}

	public override void FreeSystem()
	{

	}
}
