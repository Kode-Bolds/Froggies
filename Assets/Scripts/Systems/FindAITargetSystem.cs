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

					ecb.AddComponent(entityInQueryIndex, entity, new SwitchToState { aiState = AIState.MovingToPosition, target = targetData });

					UnityEngine.Debug.Log("Request switch to MovingToPosition state");

					currentTarget.findTargetOfType = AITargetType.None;
					commandBuffer.Clear();

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

					ecb.AddComponent(entityInQueryIndex, entity, new SwitchToState { aiState = AIState.MovingToHarvest, target = targetData }); 

					UnityEngine.Debug.Log("Request switch to MovingToHarvest state");

					currentTarget.findTargetOfType = AITargetType.None;
					commandBuffer.Clear();

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

					ecb.AddComponent(entityInQueryIndex, entity, new SwitchToState { aiState = AIState.MovingToAttack, target = targetData });

					UnityEngine.Debug.Log("Request switch to MovingToAttack state");

					currentTarget.findTargetOfType = AITargetType.None;
					commandBuffer.Clear();

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