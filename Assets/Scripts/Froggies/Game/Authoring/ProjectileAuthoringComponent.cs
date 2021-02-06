using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Froggies
{
	[RequireComponent(typeof(PhysicsBodyAuthoring))]
	[RequireComponent(typeof(PhysicsShapeAuthoring))]
	public class ProjectileAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
	{
		public Projectile projectile;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			dstManager.AddComponentData(entity, projectile);
		}
	}
}
