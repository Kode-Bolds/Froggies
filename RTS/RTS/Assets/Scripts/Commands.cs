using System;
using System.Runtime.InteropServices;
using Unity.Entities;

public enum CommandType
{
	Move = 0,
	Harvest = 1,
	Attack = 2
}

public interface ICommandData
{

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
			default:
				throw new Exception("Invalid command type");
		}
	}

	[FieldOffset(4)] private MoveCommandData moveCommandData;
	[FieldOffset(4)] private HarvestCommandData harvestCommandData;
	[FieldOffset(4)] private AttackCommandData attackCommandData;
}

public struct MoveCommandData : ICommandData
{

}

public struct HarvestCommandData : ICommandData
{

}

public struct AttackCommandData : ICommandData
{

}