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
						switch (command.commandType)
						{
							case CommandType.Move:
								MoveCommandData moveCommandData = command.CommandData<MoveCommandData>();
								StateTransitionSystem.RequestStateChange(AIState.MovingToPosition, ecb, entityInQueryIndex, entity,
									moveCommandData.targetData.targetType, moveCommandData.targetData.targetPos, moveCommandData.targetData.targetEntity);
								break;
							case CommandType.Harvest:
								HarvestCommandData harvestCommandData = command.CommandData<HarvestCommandData>();
								StateTransitionSystem.RequestStateChange(AIState.MovingToHarvest, ecb, entityInQueryIndex, entity,
									harvestCommandData.targetData.targetType, harvestCommandData.targetData.targetPos, harvestCommandData.targetData.targetEntity);
								break;
							case CommandType.Attack:
								AttackCommandData attackCommandData = command.CommandData<AttackCommandData>();
								StateTransitionSystem.RequestStateChange(AIState.MovingToAttack, ecb, entityInQueryIndex, entity,
									attackCommandData.targetData.targetType, attackCommandData.targetData.targetPos, attackCommandData.targetData.targetEntity);
								break;
							case CommandType.Deposit:
								DepositCommandData depositCommandData = command.CommandData<DepositCommandData>();
								StateTransitionSystem.RequestStateChange(AIState.MovingToDeposit, ecb, entityInQueryIndex, entity,
									depositCommandData.targetData.targetType, depositCommandData.targetData.targetPos, depositCommandData.targetData.targetEntity);
								break;
						}
						break;
					}
			}
		}).ScheduleParallel();

		m_preStateTransECBsystem.AddJobHandleForProducer(Dependency);
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
	TargetData targetData { get; set; }
}

public interface ICommand
{
	CommandType commandType { get; set; }

	T CommandData<T>() where T : ICommandData;
}

[StructLayout(LayoutKind.Explicit)]
public struct Command : IBufferElementData, ICommand
{
	public CommandType commandType { get => commandType; set => commandType = value; }

	public T CommandData<T>() where T : ICommandData
	{
		switch (commandType)
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

	[FieldOffset(4)] private MoveCommandData moveCommandData;
	[FieldOffset(4)] private HarvestCommandData harvestCommandData;
	[FieldOffset(4)] private AttackCommandData attackCommandData;
	[FieldOffset(4)] private DepositCommandData depositCommandData;
}

public struct MoveCommandData : ICommandData
{
	public TargetData targetData { get => targetData; set => targetData = value; }
}

public struct HarvestCommandData : ICommandData
{
	public TargetData targetData { get => targetData; set => targetData = value; }
}

public struct AttackCommandData : ICommandData
{
	public TargetData targetData { get => targetData; set => targetData = value; }
}

public struct DepositCommandData : ICommandData
{
	public TargetData targetData { get => targetData; set => targetData = value; }
}