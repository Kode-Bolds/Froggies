using Kodebolds.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Froggies
{
	public class FindAITargetSystem : KodeboldJobSystem
	{
		private InputManagementSystem m_inputManagementSystem;
		private RaycastSystem m_raycastSystem;

		public override void GetSystemDependencies(Dependencies dependencies)
		{
			m_inputManagementSystem = dependencies.GetDependency<InputManagementSystem>();
			m_raycastSystem = dependencies.GetDependency<RaycastSystem>();
		}

		public override void InitSystem()
		{
		}

		public override void UpdateSystem()
		{
			Dependency = JobHandle.CombineDependencies(Dependency, m_raycastSystem.RaycastSystemDependency);

			if (m_inputManagementSystem.InputData.mouseInput.rightClickPressed)
			{
				NativeArray<RaycastResult> raycastResult = m_raycastSystem.RaycastResult;
				bool shiftPressed = m_inputManagementSystem.InputData.keyboardInput.shiftDown;

				Dependency = Entities.WithReadOnly(raycastResult).WithAll<SelectedTag>().ForEach((Entity entity, int entityInQueryIndex, ref CurrentTarget currentTarget, ref DynamicBuffer<Command> commandBuffer) =>
				{
					if (raycastResult[0].raycastTargetType == RaycastTargetType.Ground)
					{
						TargetData targetData = new TargetData
						{
							targetEntity = raycastResult[0].raycastTargetEntity,
							targetType = AITargetType.Ground,
							targetPos = raycastResult[0].hitPosition
						};

						if (shiftPressed)
						{
							CommandProcessSystem.QueueCommand(CommandType.Move, commandBuffer, targetData, false);
						}
						else
						{
							commandBuffer.Clear();
							CommandProcessSystem.QueueCommand(CommandType.Move, commandBuffer, targetData, true);
						}

						return;
					}

					Translation targetPos = GetComponent<Translation>(raycastResult[0].raycastTargetEntity);
					TargetableByAI target = GetComponent<TargetableByAI>(raycastResult[0].raycastTargetEntity);

					if (raycastResult[0].raycastTargetType == RaycastTargetType.ResourceNode)
					{
						TargetData targetData = new TargetData
						{
							targetEntity = raycastResult[0].raycastTargetEntity,
							targetType = target.targetType,
							targetPos = targetPos.Value
						};

						if (shiftPressed)
						{
							CommandProcessSystem.QueueCommand(CommandType.Harvest, commandBuffer, targetData, false);
						}
						else
						{
							commandBuffer.Clear();
							CommandProcessSystem.QueueCommand(CommandType.Harvest, commandBuffer, targetData, true);
						}
						return;
					}

					if (raycastResult[0].raycastTargetType == RaycastTargetType.Enemy)
					{
						TargetData targetData = new TargetData
						{
							targetEntity = raycastResult[0].raycastTargetEntity,
							targetType = target.targetType,
							targetPos = targetPos.Value
						};

						if (shiftPressed)
						{
							CommandProcessSystem.QueueCommand(CommandType.Attack, commandBuffer, targetData, false);
						}
						else
						{
							commandBuffer.Clear();
							CommandProcessSystem.QueueCommand(CommandType.Attack, commandBuffer, targetData, true);
						}

						return;
					}

				}).ScheduleParallel(Dependency);

			}
		}

		public override void FreeSystem()
		{

		}
	}
}