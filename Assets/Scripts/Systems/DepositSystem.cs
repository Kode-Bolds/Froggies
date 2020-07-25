using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;

public class DepositSystem : KodeboldJobSystem
{
    private EndSimulationEntityCommandBufferSystem m_EndSimECBSystem;
    public override void GetSystemDependencies(Dependencies dependencies)
    {
    }

    public override void InitSystem()
    {
        m_EndSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    public override void UpdateSystem()
    {
        ComponentDataFromEntity<Store> storeLookup = GetComponentDataFromEntity<Store>();
        ComponentDataFromEntity<ResourceNode> resourceNodeLookup = GetComponentDataFromEntity<ResourceNode>();
        EntityCommandBuffer.Concurrent ecb = m_EndSimECBSystem.CreateCommandBuffer().ToConcurrent();

        Dependency = Entities
            .WithReadOnly(storeLookup)
            .WithReadOnly(resourceNodeLookup)
            .WithAll<MovingToDepositState>()
            .ForEach((Entity entity, int entityInQueryIndex, ref Harvester harvester, ref CurrentTarget currentTarget, ref DynamicBuffer<Command> commandBuffer, in Translation translation, in PreviousTarget previousTarget) =>
        {
            Store storeComponent = storeLookup[currentTarget.targetData.targetEntity];

            float dist = math.distance(translation.Value, currentTarget.targetData.targetPos);
            float range = storeComponent.depositRadius + harvester.harvestRange;

            //Are we close enough to deposit yet?
            if (dist <= range)
            {
                if(harvester.currentlyCarryingAmount == 0)
				{
                    Debug.Log(" Nothing to deposit, empty command queue will return us to Idle state");
                    return;
                }

                Debug.Log($"Deposited { harvester.currentlyCarryingAmount } of { harvester.currentlyCarryingType }");

                //Drop stuff
                harvester.currentlyCarryingAmount = 0;
                harvester.currentlyCarryingType = ResourceType.None;

                //Add stuff to global resource thingy
                //TODO: Jakey :)

                if (resourceNodeLookup.Exists(previousTarget.targetData.targetEntity))
                {
                    CommandProcessSystem.QueueCommandWithTarget<HarvestCommandWithTarget>(CommandType.HarvestWithTarget, previousTarget.targetData, commandBuffer);
                    Debug.Log($"Requesting switch to MoveToHarvest state for previously harvested resource node {previousTarget.targetData.targetEntity} of type {previousTarget.targetData.targetType}");
                }
                else
                {
                    CommandProcessSystem.QueueCommandWithoutTarget<HarvestCommandWithoutTarget>(CommandType.HarvestWithoutTarget, previousTarget.targetData.targetType, commandBuffer);

                    Debug.Log($"Previously harvested resource node {previousTarget.targetData.targetEntity} of type {previousTarget.targetData.targetType} no longer exists, requesting switch to MovingToHarvest state");
                }
            }

        }).ScheduleParallel(Dependency);

        m_EndSimECBSystem.AddJobHandleForProducer(Dependency);
    }
    public override void FreeSystem()
    {
    }
}
