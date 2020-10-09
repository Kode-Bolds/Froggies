using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;

public struct ResourceTypeValuePair
{
	public ResourceType resourceType;
	public int resourceValue;
}

public class DepositSystem : KodeboldJobSystem
{
	private EndSimulationEntityCommandBufferSystem m_EndSimECBSystem;
	private EntityQuery m_resourcesQuery;
	private NativeQueue<ResourceTypeValuePair> m_resourcesQueue;

	public override void GetSystemDependencies(Dependencies dependencies)
	{
	}

	public override void InitSystem()
	{
		m_EndSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
		m_resourcesQuery = GetEntityQuery(ComponentType.ReadWrite<Resources>());
		m_resourcesQueue = new NativeQueue<ResourceTypeValuePair>(Allocator.Persistent);
	}

	public override void UpdateSystem()
	{
		ComponentDataFromEntity<Store> storeLookup = GetComponentDataFromEntity<Store>();
		ComponentDataFromEntity<ResourceNode> resourceNodeLookup = GetComponentDataFromEntity<ResourceNode>();
		EntityCommandBuffer.Concurrent ecb = m_EndSimECBSystem.CreateCommandBuffer().ToConcurrent();
		NativeQueue<ResourceTypeValuePair>.ParallelWriter resourceQueueParallel = m_resourcesQueue.AsParallelWriter();

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
				if (harvester.currentlyCarryingAmount == 0)
				{

                    Debug.Log(" Nothing to deposit, empty command queue will return us to Idle state");
					return;
				}

				Debug.Log($"Deposited { harvester.currentlyCarryingAmount } of { harvester.currentlyCarryingType }");

				//Add stuff to global resources queue and empty inventory.
				resourceQueueParallel.Enqueue(new ResourceTypeValuePair { resourceType = harvester.currentlyCarryingType, resourceValue = harvester.currentlyCarryingAmount });

				harvester.currentlyCarryingAmount = 0;
				harvester.currentlyCarryingType = ResourceType.None;

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

		//Add all the queue'd resources just deposited by harvesters to the global resources.
		//We do this in a separate non-parallel job and pass the data via a queue because the SetComponent() function does not support parallel writing.
		Entity resourcesEntity = m_resourcesQuery.GetSingletonEntity();
		NativeQueue<ResourceTypeValuePair> resourceQueueLocal = m_resourcesQueue;

		Dependency = Job.WithCode(() =>
		{
			Resources resources = GetComponent<Resources>(resourcesEntity);
			while (resourceQueueLocal.TryDequeue(out ResourceTypeValuePair resourceTypeValuePair))
			{
				resources.ModifyResource(resourceTypeValuePair.resourceType, resourceTypeValuePair.resourceValue);
			}
			SetComponent(resourcesEntity, resources);
		}).Schedule(Dependency);
	}

	public override void FreeSystem()
	{
	}
}
