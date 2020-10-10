using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

public class FindAITargetSystem : KodeboldJobSystem
{
	private InputManagementSystem m_inputManagementSystem;
	private RaycastSystem m_raycastSystem;
	private EndSimulationEntityCommandBufferSystem m_endSimECBSystem;

	public override void GetSystemDependencies(Dependencies dependencies)
	{
		m_inputManagementSystem = dependencies.GetDependency<InputManagementSystem>();
		m_raycastSystem = dependencies.GetDependency<RaycastSystem>();
	}

	public override void InitSystem()
	{
		m_endSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
	}

	public override void UpdateSystem()
	{
		Dependency = JobHandle.CombineDependencies(Dependency, m_raycastSystem.RaycastSystemDependency);

		if (m_inputManagementSystem.InputData.mouseInput.rightClickPressed)
		{
			NativeArray<RaycastResult> raycastResult = m_raycastSystem.RaycastResult;
			EntityCommandBuffer.ParallelWriter ecb = m_endSimECBSystem.CreateCommandBuffer().AsParallelWriter();
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
						CommandProcessSystem.QueueCommandWithTarget<MoveCommandWithTarget>(CommandType.MoveWithTarget, targetData, commandBuffer);
					}
					else
					{
						commandBuffer.Clear();
						CommandProcessSystem.QueueCommandWithTarget<MoveCommandWithTarget>(CommandType.MoveWithTarget, targetData, commandBuffer);
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
						CommandProcessSystem.QueueCommandWithTarget<HarvestCommandWithTarget>(CommandType.HarvestWithTarget, targetData, commandBuffer);
					}
					else
					{
						commandBuffer.Clear();
						CommandProcessSystem.QueueCommandWithTarget<HarvestCommandWithTarget>(CommandType.HarvestWithTarget, targetData, commandBuffer);
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
						CommandProcessSystem.QueueCommandWithTarget<AttackCommandWithTarget>(CommandType.AttackWithTarget, targetData, commandBuffer);
					}
					else
					{
						commandBuffer.Clear();
						CommandProcessSystem.QueueCommandWithTarget<AttackCommandWithTarget>(CommandType.AttackWithTarget, targetData, commandBuffer);
					}

					return;
				}

			}).ScheduleParallel(Dependency);

			m_endSimECBSystem.AddJobHandleForProducer(Dependency);
		}
	}

	public override void FreeSystem()
	{

	}
}