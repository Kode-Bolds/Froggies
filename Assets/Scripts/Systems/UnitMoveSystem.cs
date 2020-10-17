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

		float deltaTime = Time.fixedDeltaTime;

#if UNITY_EDITOR
		Dependency = JobHandle.CombineDependencies(Dependency, m_debugDrawer.debugDrawDependencies);

		NativeQueue<DebugDrawCommand>.ParallelWriter debugDrawCommandQueue = m_debugDrawer.DebugDrawCommandQueueParallel;
#endif
		Dependency = Entities
			.WithReadOnly(targetableByAILookup)
			.WithAny<MovingToAttackState, MovingToDepositState, MovingToHarvestState>()
			.WithAny<MovingToPositionState>()
			.ForEach((ref PhysicsVelocity velocity, ref LocalToWorld transform, ref Rotation rotation, ref UnitMove unitMove, in CurrentTarget currentTarget, in PreviousTarget previousTarget) =>
			{
				if (!targetableByAILookup.HasComponent(currentTarget.targetData.targetEntity))
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

				float3 pos = transform.Position;
				pos.y = 0;

				float3 targetPos = currentTarget.targetData.targetPos;
				targetPos.y = 0;

				float3 targetDir = math.normalize(targetPos - pos);

				float dot = math.dot(targetDir, transform.Forward);
				float angle = math.acos(dot);

				if (angle > 0.1f)
				{
					if (!unitMove.rotating || !previousTarget.targetData.targetPos.Equals(currentTarget.targetData.targetPos))
					{
						unitMove.rotating = true;

						if (math.dot(targetDir, transform.Right) < 0)
							angle = -angle;

						unitMove.angle = angle;
					}

					rotation.Value = math.mul(rotation.Value, quaternion.RotateY(unitMove.angle * unitMove.turnRate * deltaTime));
					transform.Value = new float4x4(rotation.Value, transform.Position);
				}
				else if (unitMove.rotating)
				{
					unitMove.rotating = false;
				}

				velocity.Linear = targetDir * unitMove.moveSpeed * deltaTime;
				velocity.Linear.y = 0;
			}).ScheduleParallel(Dependency);

#if UNITY_EDITOR
		m_debugDrawer.debugDrawDependencies = Dependency;
#endif

		EntityCommandBuffer.ParallelWriter ecb = m_endSimECBSystem.CreateCommandBuffer().AsParallelWriter();

		Dependency = Entities
			.WithAll<MovingToPositionState>()
			.ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<Command> commandBuffer, ref UnitMove unitMove, ref PhysicsVelocity physicsVelocity, in Translation translation, in CurrentTarget currentTarget) =>
		{
			float distance = math.distance(translation.Value, currentTarget.targetData.targetPos);

			if (distance < 1.0f)
			{
				unitMove.rotating = false;
				physicsVelocity.Linear = 0;
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
