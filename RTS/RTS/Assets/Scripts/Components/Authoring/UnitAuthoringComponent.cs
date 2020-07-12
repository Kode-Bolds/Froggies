using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;

public enum UnitType
{
	None = 0,
	Harvester = 1 << 0,
	Melee = 1 << 1,
	Ranged = 1 << 2
}

[RequireComponent(typeof(PhysicsBodyAuthoring))]
[RequireComponent(typeof(PhysicsShapeAuthoring))]
public class UnitAuthoringComponent : MonoBehaviour, IConvertGameObjectToEntity
{
	[HideInInspector] public UnitType unitType;
	[HideInInspector] public UnitMove unitMove;
	[HideInInspector] public FreezeRotation freezeRotation;
	[HideInInspector] public Harvester harvester;
	[HideInInspector] public bool isEnemy;

	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		dstManager.AddComponentData(entity, unitMove);
		dstManager.AddComponentData(entity, freezeRotation);
		dstManager.AddComponentData(entity, new UnitTag());

		dstManager.AddComponentData(entity, new CurrentTarget{ findTargetOfType = AITargetType.None, targetData = new TargetData()});
		dstManager.AddComponentData(entity, new PreviousTarget{ targetData = new TargetData()});
		if((unitType & UnitType.Harvester) != 0)
			dstManager.AddComponentData(entity, harvester);

		if (isEnemy)
			dstManager.AddComponentData(entity, new EnemyTag());
	}
}
