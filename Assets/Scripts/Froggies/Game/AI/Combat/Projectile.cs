using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Froggies
{
	[Serializable]
	public struct Projectile : IComponentData
	{
		public float projectileSpeed;
	}

	[Serializable]
	public struct ProjectileTarget : IComponentData
	{
		public float3 targetPos;
	}
}