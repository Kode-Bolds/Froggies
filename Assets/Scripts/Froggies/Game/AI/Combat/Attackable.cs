using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Froggies
{
	[Serializable]
	public struct Attackable : IComponentData
	{
		public float3 centreOffset;
	}
}