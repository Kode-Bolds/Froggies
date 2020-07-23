using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SwitchToState : IComponentData
{
	public AIState aiState;
	public TargetData target;
}

[UpdateAfter(typeof(GameInit.PreStateTransitionEntityCommandBufferSystem))]
public class StateTransitionSystem : KodeboldJobSystem
{
	private GameInit.PostStateTransitionEntityCommandBufferSystem m_endInitECBSystem;

	public override void GetSystemDependencies(Dependencies dependencies)
	{

	}

	public override void InitSystem()
	{
		m_endInitECBSystem = World.GetOrCreateSystem<GameInit.PostStateTransitionEntityCommandBufferSystem>();
	}

	public override void UpdateSystem()
	{
		EntityCommandBuffer.Concurrent ecb = m_endInitECBSystem.CreateCommandBuffer().ToConcurrent();

		Entities.ForEach((Entity entity, int entityInQueryIndex, ref CurrentTarget target, ref PreviousTarget previousTarget, in SwitchToState switchToState) =>
		{
			//Remove all states.
			ecb.RemoveComponent<IdleState>(entityInQueryIndex, entity);
			ecb.RemoveComponent<MovingToPositionState>(entityInQueryIndex, entity);
			ecb.RemoveComponent<MovingToHarvestState>(entityInQueryIndex, entity);
			ecb.RemoveComponent<HarvestingState>(entityInQueryIndex, entity);
			ecb.RemoveComponent<MovingToDepositState>(entityInQueryIndex, entity);
			ecb.RemoveComponent<MovingToAttackState>(entityInQueryIndex, entity);
			ecb.RemoveComponent<AttackingState>(entityInQueryIndex, entity);

			//Add state that we're switching to.
			switch (switchToState.aiState)
			{
				case AIState.Idle:
					{
						ecb.AddComponent<IdleState>(entityInQueryIndex, entity);
						Debug.Log("State changed to Idle");
						break;
					}
				case AIState.MovingToPosition:
					{
						ecb.AddComponent<MovingToPositionState>(entityInQueryIndex, entity);
						Debug.Log("State changed to MovingToPosition");
						break;
					}
				case AIState.MovingToHarvest:
					{
						ecb.AddComponent<MovingToHarvestState>(entityInQueryIndex, entity);
						Debug.Log("State changed to MovingToHarvest");
						break;
					}
				case AIState.Harvesting:
					{
						ecb.AddComponent<HarvestingState>(entityInQueryIndex, entity);
						Debug.Log("State changed to Harvesting");
						break;
					}
				case AIState.MovingToDeposit:
					{
						ecb.AddComponent<MovingToDepositState>(entityInQueryIndex, entity);
						Debug.Log("State changed to MovingToDeposit");
						break;
					}
				case AIState.MovingToAttack:
					{
						ecb.AddComponent<MovingToAttackState>(entityInQueryIndex, entity);
						Debug.Log("State changed to MovingToAttack");
						break;
					}
				case AIState.Attacking:
					{
						ecb.AddComponent<AttackingState>(entityInQueryIndex, entity);
						Debug.Log("State changed to Attacking");
						break;
					}
				default:
					{
						Debug.Assert(false, "Unrecognised State");
						break;
					}
			}

			//Set our target and previous target data.
			previousTarget.targetData = target.targetData;
			target.targetData = switchToState.target;

			ecb.RemoveComponent<SwitchToState>(entityInQueryIndex, entity);
		}).Schedule();

		m_endInitECBSystem.AddJobHandleForProducer(Dependency);
	}

	public override void FreeSystem()
	{

	}

	public static void RequestStateChange(	
		AIState aiState, EntityCommandBuffer.Concurrent ecb, int entityInQueryIndex, in Entity entity, 
		in TargetData targetData = default )
	{
		SwitchToState switchToState = new SwitchToState
		{
			aiState = aiState,
			target = targetData
		};

		ecb.AddComponent(entityInQueryIndex, entity, switchToState);
	}
}
