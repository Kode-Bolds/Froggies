using System;
using Unity.Entities;

namespace Froggies
{
	[Serializable]
	public struct Resistances : IComponentData
	{
		public int armour;
		public DamageType resistanceFlags;
	}
}