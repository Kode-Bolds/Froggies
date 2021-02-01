using System;
using Unity.Entities;

namespace Froggies
{
	[Serializable]
	public struct Health : IComponentData
	{
		public int health;
	}
}