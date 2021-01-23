using Unity.Entities;

public struct RangedUnit : IComponentData
{
	public Entity projectile;
	public float projectileSpeed;
	public float projectileAccuracy;
}
