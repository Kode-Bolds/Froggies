using UnityEngine;
using Unity.Entities;
using Unity.Physics.Authoring;
using Unity.Mathematics;

namespace Froggies
{
	[RequireComponent(typeof(PhysicsShapeAuthoring))]
	public class ResourceNodeAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
	{
		[HideInInspector] public ResourceNode resourceNode;
		[HideInInspector] public TargetableByAI aiTarget;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			Transform transform = GetComponent<Transform>();
			Debug.Assert(transform.localScale.x == transform.localScale.z, "Must have a uniform scale on x and z axis!");

			float radius = GetComponent<PhysicsShapeAuthoring>().GetSphereProperties(out quaternion _).Radius;
			float scale = transform.localScale.x;
			resourceNode.harvestableRadius = radius * scale;
			dstManager.AddComponentData(entity, resourceNode);
			dstManager.AddComponentData(entity, aiTarget);
		}
	}
}