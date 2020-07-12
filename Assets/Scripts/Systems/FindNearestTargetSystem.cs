using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateAfter(typeof(FindAITargetSystem))]
public class FindNearestTargetSystem : KodeboldJobSystem
{

	private EntityQuery m_resourceQuery;
	private EntityQuery m_enemyQuery;
	private EntityQuery m_storeQuery;
	private GameInit.PreStateTransitionEntityCommandBufferSystem m_postFindTargetECBSystem;

	public override void GetSystemDependencies(Dependencies dependencies)
	{

	}

	public override void InitSystem()
	{
		m_resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceNode>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<TargetableByAI>());
		m_enemyQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyTag>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<TargetableByAI>());
		m_storeQuery = GetEntityQuery(ComponentType.ReadOnly<Store>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<TargetableByAI>());

		m_postFindTargetECBSystem = World.GetOrCreateSystem<GameInit.PreStateTransitionEntityCommandBufferSystem>();
	}

	public override void UpdateSystem()
	{
		NativeArray<Translation> resourceTranslations = m_resourceQuery.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out JobHandle getResourceTranslations);
		NativeArray<TargetableByAI> resourceTargets = m_resourceQuery.ToComponentDataArrayAsync<TargetableByAI>(Allocator.TempJob, out JobHandle getResourceTargets);
		NativeArray<Entity> resourceEntities = m_resourceQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle getResourceEntities);
		JobHandle resourceQueries = JobHandle.CombineDependencies(getResourceTranslations, getResourceTargets, getResourceEntities);

		NativeArray<Translation> enemyTranslations = m_resourceQuery.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out JobHandle getEnemyTranslations);
		NativeArray<TargetableByAI> enemyTargets = m_enemyQuery.ToComponentDataArrayAsync<TargetableByAI>(Allocator.TempJob, out JobHandle getEnemyTargets);
		NativeArray<Entity> enemyEntities = m_resourceQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle getEnemyEntites);
		JobHandle enemyQueries = JobHandle.CombineDependencies(getEnemyTranslations, getEnemyTargets, getEnemyEntites);

		NativeArray<Translation> storeTranslations = m_storeQuery.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out JobHandle getStoreTranslations);
		NativeArray<TargetableByAI> storeTargets = m_storeQuery.ToComponentDataArrayAsync<TargetableByAI>(Allocator.TempJob, out JobHandle getStoreTargets);
		NativeArray<Entity> storeEntities = m_storeQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle getStoreEntities);
		JobHandle storeQueries = JobHandle.CombineDependencies(getStoreTranslations, getStoreTargets, getStoreEntities);

		JobHandle dataQueries = JobHandle.CombineDependencies(resourceQueries, enemyQueries, storeQueries);

		EntityCommandBuffer.Concurrent ecb = m_postFindTargetECBSystem.CreateCommandBuffer().ToConcurrent();

		Dependency = Entities
		.WithReadOnly(resourceTranslations)
		.WithReadOnly(resourceTargets)
		.WithReadOnly(resourceEntities)
		.WithReadOnly(enemyTranslations)
		.WithReadOnly(enemyTargets)
		.WithReadOnly(enemyEntities)
		.WithReadOnly(storeTranslations)
		.WithReadOnly(storeTargets)
		.WithReadOnly(storeEntities)
		.WithDeallocateOnJobCompletion(resourceTranslations)
		.WithDeallocateOnJobCompletion(resourceTargets)
		.WithDeallocateOnJobCompletion(resourceEntities)
		.WithDeallocateOnJobCompletion(enemyTranslations)
		.WithDeallocateOnJobCompletion(enemyTargets)
		.WithDeallocateOnJobCompletion(enemyEntities)
		.WithDeallocateOnJobCompletion(storeTranslations)
		.WithDeallocateOnJobCompletion(storeTargets)
		.WithDeallocateOnJobCompletion(storeEntities)
		.ForEach((int entityInQueryIndex, Entity entity, ref CurrentTarget currentTarget, in DynamicBuffer<Command> commandBuffer, in Translation unitTranslation) =>
		{
			if (commandBuffer.Length > 0)
				currentTarget.findTargetOfType = AITargetType.None;

			if (currentTarget.findTargetOfType == AITargetType.None)
				return;

			Debug.Log($"Finding nearest target of type { currentTarget.findTargetOfType }");

			int closestTargetIndex = -1;
			switch (currentTarget.findTargetOfType)
			{
				case AITargetType.FoodResource:
				case AITargetType.BuildingResource:
				case AITargetType.RareResource:

					closestTargetIndex = FindTarget(resourceTargets, resourceTranslations, resourceEntities, currentTarget.findTargetOfType, unitTranslation);

					//If we don't find a nearby resource node, then find the nearest store to deposit at and request a state change to MovingToDeposit instead.
					if (closestTargetIndex == -1)
					{
						Debug.Log($"Finding nearest target of type { AITargetType.Store }");

						closestTargetIndex = FindTarget(storeTargets, storeTranslations, storeEntities, AITargetType.Store, unitTranslation);

						MovingToDeposit(ecb, entityInQueryIndex, entity, storeTargets[closestTargetIndex].targetType,
							storeTranslations[closestTargetIndex].Value, storeEntities[closestTargetIndex], ref currentTarget);
					}
					//Request state change to MovingToHarvest.
					else
					{
						MovingToHarvest(ecb, entityInQueryIndex, entity, resourceTargets[closestTargetIndex].targetType,
							resourceTranslations[closestTargetIndex].Value, resourceEntities[closestTargetIndex], ref currentTarget);
					}
					break;
				case AITargetType.Enemy:

					closestTargetIndex = FindTarget(enemyTargets, enemyTranslations, enemyEntities, currentTarget.findTargetOfType, unitTranslation);

					if (closestTargetIndex == -1)
					{

					}
					else
					{
						MovingToAttack(ecb, entityInQueryIndex, entity, enemyTargets[closestTargetIndex].targetType,
							enemyTranslations[closestTargetIndex].Value, enemyEntities[closestTargetIndex], ref currentTarget);
					}
					break;
				case AITargetType.Store:

					closestTargetIndex = FindTarget(storeTargets, storeTranslations, storeEntities, currentTarget.findTargetOfType, unitTranslation);

					if (closestTargetIndex == -1)
					{

					}
					else
					{
						MovingToDeposit(ecb, entityInQueryIndex, entity, storeTargets[closestTargetIndex].targetType,
							storeTranslations[closestTargetIndex].Value, storeEntities[closestTargetIndex], ref currentTarget);
					}
					break;
			}

		}).ScheduleParallel(JobHandle.CombineDependencies(Dependency, dataQueries));

		m_postFindTargetECBSystem.AddJobHandleForProducer(Dependency);
	}

	private static int FindTarget(in NativeArray<TargetableByAI> targets, in NativeArray<Translation> targetTranslations, in NativeArray<Entity> targetEntities, in AITargetType targetType, in Translation unitTranslation)
	{
		int closestIndex = -1;
		float smallestDistanceSq = -1.0f;
		for (int i = 0; i < targets.Length; ++i)
		{
			float distanceSq = math.distancesq(unitTranslation.Value, targetTranslations[i].Value);
			if (targets[i].targetType == targetType && (closestIndex == -1 || distanceSq < smallestDistanceSq))
			{
				smallestDistanceSq = distanceSq;
				closestIndex = i;
			}
		}

		if (closestIndex != -1)
			Debug.Log($"Closest target at index { closestIndex } with entity id { targetEntities[closestIndex].Index }");
		else
			Debug.Log("Target not found");

		return closestIndex;
	}

	private static void MovingToHarvest(EntityCommandBuffer.Concurrent ecb, int entityInQueryIndex, in Entity entity,
		AITargetType resourceTargetType, in float3 resourceTranslation, in Entity resourceEntity, ref CurrentTarget currentTarget)
	{
		StateTransitionSystem.RequestStateChange(AIState.MovingToHarvest, ecb, entityInQueryIndex, entity, resourceTargetType, resourceTranslation, resourceEntity);

		Debug.Log("Request switch to MovingToHarvest state");

		currentTarget.findTargetOfType = AITargetType.None;
	}

	private static void MovingToAttack(EntityCommandBuffer.Concurrent ecb, int entityInQueryIndex, in Entity entity,
		AITargetType enemyTargetType, in float3 enemyTranslation, in Entity enemyEntity, ref CurrentTarget currentTarget)
	{
		StateTransitionSystem.RequestStateChange(AIState.MovingToAttack, ecb, entityInQueryIndex, entity, enemyTargetType, enemyTranslation, enemyEntity);

		Debug.Log("Request switch to MovingToAttack state");

		currentTarget.findTargetOfType = AITargetType.None;
	}

	private static void MovingToDeposit(EntityCommandBuffer.Concurrent ecb, int entityInQueryIndex, in Entity entity,
		AITargetType storeTargetType, in float3 storeTranslation, in Entity storeEntity, ref CurrentTarget currentTarget)
	{
		StateTransitionSystem.RequestStateChange(AIState.MovingToDeposit, ecb, entityInQueryIndex, entity, storeTargetType, storeTranslation, storeEntity);

		Debug.Log("Request switch to MovingToDeposit state");

		currentTarget.findTargetOfType = AITargetType.None;
	}

	public override void FreeSystem()
	{

	}
}
