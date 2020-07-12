using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;

public class HarvestingSystem : KodeboldJobSystem
{
	private EndSimulationEntityCommandBufferSystem m_endSimECBSystem;

	public override void GetSystemDependencies(Dependencies dependencies)
	{
	}

	public override void InitSystem()
	{
		m_endSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
	}

	public override void UpdateSystem()
	{
		EntityCommandBuffer.Concurrent ecb = m_endSimECBSystem.CreateCommandBuffer().ToConcurrent();

		ComponentDataFromEntity<ResourceNode> resourceNodeLookup = GetComponentDataFromEntity<ResourceNode>();

		JobHandle movingToHarvestHandle = Entities
		.WithReadOnly(resourceNodeLookup)
		.WithAll<MovingToHarvestState>()
		.ForEach((Entity entity, int entityInQueryIndex, ref Harvester harvester, ref CurrentTarget currentTarget, in Translation translation) =>
		{
			if (currentTarget.findTargetOfType != AITargetType.None)
				return;

			if (!resourceNodeLookup.TryGetComponentDataFromEntity(currentTarget.targetData.targetEntity, out ResourceNode resourceNode))
			{
				//Debug.Log($"Harvest node {currentTarget.targetData.targetEntity} destroyed when moving to it, finding nearby resource node of type {currentTarget.targetData.targetType} instead");
				
				currentTarget.findTargetOfType = currentTarget.targetData.targetType;
				return;
			}

			//Get harvestable radius
			float dist = math.distance(translation.Value, currentTarget.targetData.targetPos);
			float range = resourceNode.harvestableRadius + harvester.harvestRange;

			//Are we close enough to harvest yet?
			if (dist <= range)
			{
				StateTransitionSystem.RequestStateChange(AIState.Harvesting, ecb, entityInQueryIndex, entity, 
					currentTarget.targetData.targetType, currentTarget.targetData.targetPos, currentTarget.targetData.targetEntity);
				//Debug.Log("Request switch to Harvesting state");
				
				//Set type we are harvesting + empty inventory if type is different
				ResourceNode resource = GetComponent<ResourceNode>(currentTarget.targetData.targetEntity);
				if (harvester.currentlyCarryingType != resource.resourceType)
				{
					//Debug.Log($"Harvesting type { resource.resourceType } setting carry amount to 0");

					harvester.currentlyCarryingAmount = 0;
					harvester.currentlyCarryingType = resource.resourceType;
				}
			}
		}).ScheduleParallel(Dependency);

		float dt = Time.DeltaTime;
		EntityCommandBuffer.Concurrent ecb2 = m_endSimECBSystem.CreateCommandBuffer().ToConcurrent();

		Dependency = Entities
		.WithAll<HarvestingState>()
		.ForEach((Entity entity, int entityInQueryIndex, ref Harvester harvester, ref CurrentTarget currentTarget) =>
		{
			ResourceNode resource = GetComponent<ResourceNode>(currentTarget.targetData.targetEntity);

			//If harvest is on cd
			if (harvester.harvestTickTimer > 0)
			{
				//Cooling down
				harvester.harvestTickTimer -= dt;
				return;
			}
			//Put harvest on cd
			harvester.harvestTickTimer = harvester.harvestTickCooldown;
			
			//Harvest the smallest amount between amount of resource, amount harvestable and inventory space
			int inventorySpace = harvester.carryCapacity - harvester.currentlyCarryingAmount;
			int harvestAmount = math.min(math.min(resource.resourceAmount, harvester.harvestAmount), inventorySpace);

			//Transfer resource from resource node to harvester
			//Debug.Log($"Harvested { harvestAmount } of {resource.resourceType}");
			harvester.currentlyCarryingAmount += harvestAmount;
			resource.resourceAmount -= harvestAmount;

			//If the resource is empty destroy it, we must do this before deciding whether to continue harvesting or go deposit
			if (resource.resourceAmount <= 0)
			{
				//Debug.Log("Fully harvested resource");
				ecb2.DestroyEntity(entityInQueryIndex, currentTarget.targetData.targetEntity);
			}
			else //If the resource isn't being destroyed then update its values
			{
				ecb2.SetComponent(entityInQueryIndex, currentTarget.targetData.targetEntity, resource);
			}

			//If we are at capacity go back to deposit
			if (harvester.currentlyCarryingAmount >= harvester.carryCapacity)
			{
				currentTarget.findTargetOfType = AITargetType.Store;
				return;
			}

			//If the resource is empty find a new one
			if (resource.resourceAmount <= 0)
			{
				currentTarget.findTargetOfType = currentTarget.targetData.targetType;
				return;
			}

		}).ScheduleParallel(movingToHarvestHandle);

		m_endSimECBSystem.AddJobHandleForProducer(Dependency);
	}

	public override void FreeSystem()
	{
	}
}
