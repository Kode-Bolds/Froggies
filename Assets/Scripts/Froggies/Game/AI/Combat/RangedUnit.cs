using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Froggies
{
	[Serializable]
	public struct RangedUnit : IComponentData
	{
		public Entity projectile;
		public float accuracy;
		public float3 projectileSpawnOffset;
	}
}