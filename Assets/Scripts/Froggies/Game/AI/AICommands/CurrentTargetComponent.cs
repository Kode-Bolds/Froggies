using Unity.Entities;
using Unity.Mathematics;

namespace Froggies
{
	public struct CurrentTarget : IComponentData
	{
		public TargetData targetData;
	}

	public struct PreviousTarget : IComponentData
	{
		public TargetData targetData;
	}

	public struct TargetData : IComponentData
	{
		public Entity targetEntity;
		public AITargetType targetType;
		public float3 targetPos;
	}
}