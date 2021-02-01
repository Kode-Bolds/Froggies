using Unity.Entities;

namespace Froggies
{
	public struct RangedUnit : IComponentData
	{
		public Entity projectile;
		public float accuracy;
	}
}