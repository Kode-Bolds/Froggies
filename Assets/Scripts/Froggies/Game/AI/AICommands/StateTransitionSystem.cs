using Kodebolds.Core;
using Unity.Entities;
using UnityEngine;

namespace Froggies
{
	public class StateTransitionSystem : KodeboldJobSystem
	{
		private EndSimulationEntityCommandBufferSystem m_endSimulationECB;

		public override void GetSystemDependencies(Dependencies dependencies)
		{

		}

		public override void InitSystem()
		{
			m_endSimulationECB = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		}

		public override void UpdateSystem()
		{
			EntityCommandBuffer.ParallelWriter ecb = m_endSimulationECB.CreateCommandBuffer().AsParallelWriter();

			Entities.ForEach((Entity entity, int entityInQueryIndex, ref CurrentTarget target, ref PreviousTarget previousTarget, ref CurrentAIState currentAIState) =>
			{
				if (currentAIState.requestedAIState == AIState.None)
					return;

			//Remove all states.
			ecb.RemoveComponent<IdleState>(entityInQueryIndex, entity);
				ecb.RemoveComponent<MovingToPositionState>(entityInQueryIndex, entity);
				ecb.RemoveComponent<MovingToHarvestState>(entityInQueryIndex, entity);
				ecb.RemoveComponent<HarvestingState>(entityInQueryIndex, entity);
				ecb.RemoveComponent<MovingToDepositState>(entityInQueryIndex, entity);
				ecb.RemoveComponent<MovingToAttackState>(entityInQueryIndex, entity);
				ecb.RemoveComponent<AttackingState>(entityInQueryIndex, entity);

			//Add state that we're switching to.
			switch (currentAIState.requestedAIState)
				{
					case AIState.Idle:
						{
							ecb.AddComponent<IdleState>(entityInQueryIndex, entity);
							//Debug.Log("State changed to Idle");
							break;
						}
					case AIState.MovingToPosition:
						{
							ecb.AddComponent<MovingToPositionState>(entityInQueryIndex, entity);
							//Debug.Log("State changed to MovingToPosition");
							break;
						}
					case AIState.MovingToHarvest:
						{
							ecb.AddComponent<MovingToHarvestState>(entityInQueryIndex, entity);
							//Debug.Log("State changed to MovingToHarvest");
							break;
						}
					case AIState.Harvesting:
						{
							ecb.AddComponent<HarvestingState>(entityInQueryIndex, entity);
							//Debug.Log("State changed to Harvesting");
							break;
						}
					case AIState.MovingToDeposit:
						{
							ecb.AddComponent<MovingToDepositState>(entityInQueryIndex, entity);
							//Debug.Log("State changed to MovingToDeposit");
							break;
						}
					case AIState.MovingToAttack:
						{
							ecb.AddComponent<MovingToAttackState>(entityInQueryIndex, entity);
							//Debug.Log("State changed to MovingToAttack");
							break;
						}
					case AIState.Attacking:
						{
							ecb.AddComponent<AttackingState>(entityInQueryIndex, entity);
							//Debug.Log("State changed to Attacking");
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
				target.targetData = currentAIState.requestedAIStateTargetData;

				currentAIState.CompleteStateChange();

			}).Schedule();

			m_endSimulationECB.AddJobHandleForProducer(Dependency);
		}

		public override void FreeSystem()
		{

		}
	}
}