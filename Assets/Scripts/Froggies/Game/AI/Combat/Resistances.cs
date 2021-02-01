using Unity.Entities;

namespace Froggies
{
	public struct Resistances : IComponentData
	{
		public float armour;
		public DamageType resistanceFlags;
	}
}