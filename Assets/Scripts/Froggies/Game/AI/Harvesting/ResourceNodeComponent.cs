using System;
using Unity.Entities;

namespace Froggies
{
	public enum ResourceType
	{
		None = 0,
		Food = 1 << 0,
		Building = 1 << 1,
		Rare = 1 << 2
	}

	[Serializable]
	public struct ResourceNode : IComponentData
	{
		public ResourceType resourceType;
		public int resourceAmount;
		public float harvestableRadius;
	}
}