using System;
using Unity.Entities;

[Serializable]
public struct FreezeRotation : IComponentData
{
	public bool x;
	public bool y;
	public bool z;
}
