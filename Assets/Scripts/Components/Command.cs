using System.Runtime.InteropServices;
using Unity.Entities;

public enum CommandType
{
	Move,
	Harvest,
	Attack,
	Deposit,
}

public enum CommandStatus
{
	Queued,
	MovingPhase,
	ExecutionPhase,
	Complete
}

[InternalBufferCapacity(10)]
[StructLayout(LayoutKind.Explicit)]
public struct Command : IBufferElementData
{
	[FieldOffset(0)] public CommandStatus commandStatus;
	[FieldOffset(4)] public CommandStatus previousCommandStatus;
	[FieldOffset(8)] public CommandType commandType;
	[FieldOffset(12)] public CommandData commandData;
}

public struct CommandData
{
	public TargetData targetData;
}