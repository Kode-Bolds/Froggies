using System;
using System.Runtime.InteropServices;
using Unity.Entities;

[UpdateAfter(typeof(FindNearestTargetSystem))]
public class CommandProcessSystem : KodeboldJobSystem
{
    private GameInit.PreStateTransitionEntityCommandBufferSystem m_preStateTransECBsystem;
    private EntityQuery m_stateTransitionQueueQuery;

    public override void FreeSystem()
    {

    }

    public override void GetSystemDependencies(Dependencies dependencies)
    {

    }

    public override void InitSystem()
    {
        m_preStateTransECBsystem = World.GetOrCreateSystem<GameInit.PreStateTransitionEntityCommandBufferSystem>();
        m_stateTransitionQueueQuery = GetEntityQuery(ComponentType.ReadWrite<StateTransition>());
    }

    public override void UpdateSystem()
    {
        BufferFromEntity<StateTransition> stateTransitionQueueLookup = GetBufferFromEntity<StateTransition>();
        Entity stateTransitionQueueEntity = m_stateTransitionQueueQuery.GetSingletonEntity();
        DynamicBuffer<StateTransition> stateTransitionQueue = stateTransitionQueueLookup[stateTransitionQueueEntity];
        ComponentDataFromEntity<IdleState> idleComponentLookup = GetComponentDataFromEntity<IdleState>(true);

        Entities
        .WithReadOnly(idleComponentLookup)
        .ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<Command> commandBuffer, ref CurrentTarget currentTarget) =>
        {
            if (commandBuffer.Length <= 0)
            {
                if (idleComponentLookup.Exists(entity))
                {
                    AddStateTransitionToQueue(ref stateTransitionQueue, entity, new TargetData { }, AIState.Idle);
                }
                return;
            }

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
                currentCommand.commandStatus = CommandStatus.MovingPhase;
                UnityEngine.Debug.Log($"Processing the moving phase { currentCommand.commandType } command from queue");
                ProcessMovingPhaseCommand(stateTransitionQueue, entity, ref currentCommand, ref currentTarget);
                //We do not progress state to execution here as we leave the specific systems to tell us when we are in range for the command
            }
            else if (currentCommand.commandStatus == CommandStatus.ExecutionPhase && currentCommand.commandStatus != currentCommand.previousCommandStatus)
            {
                UnityEngine.Debug.Log($"Processing the execution phase { currentCommand.commandType } command from queue");
                ProcessExecutionPhaseCommand(stateTransitionQueue, entity, ref currentCommand, ref currentTarget);
                currentCommand.commandStatus = CommandStatus.Complete;
            }

