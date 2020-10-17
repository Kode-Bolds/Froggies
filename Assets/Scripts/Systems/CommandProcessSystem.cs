using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(FindAITargetSystem))]
public class CommandProcessSystem : KodeboldJobSystem
{
	private EntityQuery m_stateTransitionQueueQuery;

	private EntityQuery m_resourceQuery;
	private EntityQuery m_enemyQuery;
	private EntityQuery m_storeQuery;

	private DebugDrawer m_debugDrawer;

	public override void FreeSystem()
	{

	}

	public override void GetSystemDependencies(Dependencies dependencies)
	{
		m_debugDrawer = dependencies.GetDependency<DebugDrawer>();
	}

	public override void InitSystem()
	{
		m_stateTransitionQueueQuery = GetEntityQuery(ComponentType.ReadWrite<StateTransition>());

		m_resourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceNode>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<TargetableByAI>());
		m_enemyQuery = GetEntityQuery(ComponentType.ReadOnly<EnemyTag>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<TargetableByAI>());
		m_storeQuery = GetEntityQuery(ComponentType.ReadOnly<Store>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<TargetableByAI>());
	}

	public override void UpdateSystem()
	{
		BufferFromEntity<StateTransition> stateTransitionQueueLookup = GetBufferFromEntity<StateTransition>();
		Entity stateTransitionQueueEntity = m_stateTransitionQueueQuery.GetSingletonEntity();
		ComponentDataFromEntity<IdleState> idleComponentLookup = GetComponentDataFromEntity<IdleState>(true);

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

		Dependency = JobHandle.CombineDependencies(JobHandle.CombineDependencies(resourceQueries, enemyQueries, storeQueries), Dependency);

#if UNITY_EDITOR
		Dependency = JobHandle.CombineDependencies(Dependency, m_debugDrawer.debugDrawDependencies);

		NativeQueue<DebugDrawCommand>.ParallelWriter debugDrawCommandQueue = m_debugDrawer.DebugDrawCommandQueueParallel;
#endif

		Dependency = Entities
		.WithReadOnly(idleComponentLookup)
		.WithReadOnly(resourceTranslations)
		.WithReadOnly(resourceTargets)
		.WithReadOnly(resourceEntities)
		.WithReadOnly(enemyTranslations)
		.WithReadOnly(enemyTargets)
		.WithReadOnly(enemyEntities)
		.WithReadOnly(storeTranslations)
		.WithReadOnly(storeTargets)
		.WithReadOnly(storeEntities)
		.WithDisposeOnCompletion(resourceTranslations)
		.WithDisposeOnCompletion(resourceTargets)
		.WithDisposeOnCompletion(resourceEntities)
		.WithDisposeOnCompletion(enemyTranslations)
		.WithDisposeOnCompletion(enemyTargets)
		.WithDisposeOnCompletion(enemyEntities)
		.WithDisposeOnCompletion(storeTranslations)
		.WithDisposeOnCompletion(storeTargets)
		.WithDisposeOnCompletion(storeEntities)
		.ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<Command> commandBuffer, ref CurrentTarget currentTarget, in Translation translation) =>
		{
			DynamicBuffer<StateTransition> stateTransitionQueue = stateTransitionQueueLookup[stateTransitionQueueEntity];

			if (commandBuffer.Length <= 0)
			{
				if (!idleComponentLookup.HasComponent(entity))
				{
					AddStateTransitionToQueue(ref stateTransitionQueue, entity, new TargetData { }, AIState.Idle);
				}
				return;
			}

#if UNITY_EDITOR
			for (int commandIndex = 1; commandIndex < commandBuffer.Length; commandIndex++)
			{
				debugDrawCommandQueue.Enqueue(new DebugDrawCommand
				{
					debugDrawCommandType = DebugDrawCommandType.Line,
					debugDrawLineData = new DebugDrawLineData
					{
						colour = Color.red,
						start = commandBuffer[commandIndex - 1].commandData.targetData.targetPos,
						end = commandBuffer[commandIndex].commandData.targetData.targetPos
					}
				});
			}
#endif

			Command currentCommand = commandBuffer[0];

			if (currentCommand.commandStatus == CommandStatus.Complete)
			{
				//If the command is complete remove it from the queue
				commandBuffer.RemoveAt(0);
				if (commandBuffer.Length != 0)
				{
					//Process the next command
					currentCommand = commandBuffer[0];
				}
				else
				{
					return;
				}
			}

			if (currentCommand.commandStatus == CommandStatus.Queued)
			{
				//If we have not been assigned a target for this command, then we attempt to find a target for it.
				//We loop here until we hit a command with a target or the command queue is empty, in which case we switch to idle.
				while (currentCommand.commandData.targetData.targetEntity == Entity.Null)
				{
					//If we don't find a target, then we move on to the next command if there is one, or set our state to idle.
					if(!FindNearestTarget(ref currentCommand, translation, resourceTargets, resourceTranslations, resourceEntities, storeTargets, storeTranslations, storeEntities, enemyTargets, enemyTranslations, enemyEntities))
					{
						commandBuffer.RemoveAt(0);
						if (commandBuffer.Length > 0)
						{
							currentCommand = commandBuffer[0];
						}
						else
						{
							AddStateTransitionToQueue(ref stateTransitionQueue, entity, new TargetData { }, AIState.Idle);
							return;
						}
					}
				}

				currentCommand.commandStatus = CommandStatus.MovingPhase;
				Debug.Log($"Processing the moving phase { currentCommand.commandType } command from queue");
				ProcessMovingPhaseCommand(stateTransitionQueue, entity, ref currentCommand);
				//We do not progress state to execution here as we leave the specific systems to tell us when we are in range for the command

			}
			else if (currentCommand.commandStatus == CommandStatus.ExecutionPhase && currentCommand.commandStatus != currentCommand.previousCommandStatus)
			{
				Debug.Log($"Processing the execution phase { currentCommand.commandType } command from queue");
				ProcessExecutionPhaseCommand(stateTransitionQueue, entity, ref currentCommand);
				//We do not progress state to command here as we leave the specific systems to tell us when we have completed the command.
			}

			currentCommand.previousCommandStatus = currentCommand.commandStatus;
			commandBuffer[0] = currentCommand;
		}).Schedule(Dependency);

