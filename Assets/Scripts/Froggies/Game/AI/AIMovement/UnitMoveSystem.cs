using Kodebolds.Core;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

namespace Froggies
{
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
			float deltaTime = Time.fixedDeltaTime;

#if UNITY_EDITOR
			Dependency = JobHandle.CombineDependencies(Dependency, m_debugDrawer.debugDrawDependencies);

			NativeQueue<DebugDrawCommand>.ParallelWriter debugDrawCommandQueue = m_debugDrawer.DebugDrawCommandQueueParallel;
#endif
			Dependency = Entities
				.WithAny<MovingToAttackState, MovingToDepositState, MovingToHarvestState>()
				.WithAny<MovingToPositionState>()
				.WithAll<HasPathTag>()
				.ForEach((ref PhysicsVelocity velocity, ref LocalToWorld transform, ref Rotation rotation,
					ref UnitMove unitMove, in PathFinding pathFinding, in PreviousTarget previousTarget,
					in DynamicBuffer<PathNode> path) =>
				{
#if UNITY_EDITOR
					float3 pos1 = transform.Position;

					for (int i = pathFinding.currentIndexOnPath; i < path.Length; ++i)
					{
						float3 pos2 = path[i].position;

						debugDrawCommandQueue.Enqueue(new DebugDrawCommand
						{
							debugDrawCommandType = DebugDrawCommandType.Line,
							debugDrawLineData = new DebugDrawLineData
							{
								colour = Color.green,
								start = pos1,
								end = pos2
							}
						});

						pos1 = pos2;
					}
#endif

					float3 pos = transform.Position;
					pos.y = 0;

					float3 targetPos = path[pathFinding.currentIndexOnPath].position;
					targetPos.y = 0;

					float3 targetDir = math.normalize(targetPos - pos);

					float dot = math.dot(targetDir, transform.Forward);
					float angle = math.acos(dot);

					if (angle > 0.1f)
					{
						if (!unitMove.rotating || !previousTarget.targetData.targetPos.Equals(targetPos))
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
				.WithAll<MovingToPositionState, HasPathTag>()
				.ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<Command> commandBuffer,
					ref UnitMove unitMove, ref PhysicsVelocity physicsVelocity, ref PathFinding pathfinding,
					in Translation translation, in DynamicBuffer<PathNode> path) =>
				{
					float distanceSq = math.distancesq(translation.Value.xz, path[pathfinding.currentIndexOnPath].position.xz);


					if (distanceSq < 1.0f)
					{
						pathfinding.currentNode = path[pathfinding.currentIndexOnPath].gridPosition;

						pathfinding.currentIndexOnPath++;

						if (pathfinding.currentIndexOnPath < path.Length)
							return;

						unitMove.rotating = false;
						physicsVelocity.Linear = 0;
						commandBuffer.RemoveAt(0);
						pathfinding.completedPath = true;
						//Debug.Log("Reached target position, move to next command");
					}
				}).ScheduleParallel(Dependency);

			m_endSimECBSystem.AddJobHandleForProducer(Dependency);
		}

		public override void FreeSystem()
		{

		}
	}
}