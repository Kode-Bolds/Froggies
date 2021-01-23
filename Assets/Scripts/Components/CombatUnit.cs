using Unity.Entities;

public enum DamageType
{
	Normal = 0,
	Fire = 1,
	Acid = 2,
	Cold = 3
}

public struct CombatUnit : IComponentData
{
	public float attackRange;
	public float attackSpeed;
	public float attackDamage;
	public DamageType damageType;
}
