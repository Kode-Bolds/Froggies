using Unity.Entities;

[GenerateAuthoringComponent]
public struct FreezeRotation : IComponentData
{
	public bool x;
	public bool y;
	public bool z;

	public bool DeanIsSmelly;
}