using System;
using Unity.Entities;

namespace Froggies
{
	public enum AITargetType
	{
		None = 0,
		FoodResource = 1 << 0,
		BuildingResource = 1 << 1,
		RareResource = 1 << 2,
		ResourceNode = FoodResource | BuildingResource | RareResource,

		Store = 1 << 3,
		Enemy = 1 << 4,
		Ground = 1 << 5
	}

	[Serializable]
	[GenerateAuthoringComponent]
	public struct TargetableByAI : IComponentData
	{
		public AITargetType targetType;
	}
}