#if UNITY_EDITOR
		m_debugDrawer.debugDrawDependencies = Dependency;
#endif
	}

	private static bool FindNearestTarget(ref Command currentCommand, in Translation translation,
		in NativeArray<TargetableByAI> resourceTargets, in NativeArray<Translation> resourceTranslations, NativeArray<Entity> resourceEntities,
		in NativeArray<TargetableByAI> storeTargets, in NativeArray<Translation> storeTranslations, NativeArray<Entity> storeEntities,
		in NativeArray<TargetableByAI> enemyTargets, in NativeArray<Translation> enemyTranslations, NativeArray<Entity> enemyEntities)
	{
		AITargetType targetType = currentCommand.commandData.targetData.targetType;

		Debug.Log($"Finding nearest target of type { targetType }");

		int closestTargetIndex = -1;
		switch (targetType)
		{
			case AITargetType.FoodResource:
			case AITargetType.BuildingResource:
			case AITargetType.RareResource:

				closestTargetIndex = FindTarget(resourceTargets, resourceTranslations, resourceEntities, targetType, translation);

				//If we don't find a nearby resource node, then find the nearest store to deposit at and queue a deposit command with the new target.
				if (closestTargetIndex == -1)
				{
					Debug.Log($"Finding nearest target of type { AITargetType.Store }");

					closestTargetIndex = FindTarget(storeTargets, storeTranslations, storeEntities, AITargetType.Store, translation);

					if (closestTargetIndex != -1)
					{
						TargetData targetData = new TargetData
						{
							targetEntity = storeEntities[closestTargetIndex],
							targetType = AITargetType.Store,
							targetPos = storeTranslations[closestTargetIndex].Value
						};

						currentCommand.commandData.targetData = targetData;
						return true;
					}
					else
					{
						//If we don't find a store, then we have no more viable targets to search for and fail to find target!
						return false;
					}
				}
				//Set targetData on current command
				else
				{
					TargetData target = new TargetData
					{
						targetEntity = resourceEntities[closestTargetIndex],
						targetType = targetType,
						targetPos = resourceTranslations[closestTargetIndex].Value
					};

					currentCommand.commandData.targetData = target;
					return true;
				}
			case AITargetType.Enemy:

				closestTargetIndex = FindTarget(enemyTargets, enemyTranslations, enemyEntities, targetType, translation);

				if (closestTargetIndex == -1)
				{
					//TODO: HANDLE THIS CASE???
					return false;
				}
				else
				{
					TargetData target = new TargetData
					{
						targetEntity = enemyEntities[closestTargetIndex],
						targetType = targetType,
						targetPos = enemyTranslations[closestTargetIndex].Value
					};

					currentCommand.commandData.targetData = target;
					return true;
				}
			case AITargetType.Store:

				closestTargetIndex = FindTarget(storeTargets, storeTranslations, storeEntities, targetType, translation);

				if (closestTargetIndex == -1)
				{
					//TODO: HANDLE THIS CASE???
					return false;
				}
				else
				{
					TargetData target = new TargetData
					{
						targetEntity = storeEntities[closestTargetIndex],
						targetType = targetType,
						targetPos = storeTranslations[closestTargetIndex].Value
					};

					currentCommand.commandData.targetData = target;
					return true;
				}

			default:
				return false;
		}
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

	private static void ProcessMovingPhaseCommand(DynamicBuffer<StateTransition> stateTransitionQueue, Entity entity, ref Command command)
	{
		switch (command.commandType)
		{
			case CommandType.Move:
				CommandData moveCommand = command.commandData;
				AddStateTransitionToQueue(ref stateTransitionQueue, entity, moveCommand.targetData, AIState.MovingToPosition);
				break;
			case CommandType.Harvest:
				CommandData harvestCommand = command.commandData;
				AddStateTransitionToQueue(ref stateTransitionQueue, entity, harvestCommand.targetData, AIState.MovingToHarvest);
				break;
			case CommandType.Attack:
				CommandData attackCommand = command.commandData;
				AddStateTransitionToQueue(ref stateTransitionQueue, entity, attackCommand.targetData, AIState.MovingToAttack);
				break;
			case CommandType.Deposit:
				CommandData depositCommand = command.commandData;
				AddStateTransitionToQueue(ref stateTransitionQueue, entity, depositCommand.targetData, AIState.MovingToDeposit);
				break;
		}
	}
	private static void ProcessExecutionPhaseCommand(DynamicBuffer<StateTransition> stateTransitionQueue, Entity entity, ref Command command)
	{
		switch (command.commandType)
		{
			case CommandType.Move:
				Debug.Assert(false, "Moving to target has no execution phase");
				break;
			case CommandType.Harvest:
				CommandData harvestCommand = command.commandData;
				AddStateTransitionToQueue(ref stateTransitionQueue, entity, harvestCommand.targetData, AIState.Harvesting);
				break;
			case CommandType.Attack:
				CommandData attackCommand = command.commandData;
				AddStateTransitionToQueue(ref stateTransitionQueue, entity, attackCommand.targetData, AIState.Attacking);
				break;
			case CommandType.Deposit:
				Debug.Assert(false, "Deposit command has no execution phase (deposit is immediate)");
				break;
		}
	}
	private static void AddStateTransitionToQueue(ref DynamicBuffer<StateTransition> stateTransitionQueue, in Entity entityToTransition, in TargetData targetData, in AIState state)
	{
		stateTransitionQueue.Add(new StateTransition { entity = entityToTransition, target = targetData, aiState = state });
	}

	public static void QueueCommand(CommandType commandType, in DynamicBuffer<Command> commandBuffer, in TargetData targetData, bool onlyQueueIfEmpty)
	{
		if (onlyQueueIfEmpty && commandBuffer.Length > 1)
			return;

		Command newCommand = new Command
		{
			commandType = commandType,
			commandData = new CommandData
			{
				targetData = targetData
			}
		};

		commandBuffer.Add(newCommand);
		Debug.Log($"Added { commandType } command to the queue");
	}

	public static void ExecuteCommand(ref DynamicBuffer<Command> commandBuffer)
	{
		Command currentCommand = commandBuffer[0];
		currentCommand.previousCommandStatus = currentCommand.commandStatus;
		currentCommand.commandStatus = CommandStatus.ExecutionPhase;
		commandBuffer[0] = currentCommand;
		Debug.Log("Execute command of type " + currentCommand.commandType);
	}

	public static void CompleteCommand(ref DynamicBuffer<Command> commandBuffer)
	{
		Command currentCommand = commandBuffer[0];
		currentCommand.previousCommandStatus = currentCommand.commandStatus;
		currentCommand.commandStatus = CommandStatus.Complete;
		commandBuffer[0] = currentCommand;
		Debug.Log("Complete execution command of type " + currentCommand.commandType);
	}
}