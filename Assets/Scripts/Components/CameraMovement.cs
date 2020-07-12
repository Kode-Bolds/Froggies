using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct CameraMovement : IComponentData
{
	public float keyMoveSpeed;
	public float edgePanMoveSpeed;
	public float middleMouseMoveSpeed;
	public float zoomSpeed;
	public float edgePanMargin; // Keep margin?
	public float2 cameraLimits;
}