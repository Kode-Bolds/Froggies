using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SwitchToState : IComponentData
{
    public AIState aiState;
    public TargetData target;
}

[UpdateAfter(typeof(CommandProcessSystem))]
public class StateTransitionSystem : KodeboldJobSystem
{
    private GameInit.PostStateTransitionEntityCommandBufferSystem m_postStateTransitionECBSystem;
    private EntityQuery m_stateTransitionQueueQuery;

    public override void GetSystemDependencies(Dependencies dependencies)
    {

    }

    public override void InitSystem()
    {
        m_postStateTransitionECBSystem = World.GetOrCreateSystem<GameInit.PostStateTransitionEntityCommandBufferSystem>();
        m_stateTransitionQueueQuery = GetEntityQuery(ComponentType.ReadWrite<StateTransition>());
    }

    public override void UpdateSystem()
    {
		EntityCommandBuffer.ParallelWriter ecb = m_postStateTransitionECBSystem.CreateCommandBuffer().AsParallelWriter();
        BufferFromEntity<StateTransition> stateTransitionQueueLookup = GetBufferFromEntity<StateTransition>();
        Entity stateTransitionQueueEntity = m_stateTransitionQueueQuery.GetSingletonEntity();

        Entities.ForEach((Entity entity, int entityInQueryIndex, ref CurrentTarget target, ref PreviousTarget previousTarget) =>
        {
            DynamicBuffer<StateTransition> stateTransitionQueue = stateTransitionQueueLookup[stateTransitionQueueEntity];

            for (int i = 0; i < stateTransitionQueue.Length; ++i)
            {
                StateTransition stateTransition = stateTransitionQueue[i];
                if (stateTransition.entity != entity)
                {
                    continue;
                }

                //Remove all states.
                ecb.RemoveComponent<IdleState>(entityInQueryIndex, entity);
                ecb.RemoveComponent<MovingToPositionState>(entityInQueryIndex, entity);
                ecb.RemoveComponent<MovingToHarvestState>(entityInQueryIndex, entity);
                ecb.RemoveComponent<HarvestingState>(entityInQueryIndex, entity);
                ecb.RemoveComponent<MovingToDepositState>(entityInQueryIndex, entity);
                ecb.RemoveComponent<MovingToAttackState>(entityInQueryIndex, entity);
                ecb.RemoveComponent<AttackingState>(entityInQueryIndex, entity);

                //Add state that we're switching to.
                switch (stateTransition.aiState)
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
                target.targetData = stateTransition.target;

                stateTransitionQueue.RemoveAt(i);

				return;
            }
        }).Schedule();

        m_postStateTransitionECBSystem.AddJobHandleForProducer(Dependency);
    }

    public override void FreeSystem()
    {

    }
}
