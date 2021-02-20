using Kodebolds.Core;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using System.Diagnostics;

namespace Froggies
{
	public class UnitMoveSystem : KodeboldJobSystem
	{
		private DebugDrawer m_debugDrawer;

		private const float m_distanceThresholdSqrd = 1.0f;
		public const float RotationAngleThresholdDot = 0.99939082649f; //Dot product equalling 2 degrees.

		public override void GetSystemDependencies(Dependencies dependencies)
		{
			m_debugDrawer = dependencies.GetDependency<DebugDrawer>();
		}

		public override void InitSystem()
		{
		}

		public override void UpdateSystem()
		{
			float deltaTime = Time.fixedDeltaTime;

			MovingToJob(deltaTime);
			RotatingJob(deltaTime);
			CheckEndPathMovingToPosJob();
			CheckEndPathMovingToActionJob();
		}

		private void MovingToJob(float deltaTime)
		{
#if UNITY_EDITOR
			Dependency = JobHandle.CombineDependencies(Dependency, m_debugDrawer.debugDrawDependencies);

			NativeQueue<DebugDrawCommand>.ParallelWriter debugDrawCommandQueue = m_debugDrawer.DebugDrawCommandQueueParallel;
#endif
			Dependency = Entities
				.WithAny<MovingToAttackState, MovingToDepositState, MovingToHarvestState>()
				.WithAny<MovingToPositionState>()
				.WithAll<HasPathTag>()
				.ForEach((ref PhysicsVelocity velocity, ref LocalToWorld localToWorld, ref UnitMove unitMove, ref Rotation rotation,
					in PathFinding pathfinding, in PreviousTarget previousTarget, in DynamicBuffer<PathNode> path) =>
				{
					DrawDebugPath(ref localToWorld, in pathfinding, in path, debugDrawCommandQueue);

					float3 pos = localToWorld.Position;
					pos.y = 0;

					float3 targetPos = path[pathfinding.currentIndexOnPath].position;
					targetPos.y = 0;

					float3 targetDir = math.normalize(targetPos - pos);

					Rotate(deltaTime, ref localToWorld, ref rotation, ref unitMove, ref targetPos, ref targetDir, in previousTarget);

					velocity.Linear = targetDir * unitMove.moveSpeed;
					velocity.Linear.y = 0;
				}).ScheduleParallel(Dependency);

#if UNITY_EDITOR
			m_debugDrawer.debugDrawDependencies = Dependency;
#endif
		}

		private void RotatingJob(float deltaTime)
		{
			Dependency = Entities
				.WithAny<HarvestingState, AttackingState>()
				.ForEach((ref Rotation rotation, ref LocalToWorld localToWorld, ref UnitMove unitMove,
					in CurrentTarget currentTarget, in PreviousTarget previousTarget) =>
				{
					float3 targetPos = currentTarget.targetData.targetPos;
					float3 targetDir = math.normalize(targetPos - localToWorld.Position);

					Rotate(deltaTime, ref localToWorld, ref rotation, ref unitMove, ref targetPos, ref targetDir, in previousTarget);
				}).ScheduleParallel(Dependency);
		}

		private void CheckEndPathMovingToPosJob()
		{
			Dependency = Entities
				.WithAll<MovingToPositionState, HasPathTag>()
				.ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<Command> commandBuffer,
					ref UnitMove unitMove, ref PhysicsVelocity physicsVelocity, ref PathFinding pathfinding,
					in Translation translation, in DynamicBuffer<PathNode> path) =>
				{
					if (CheckEndPath(ref pathfinding, ref unitMove, ref physicsVelocity, path, translation))
						commandBuffer.RemoveAt(0);

				}).ScheduleParallel(Dependency);
		}

		private void CheckEndPathMovingToActionJob()
		{
			Dependency = Entities
				.WithAny<MovingToAttackState, MovingToDepositState, MovingToHarvestState>()
				.WithAll<HasPathTag>()
				.ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<Command> commandBuffer,
					ref UnitMove unitMove, ref PhysicsVelocity physicsVelocity, ref PathFinding pathfinding,
					in Translation translation, in DynamicBuffer<PathNode> path) =>
				{
					CheckEndPath(ref pathfinding, ref unitMove, ref physicsVelocity, path, translation);
				}).ScheduleParallel(Dependency);
		}

		private static bool CheckEndPath(ref PathFinding pathfinding, ref UnitMove unitMove, ref PhysicsVelocity physicsVelocity, in DynamicBuffer<PathNode> path, in Translation translation)
		{
			float distanceSq = math.distancesq(translation.Value.xz, path[pathfinding.currentIndexOnPath].position.xz);

			if (distanceSq < m_distanceThresholdSqrd)
			{
				pathfinding.currentNode = path[pathfinding.currentIndexOnPath].gridPosition;

				pathfinding.currentIndexOnPath++;

				if (pathfinding.currentIndexOnPath < path.Length)
					return false;

				unitMove.rotating = false;
				physicsVelocity.Linear = 0;
				pathfinding.completedPath = true;
				UnityEngine.Debug.Log("Reached end of path.");
				return true;
			}

			return false;
		}

		[Conditional("DEBUG")]
		private static void DrawDebugPath(ref LocalToWorld kLocalToWorld, in PathFinding pathfinding, in DynamicBuffer<PathNode> path,
			NativeQueue<DebugDrawCommand>.ParallelWriter debugDrawCommandQueue)
		{
			float3 pos1 = kLocalToWorld.Position;

			for (int i = pathfinding.currentIndexOnPath; i < path.Length; ++i)
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
		}

		private static void Rotate(float deltaTime, ref LocalToWorld localToWorld, ref Rotation rotation, ref UnitMove unitMove, ref float3 kTargetPos,
			ref float3 kTargetDir, in PreviousTarget kPreviousTarget)
		{
			float dot = math.dot(kTargetDir, localToWorld.Forward);

			if (dot < RotationAngleThresholdDot)
			{
				if (!unitMove.rotating || !kPreviousTarget.targetData.targetPos.Equals(kTargetPos))
				{
					unitMove.rotating = true;

					unitMove.rotationAngleSign = math.dot(kTargetDir, localToWorld.Right) < 0 ? -1 : 1;
				}

				float turnRateSigned = unitMove.turnRate * math.sign(unitMove.rotationAngleSign);
				rotation.Value = math.mul(rotation.Value, quaternion.RotateY(turnRateSigned * deltaTime));
				localToWorld.Value = new float4x4(rotation.Value, localToWorld.Position);
			}
			else if (unitMove.rotating)
			{
				unitMove.rotating = false;
			}
		}

		public override void FreeSystem()
		{

		}
	}
}