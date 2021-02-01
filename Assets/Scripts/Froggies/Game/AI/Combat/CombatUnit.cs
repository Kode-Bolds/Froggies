using System;
using Unity.Entities;

namespace Froggies
{
	[Flags]
	public enum DamageType
	{
		Normal = 1 << 0,
		Fire = 1 << 1,
		Acid = 1 << 2,
		Cold = 1 << 3
	}

	public struct CombatUnit : IComponentData
	{
		public float attackRange;
		public float attackSpeed;
		public float attackDamage;
		public DamageType damageType;
	}
}