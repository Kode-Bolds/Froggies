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

	[Serializable]
	public struct CombatUnit : IComponentData
	{
		public int attackDamage;
		public DamageType damageType;
		public int attackRange;
		public float attackSpeed;
		public float attackTimer;
	}
}