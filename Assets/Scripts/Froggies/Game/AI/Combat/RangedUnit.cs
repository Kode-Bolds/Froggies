using System;
using Unity.Entities;

namespace Froggies
{
	[Serializable]
	public struct RangedUnit : IComponentData
	{
		public Entity projectile;
		public float accuracy;
	}
}