            currentCommand.previousCommandStatus = currentCommand.commandStatus;
            commandBuffer[0] = currentCommand;
        }).Schedule();

        m_preStateTransECBsystem.AddJobHandleForProducer(Dependency);
    }

    private static void ProcessMovingPhaseCommand(DynamicBuffer<StateTransition> stateTransitionQueue, Entity entity, ref Command command, ref CurrentTarget currentTarget)
    {
        switch (command.commandType)
        {
            case CommandType.MoveWithTarget:
                MoveCommandWithTarget moveCommandWith = command.GetCommandWithTarget<MoveCommandWithTarget>();
                AddStateTransitionToQueue(ref stateTransitionQueue, entity, moveCommandWith.TargetData, AIState.MovingToPosition);
                break;
            case CommandType.HarvestWithTarget:
                HarvestCommandWithTarget harvestCommandWith = command.GetCommandWithTarget<HarvestCommandWithTarget>();
                AddStateTransitionToQueue(ref stateTransitionQueue, entity, harvestCommandWith.TargetData, AIState.MovingToHarvest);
                break;
            case CommandType.AttackWithTarget:
                AttackCommandWithTarget attackCommandWith = command.GetCommandWithTarget<AttackCommandWithTarget>();
                AddStateTransitionToQueue(ref stateTransitionQueue, entity, attackCommandWith.TargetData, AIState.MovingToAttack);
                break;
            case CommandType.DepositWithTarget:
                DepositCommandWithTarget depositCommandWith = command.GetCommandWithTarget<DepositCommandWithTarget>();
                AddStateTransitionToQueue(ref stateTransitionQueue, entity, depositCommandWith.TargetData, AIState.MovingToDeposit);
                break;
            case CommandType.MoveWithoutTarget:
                //What?	
                UnityEngine.Debug.Assert(false, "Moving with no target, what are you trying to do here?");
                break;
            case CommandType.HarvestWithoutTarget:
                HarvestCommandWithoutTarget harvestCommandWithout = command.GetCommandWithoutTarget<HarvestCommandWithoutTarget>();
                currentTarget.findTargetOfType = harvestCommandWithout.TargetTypeToFind;
                break;
            case CommandType.AttackWithoutTarget:
                AttackCommandWithoutTarget attackCommandWithout = command.GetCommandWithoutTarget<AttackCommandWithoutTarget>();
                currentTarget.findTargetOfType = attackCommandWithout.TargetTypeToFind;
                break;
            case CommandType.DepositWithoutTarget:
                DepositCommandWithoutTarget depositCommandWithout = command.GetCommandWithoutTarget<DepositCommandWithoutTarget>();
                currentTarget.findTargetOfType = depositCommandWithout.TargetTypeToFind;
                break;
        }
    }
    private static void ProcessExecutionPhaseCommand(DynamicBuffer<StateTransition> stateTransitionQueue, Entity entity, ref Command command, ref CurrentTarget currentTarget)
    {
        switch (command.commandType)
        {
            case CommandType.MoveWithTarget:
                UnityEngine.Debug.Assert(false, "Moving to target has no execution phase");
                break;
            case CommandType.HarvestWithTarget:
                HarvestCommandWithTarget harvestCommandWith = command.GetCommandWithTarget<HarvestCommandWithTarget>();
                AddStateTransitionToQueue(ref stateTransitionQueue, entity, harvestCommandWith.TargetData, AIState.Harvesting);
                break;
            case CommandType.AttackWithTarget:
                AttackCommandWithTarget attackCommandWith = command.GetCommandWithTarget<AttackCommandWithTarget>();
                AddStateTransitionToQueue(ref stateTransitionQueue, entity, attackCommandWith.TargetData, AIState.Attacking);
                break;
            case CommandType.DepositWithTarget:
                UnityEngine.Debug.Assert(false, "Deposit command has no execution phase (deposit is immediate)");
                break;
            case CommandType.MoveWithoutTarget:
                //What?	
                UnityEngine.Debug.Assert(false, "Moving with no target, what are you trying to do here?");
                break;
            case CommandType.HarvestWithoutTarget:
                HarvestCommandWithoutTarget harvestCommandWithout = command.GetCommandWithoutTarget<HarvestCommandWithoutTarget>();
                AddStateTransitionToQueue(ref stateTransitionQueue, entity, currentTarget.targetData, AIState.Harvesting);
                break;
            case CommandType.AttackWithoutTarget:
                AttackCommandWithoutTarget attackCommandWithout = command.GetCommandWithoutTarget<AttackCommandWithoutTarget>();
                AddStateTransitionToQueue(ref stateTransitionQueue, entity, currentTarget.targetData, AIState.Attacking);
                break;
            case CommandType.DepositWithoutTarget:
                UnityEngine.Debug.Assert(false, "Deposit command has no execution phase (deposit is immediate)");
                break;
        }
    }
    public static void QueueCommandWithTarget<T>(CommandType commandType, in TargetData targetData, in DynamicBuffer<Command> commandBuffer) where T : struct, ICommandWithTarget
    {
        Command newCommand = new Command
        {
            commandType = commandType,
        };

        T commandData = new T
        {
            TargetData = targetData
        };

        newCommand.AddCommandWithTarget(commandData);

        commandBuffer.Add(newCommand);
        UnityEngine.Debug.Log($"Added { commandType } command to the queue");
    }
    public static void QueueCommandWithoutTarget<T>(CommandType commandType, in AITargetType aiTargetToFind, in DynamicBuffer<Command> commandBuffer) where T : struct, ICommandWithoutTarget
    {
        Command newCommand = new Command
        {
            commandType = commandType,
        };

        T commandData = new T
        {
            TargetTypeToFind = aiTargetToFind
        };

        newCommand.AddCommandWithoutTarget(commandData);

        commandBuffer.Add(newCommand);
        UnityEngine.Debug.Log($"Added { commandType } command to the queue");
    }

    public static void AddStateTransitionToQueue(ref DynamicBuffer<StateTransition> stateTransitionQueue, in Entity entityToTransition, in TargetData targetData, in AIState state)
    {
        stateTransitionQueue.Add(new StateTransition { entity = entityToTransition, target = targetData, aiState = state });
    }
}


public enum CommandType
{
    MoveWithTarget,
    MoveWithoutTarget,
    HarvestWithTarget,
    HarvestWithoutTarget,
    AttackWithTarget,
    AttackWithoutTarget,
    DepositWithTarget,
    DepositWithoutTarget
}

public enum CommandStatus
{
    Queued,
    MovingPhase,
    ExecutionPhase,
    Complete
}

public interface ICommandWithTarget
{
    TargetData TargetData { get; set; }
}

public interface ICommandWithoutTarget
{
    AITargetType TargetTypeToFind { get; set; }
}

[InternalBufferCapacity(10)]
[StructLayout(LayoutKind.Explicit)]
public struct Command : IBufferElementData
{
    [FieldOffset(0)] public CommandStatus commandStatus;
    [FieldOffset(4)] public CommandStatus previousCommandStatus;
    [FieldOffset(8)] public CommandType commandType;
    [FieldOffset(12)] private MoveCommandWithTarget moveCommandWithTarget;
    [FieldOffset(12)] private MoveCommandWithoutTarget moveCommandWithoutTarget;
    [FieldOffset(12)] private HarvestCommandWithTarget harvestCommandWithTarget;
    [FieldOffset(12)] private HarvestCommandWithoutTarget harvestCommandWithoutTarget;
    [FieldOffset(12)] private AttackCommandWithTarget attackCommandWithTarget;
    [FieldOffset(12)] private AttackCommandWithoutTarget attackCommandWithoutTarget;
    [FieldOffset(12)] private DepositCommandWithTarget depositCommandWithTarget;
    [FieldOffset(12)] private DepositCommandWithoutTarget depositCommandWithoutTarget;

