using System;
using System.Runtime.InteropServices;
using Unity.Entities;

[UpdateAfter(typeof(FindNearestTargetSystem))]
public class CommandProcessSystem : KodeboldJobSystem
{
	private GameInit.PreStateTransitionEntityCommandBufferSystem m_preStateTransECBsystem;

	public override void FreeSystem()
	{

	}

	public override void GetSystemDependencies(Dependencies dependencies)
	{

	}

	public override void InitSystem()
	{
		m_preStateTransECBsystem = World.GetOrCreateSystem<GameInit.PreStateTransitionEntityCommandBufferSystem>();
	}

	public override void UpdateSystem()
	{

		EntityCommandBuffer.Concurrent ecb = m_preStateTransECBsystem.CreateCommandBuffer().ToConcurrent();

		Entities.WithAll<IdleState>().ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<Command> commandBuffer) =>
		{
			if (commandBuffer.Length <= 0)
				return;
			
			Command command = commandBuffer[0];
			commandBuffer.RemoveAt(0);
			UnityEngine.Debug.Log($"Processing { command.CommandType } command from queue");
			ProcessCommand(ecb, entityInQueryIndex, entity, command);

		}).ScheduleParallel();

		Entities.ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<Command> commandBuffer, in SwitchToState switchToState) =>
		{
			if (commandBuffer.Length <= 0)
				return;


			switch (switchToState.aiState)
			{
				case AIState.Harvesting:
				case AIState.Attacking:
					return;
				case AIState.Idle:
				case AIState.MovingToAttack:
				case AIState.MovingToDeposit:
				case AIState.MovingToHarvest:
				case AIState.MovingToPosition:
					{
						Command command = commandBuffer[0];
						commandBuffer.RemoveAt(0);
						UnityEngine.Debug.Log($"Processing { command.CommandType } command from queue");
						ProcessCommand(ecb, entityInQueryIndex, entity, command);
						break;
					}
			}

		}).ScheduleParallel();

		m_preStateTransECBsystem.AddJobHandleForProducer(Dependency);
	}

	private static void ProcessCommand(EntityCommandBuffer.Concurrent ecb, int entityInQueryIndex, Entity entity, Command command)
	{
		switch (command.CommandType)
		{
			case CommandType.Move:
				MoveCommandData moveCommandData = command.GetCommandData<MoveCommandData>();
				StateTransitionSystem.RequestStateChange(AIState.MovingToPosition, ecb, entityInQueryIndex, entity,
					moveCommandData.TargetData);
				break;
			case CommandType.Harvest:
				HarvestCommandData harvestCommandData = command.GetCommandData<HarvestCommandData>();
				StateTransitionSystem.RequestStateChange(AIState.MovingToHarvest, ecb, entityInQueryIndex, entity,
					harvestCommandData.TargetData);
				break;
			case CommandType.Attack:
				AttackCommandData attackCommandData = command.GetCommandData<AttackCommandData>();
				StateTransitionSystem.RequestStateChange(AIState.MovingToAttack, ecb, entityInQueryIndex, entity,
					attackCommandData.TargetData);
				break;
			case CommandType.Deposit:
				DepositCommandData depositCommandData = command.GetCommandData<DepositCommandData>();
				StateTransitionSystem.RequestStateChange(AIState.MovingToDeposit, ecb, entityInQueryIndex, entity,
					depositCommandData.TargetData);
				break;
		}
	}
	public static void QueueCommand<T>(CommandType commandType, in TargetData targetData, in DynamicBuffer<Command> commandBuffer) where T : struct, ICommandData
	{
		Command newCommand = new Command
		{
			CommandType = commandType,
		};

		T commandData = new T
		{
			TargetData = targetData
		};

		newCommand.AddCommandData(commandData);
		
		commandBuffer.Add(newCommand);
		UnityEngine.Debug.Log($"Added { commandType } command to the queue");

	}
}


public enum CommandType
{
	Move = 0,
	Harvest = 1,
	Attack = 2,
	Deposit = 3
}

public interface ICommandData
{
	TargetData TargetData { get; set; }
}



[StructLayout(LayoutKind.Explicit)]
public struct Command : IBufferElementData
{
	[FieldOffset(0)]private CommandType commandType;
	[FieldOffset(4)] private MoveCommandData moveCommandData;
	[FieldOffset(4)] private HarvestCommandData harvestCommandData;
	[FieldOffset(4)] private AttackCommandData attackCommandData;
	[FieldOffset(4)] private DepositCommandData depositCommandData;

	public CommandType CommandType { get => commandType; set => commandType = value; }

	public T GetCommandData<T>() where T : struct, ICommandData
	{
		switch (CommandType)
		{
			case CommandType.Move:
				return (T)(ICommandData)moveCommandData;
			case CommandType.Harvest:
				return (T)(ICommandData)harvestCommandData;
			case CommandType.Attack:
				return (T)(ICommandData)attackCommandData;
			case CommandType.Deposit:
				return (T)(ICommandData)depositCommandData;
			default:
				throw new Exception("Invalid command type");
		}
	}

	public void AddCommandData<T>(T commandData) where T : struct, ICommandData
	{
		if (commandData is MoveCommandData moveCommand)
		{
			moveCommandData = moveCommand;
			return;
		}
		if (commandData is HarvestCommandData harvestCommand)
		{
			harvestCommandData = harvestCommand;
			return;
		}
		if (commandData is AttackCommandData attackCommand)
		{
			attackCommandData = attackCommand;
			return;
		}
		if (commandData is DepositCommandData depositCommand)
		{
			depositCommandData = depositCommand;
			return;
		}
		throw new Exception("Invalid command type");
	}

}

public struct MoveCommandData : ICommandData
{
	private TargetData targetData;
	public TargetData TargetData { get => targetData; set => targetData = value; }

}

public struct HarvestCommandData : ICommandData
{
	private TargetData targetData;
	public TargetData TargetData { get => targetData; set => targetData = value; }
}

public struct AttackCommandData : ICommandData
{
	private TargetData targetData;
	public TargetData TargetData { get => targetData; set => targetData = value; }
}

public struct DepositCommandData : ICommandData
{
	private TargetData targetData;
	public TargetData TargetData { get => targetData; set => targetData = value; }
}