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
			EntityCommandBuffer.Concurrent ecb = m_endSimECBSystem.CreateCommandBuffer().ToConcurrent();
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
						CommandProcessSystem.QueueCommand<MoveCommandData>(CommandType.Move, targetData, commandBuffer);
					}
					else
					{
						StateTransitionSystem.RequestStateChange(AIState.MovingToPosition, ecb, entityInQueryIndex, entity, targetData);
						currentTarget.findTargetOfType = AITargetType.None;
						commandBuffer.Clear();
						UnityEngine.Debug.Log("Request switch to MovingToPosition state");
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
						CommandProcessSystem.QueueCommand<HarvestCommandData>(CommandType.Harvest, targetData, commandBuffer);
					}
					else
					{
						StateTransitionSystem.RequestStateChange(AIState.MovingToHarvest, ecb, entityInQueryIndex, entity, targetData); 
						currentTarget.findTargetOfType = AITargetType.None;
						commandBuffer.Clear();
						UnityEngine.Debug.Log("Request switch to MovingToHarvest state");
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
						CommandProcessSystem.QueueCommand<AttackCommandData>(CommandType.Attack, targetData, commandBuffer);
					}
					else
					{
						StateTransitionSystem.RequestStateChange(AIState.MovingToAttack, ecb, entityInQueryIndex, entity, targetData);
						currentTarget.findTargetOfType = AITargetType.None;
						commandBuffer.Clear();
						UnityEngine.Debug.Log("Request switch to MovingToHarvest state");
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