    public T GetCommandWithTarget<T>() where T : struct, ICommandWithTarget
    {
        switch (commandType)
        {
            case CommandType.MoveWithTarget:
                return (T)(ICommandWithTarget)moveCommandWithTarget;
            case CommandType.HarvestWithTarget:
                return (T)(ICommandWithTarget)harvestCommandWithTarget;
            case CommandType.AttackWithTarget:
                return (T)(ICommandWithTarget)attackCommandWithTarget;
            case CommandType.DepositWithTarget:
                return (T)(ICommandWithTarget)depositCommandWithTarget;
            default:
                throw new Exception("Invalid command type");
        }
    }
    public T GetCommandWithoutTarget<T>() where T : struct, ICommandWithoutTarget
    {
        switch (commandType)
        {
            case CommandType.MoveWithoutTarget:
                return (T)(ICommandWithoutTarget)moveCommandWithoutTarget;
            case CommandType.HarvestWithoutTarget:
                return (T)(ICommandWithoutTarget)harvestCommandWithoutTarget;
            case CommandType.AttackWithoutTarget:
                return (T)(ICommandWithoutTarget)attackCommandWithoutTarget;
            case CommandType.DepositWithoutTarget:
                return (T)(ICommandWithoutTarget)depositCommandWithoutTarget;
            default:
                throw new Exception("Invalid command type");
        }
    }
    public void AddCommandWithTarget<T>(T commandData) where T : struct, ICommandWithTarget
    {
        if (commandData is MoveCommandWithTarget moveCommandWith)
        {
            moveCommandWithTarget = moveCommandWith;
            return;
        }
        if (commandData is HarvestCommandWithTarget harvestCommandWith)
        {
            harvestCommandWithTarget = harvestCommandWith;
            return;
        }
        if (commandData is AttackCommandWithTarget attackCommandWith)
        {
            attackCommandWithTarget = attackCommandWith;
            return;
        }
        if (commandData is DepositCommandWithTarget depositCommandWith)
        {
            depositCommandWithTarget = depositCommandWith;
            return;
        }
        throw new Exception("Invalid command type");
    }
    public void AddCommandWithoutTarget<T>(T commandData) where T : struct, ICommandWithoutTarget
    {
        if (commandData is MoveCommandWithoutTarget moveCommandWithout)
        {
            moveCommandWithoutTarget = moveCommandWithout;
            return;
        }
        if (commandData is HarvestCommandWithoutTarget harvestCommandWithout)
        {
            harvestCommandWithoutTarget = harvestCommandWithout;
            return;
        }
        if (commandData is AttackCommandWithoutTarget attackCommandWithout)
        {
            attackCommandWithoutTarget = attackCommandWithout;
            return;
        }
        if (commandData is DepositCommandWithoutTarget depositCommandWithout)
        {
            depositCommandWithoutTarget = depositCommandWithout;
            return;
        }
        throw new Exception("Invalid command type");
    }
}

public struct MoveCommandWithTarget : ICommandWithTarget
{
    private TargetData targetData;
    public TargetData TargetData { get => targetData; set => targetData = value; }
}

public struct MoveCommandWithoutTarget : ICommandWithoutTarget
{
    private AITargetType targetTypeToFind;
    public AITargetType TargetTypeToFind { get => targetTypeToFind; set => targetTypeToFind = value; }
}

public struct HarvestCommandWithTarget : ICommandWithTarget
{
    private TargetData targetData;
    public TargetData TargetData { get => targetData; set => targetData = value; }
}

public struct HarvestCommandWithoutTarget : ICommandWithoutTarget
{
    private AITargetType targetTypeToFind;
    public AITargetType TargetTypeToFind { get => targetTypeToFind; set => targetTypeToFind = value; }
}

public struct AttackCommandWithTarget : ICommandWithTarget
{
    private TargetData targetData;
    public TargetData TargetData { get => targetData; set => targetData = value; }
}

public struct AttackCommandWithoutTarget : ICommandWithoutTarget
{
    private AITargetType targetTypeToFind;
    public AITargetType TargetTypeToFind { get => targetTypeToFind; set => targetTypeToFind = value; }
}

public struct DepositCommandWithTarget : ICommandWithTarget
{
    public TargetData targetData;
    public TargetData TargetData { get => targetData; set => targetData = value; }
}

public struct DepositCommandWithoutTarget : ICommandWithoutTarget
{
    private AITargetType targetTypeToFind;
    public AITargetType TargetTypeToFind { get => targetTypeToFind; set => targetTypeToFind = value; }
}